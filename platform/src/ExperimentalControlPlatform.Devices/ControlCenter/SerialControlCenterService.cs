using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace ExperimentalControlPlatform.Devices.ControlCenter;

public sealed class SerialControlCenterService : IControlCenterService
{
    public IReadOnlyList<ControlCenterDeviceInfo> ListDevices()
    {
        var portNames = SerialPort.GetPortNames();
        Array.Sort(portNames, StringComparer.OrdinalIgnoreCase);

        var devices = new List<ControlCenterDeviceInfo>(portNames.Length);
        foreach (var portName in portNames)
        {
            devices.Add(ControlCenterDeviceInfo.FromPortName(portName));
        }

        return devices;
    }

    public IControlCenterConnection Open(ControlCenterDeviceInfo device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var port = new SerialPort(device.PortName, ControlCenterProtocol.DefaultBaudRate)
        {
            ReadTimeout = 2000,
            WriteTimeout = 2000,
            NewLine = "\n"
        };
        port.Open();
        return new SerialControlCenterConnection(device, port);
    }

    public void SendCommand(IControlCenterConnection connection, ControlCenterCommand command)
    {
        var serialConnection = RequireSerialConnection(connection);
        var formatted = ControlCenterProtocol.FormatCommand(command);
        serialConnection.Port.Write(formatted);
    }

    public ControlCenterPulseReadback ReadPulseCount(IControlCenterConnection connection)
    {
        var serialConnection = RequireSerialConnection(connection);
        var line = serialConnection.Port.ReadLine();
        return ControlCenterProtocol.ParsePulseReadback(line, serialConnection.Device, DateTimeOffset.UtcNow);
    }

    private static SerialControlCenterConnection RequireSerialConnection(IControlCenterConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return connection as SerialControlCenterConnection
            ?? throw new InvalidOperationException("Control center connection was not created by SerialControlCenterService.");
    }

    private sealed class SerialControlCenterConnection : IControlCenterConnection
    {
        public SerialControlCenterConnection(ControlCenterDeviceInfo device, SerialPort port)
        {
            Device = device;
            Port = port;
        }

        public ControlCenterDeviceInfo Device { get; }

        public SerialPort Port { get; }

        public void Dispose()
        {
            if (Port.IsOpen)
            {
                Port.Close();
            }

            Port.Dispose();
        }
    }
}
