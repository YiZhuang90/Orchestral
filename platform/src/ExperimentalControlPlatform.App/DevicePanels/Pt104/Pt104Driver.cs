using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App.DevicePanels.Pt104;

public sealed class Pt104Driver : IPt104RuntimeDriver
{
    private const uint PicoOk = 0x00000000;
    private const uint PicoNoSamplesAvailable = 0x00000025;
    private const uint PicoBusy = 0x00000027;
    private const uint PicoWarningRepeatValue = 0x00000118;
    private const uint CtUsb = 0x00000001;

    private static readonly string[] LibraryCandidates =
    {
        @"C:\Program Files\Pico Technology\SDK\lib\usbpt104.dll",
        @"C:\Program Files\Pico Technology\PicoLog\usbpt104.dll",
    };

    private static readonly object LibrarySync = new();
    private static bool s_libraryLoaded;
    private static nint s_libraryHandle;

    private short _handle;
    private int _configuredChannel;

    public bool IsConnected => _handle != 0;

    public string? ConnectedDeviceId { get; private set; }

    public IReadOnlyList<string> EnumerateUsbUnits()
    {
        EnsureLibraryLoaded();

        var details = new byte[512];
        var length = (uint)details.Length;
        var status = UsbPt104Enumerate(details, ref length, CtUsb);
        EnsureStatus(status, "enumerate PT-104 units");

        var raw = DecodeCString(details);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    public void Connect(Pt104ConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (IsConnected)
        {
            throw new InvalidOperationException("PT-104 is already connected.");
        }

        EnsureLibraryLoaded();

        var deviceEntries = EnumerateUsbUnits();
        if (deviceEntries.Count == 0)
        {
            throw new InvalidOperationException("No PT-104 device was found over USB.");
        }

        var firstEntry = deviceEntries[0];
        var serial = ExtractSerial(firstEntry);
        short handle = 0;

        var status = UsbPt104OpenUnit(ref handle, serial);
        EnsureStatus(status, $"open PT-104 unit '{serial}'");

        try
        {
            ConfigureChannel(handle, settings);
            _handle = handle;
            _configuredChannel = settings.Channel;
            ConnectedDeviceId = firstEntry;
        }
        catch
        {
            if (handle != 0)
            {
                UsbPt104CloseUnit(handle);
            }

            throw;
        }
    }

    public void Connect(Pt104ChannelConfiguration configuration)
    {
        Connect(ToPanelSettings(configuration));
    }

    public double ReadTemperatureC(bool filtered, int attempts = 10, int delayMilliseconds = 800, bool allowRepeatValue = true)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("PT-104 is not connected.");
        }

        uint lastStatus = PicoNoSamplesAvailable;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            int value = 0;
            var status = UsbPt104GetValue(_handle, _configuredChannel, ref value, (short)(filtered ? 1 : 0));
            if (status == PicoOk || (allowRepeatValue && status == PicoWarningRepeatValue))
            {
                return value / 1000.0;
            }

            lastStatus = status;
            if (status is PicoNoSamplesAvailable or PicoBusy)
            {
                Thread.Sleep(delayMilliseconds);
                continue;
            }

            EnsureStatus(status, "read PT-104 value");
        }

        throw new TimeoutException($"PT-104 did not produce a fresh sample after {attempts} attempts. Last status: 0x{lastStatus:X8}.");
    }

    public void ApplySettings(Pt104ConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!IsConnected)
        {
            throw new InvalidOperationException("PT-104 is not connected.");
        }

        ConfigureChannel(_handle, settings);
        _configuredChannel = settings.Channel;
    }

    public void ApplySettings(Pt104ChannelConfiguration configuration)
    {
        ApplySettings(ToPanelSettings(configuration));
    }

    public void ConfigureChannel(Pt104ConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!IsConnected)
        {
            throw new InvalidOperationException("PT-104 is not connected.");
        }

        ConfigureChannel(_handle, settings);
    }

    public void ConfigureChannel(Pt104ChannelConfiguration configuration)
    {
        ConfigureChannel(ToPanelSettings(configuration));
    }

    public double ReadTemperatureC(int channel, bool filtered, int attempts = 10, int delayMilliseconds = 800, bool allowRepeatValue = true)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("PT-104 is not connected.");
        }

        uint lastStatus = PicoNoSamplesAvailable;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            int value = 0;
            var status = UsbPt104GetValue(_handle, channel, ref value, (short)(filtered ? 1 : 0));
            if (status == PicoOk || (allowRepeatValue && status == PicoWarningRepeatValue))
            {
                return value / 1000.0;
            }

            lastStatus = status;
            if (status is PicoNoSamplesAvailable or PicoBusy)
            {
                Thread.Sleep(delayMilliseconds);
                continue;
            }

            EnsureStatus(status, $"read PT-104 channel {channel} value");
        }

        throw new TimeoutException($"PT-104 channel {channel} did not produce a fresh sample after {attempts} attempts. Last status: 0x{lastStatus:X8}.");
    }

    public void Disconnect()
    {
        if (!IsConnected)
        {
            return;
        }

        var status = UsbPt104CloseUnit(_handle);
        _handle = 0;
        _configuredChannel = 0;
        ConnectedDeviceId = null;
        EnsureStatus(status, "close PT-104 unit");
    }

    public void Dispose()
    {
        if (IsConnected)
        {
            try
            {
                Disconnect();
            }
            catch
            {
                // Best effort on dispose for the first device panel slice.
            }
        }
    }

    private static Pt104ConnectionSettings ToPanelSettings(Pt104ChannelConfiguration configuration)
    {
        return new Pt104ConnectionSettings(
            configuration.Channel,
            configuration.MeasurementMode switch
            {
                Pt104MeasurementMode.Pt1000 => Pt104MeasurementType.Pt1000,
                _ => Pt104MeasurementType.Pt100
            },
            configuration.WireCount,
            configuration.MainsFrequencyHz,
            configuration.FilteredRead);
    }

    private static void EnsureLibraryLoaded()
    {
        if (s_libraryLoaded)
        {
            return;
        }

        lock (LibrarySync)
        {
            if (s_libraryLoaded)
            {
                return;
            }

            foreach (var candidate in LibraryCandidates.Where(File.Exists))
            {
                s_libraryHandle = NativeLibrary.Load(candidate);
                s_libraryLoaded = true;
                return;
            }

            throw new FileNotFoundException($"Could not locate usbpt104.dll. Checked: {string.Join(", ", LibraryCandidates)}");
        }
    }

    private static string ExtractSerial(string entry)
    {
        if (entry.Contains(':'))
        {
            return entry.Split(':', 2)[1].Trim();
        }

        return entry.Trim();
    }

    private static string DecodeCString(byte[] buffer)
    {
        var zeroIndex = Array.IndexOf(buffer, (byte)0);
        if (zeroIndex < 0)
        {
            zeroIndex = buffer.Length;
        }

        return System.Text.Encoding.ASCII.GetString(buffer, 0, zeroIndex).Trim();
    }

    private static void EnsureStatus(uint status, string action)
    {
        if (status == PicoOk)
        {
            return;
        }

        throw new InvalidOperationException($"Unable to {action}. Pico status: 0x{status:X8}.");
    }

    private static void ConfigureChannel(short handle, Pt104ConnectionSettings settings)
    {
        var status = UsbPt104SetMains(handle, settings.MainsFrequencyHz == 60 ? (ushort)1 : (ushort)0);
        EnsureStatus(status, "set PT-104 mains filter");

        status = UsbPt104SetChannel(handle, settings.Channel, (int)settings.MeasurementType, (short)settings.WireCount);
        EnsureStatus(status, "configure PT-104 channel");
    }

    [DllImport("usbpt104.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "UsbPt104Enumerate")]
    private static extern uint UsbPt104Enumerate([Out] byte[] details, ref uint length, uint type);

    [DllImport("usbpt104.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "UsbPt104OpenUnit", CharSet = CharSet.Ansi)]
    private static extern uint UsbPt104OpenUnit(ref short handle, string serial);

    [DllImport("usbpt104.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "UsbPt104SetMains")]
    private static extern uint UsbPt104SetMains(short handle, ushort sixtyHertz);

    [DllImport("usbpt104.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "UsbPt104SetChannel")]
    private static extern uint UsbPt104SetChannel(short handle, int channel, int type, short noOfWires);

    [DllImport("usbpt104.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "UsbPt104GetValue")]
    private static extern uint UsbPt104GetValue(short handle, int channel, ref int value, short filtered);

    [DllImport("usbpt104.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "UsbPt104CloseUnit")]
    private static extern uint UsbPt104CloseUnit(short handle);
}
