using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ExperimentalControlPlatform.Devices.HuaTeng;

public sealed class NativeHuaTengSdk : IHuaTengSdk
{
    private const string DllName = "MVCAMSDK_X64.dll";
    private const uint CameraMediaTypeMono8 = 0x01080001;
    private const uint CameraMediaTypeBgr8 = 0x02180014;
    private const int Success = 0;

    public IReadOnlyList<HuaTengCameraInfo> EnumerateDevices()
    {
        var count = 32;
        var devices = new SdkCameraDevInfo[count];
        var status = CameraEnumerateDevice(devices, ref count);
        ThrowIfFailed(status, "CameraEnumerateDevice");

        var result = new List<HuaTengCameraInfo>(count);
        for (var index = 0; index < count; index++)
        {
            var item = devices[index];
            result.Add(new HuaTengCameraInfo(
                index,
                item.ProductName ?? string.Empty,
                item.FriendlyName ?? string.Empty,
                item.PortType ?? string.Empty,
                item.SerialNumber ?? string.Empty,
                item.SensorType ?? string.Empty,
                unchecked((int)item.Instance)));
        }

        return result;
    }

    public IHuaTengCameraSession OpenCamera(HuaTengCameraInfo camera)
    {
        var count = 32;
        var devices = new SdkCameraDevInfo[count];
        var status = CameraEnumerateDevice(devices, ref count);
        ThrowIfFailed(status, "CameraEnumerateDevice");

        if (camera.Index < 0 || camera.Index >= count)
        {
            throw new InvalidOperationException($"HuaTeng camera index {camera.Index} is out of range.");
        }

        var selected = devices[camera.Index];
        status = CameraInit(ref selected, -1, -1, out var handle);
        ThrowIfFailed(status, "CameraInit");

        try
        {
            status = CameraGetCapabilityEx2(handle, out var maxWidth, out var maxHeight, out var colorCamera);
            ThrowIfFailed(status, "CameraGetCapabilityEx2");

            var capability = new HuaTengCameraCapability(maxWidth, maxHeight, colorCamera == 0);
            return new NativeHuaTengCameraSession(handle, camera, capability);
        }
        catch
        {
            CameraUnInit(handle);
            throw;
        }
    }

    public string GetErrorString(int statusCode)
    {
        var pointer = CameraGetErrorStringNative(statusCode);
        return pointer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(pointer) ?? string.Empty;
    }

    private void ThrowIfFailed(int statusCode, string operation)
    {
        if (statusCode == Success)
        {
            return;
        }

        var detail = GetErrorString(statusCode);
        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new InvalidOperationException($"{operation} failed with status {statusCode}.");
        }

        throw new InvalidOperationException($"{operation} failed with status {statusCode}: {detail}");
    }

    private sealed class NativeHuaTengCameraSession : IHuaTengCameraSession
    {
        private readonly int _handle;
        private readonly HuaTengCameraCapability _capability;
        private IntPtr _frameBuffer;
        private HuaTengPixelFormat _outputFormat = HuaTengPixelFormat.Auto;
        private HuaTengTriggerMode _triggerMode = HuaTengTriggerMode.Triggered;
        private double _exposureUs;
        private HuaTengRoi? _appliedRoi;
        private bool _disposed;

        public NativeHuaTengCameraSession(int handle, HuaTengCameraInfo cameraInfo, HuaTengCameraCapability capability)
        {
            _handle = handle;
            CameraInfo = cameraInfo;
            _capability = capability;
            _frameBuffer = CameraAlignMalloc(Math.Max(1, capability.MaxWidth * capability.MaxHeight * 3), 16);
            if (_frameBuffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("CameraAlignMalloc failed.");
            }
        }

        public HuaTengCameraInfo CameraInfo { get; }

        public HuaTengCameraCapability Capability => _capability;

        public void SetOutputFormat(HuaTengPixelFormat pixelFormat)
        {
            var actual = pixelFormat == HuaTengPixelFormat.Auto
                ? (_capability.IsMonoSensor ? HuaTengPixelFormat.Mono8 : HuaTengPixelFormat.Bgr8)
                : pixelFormat;

            var status = CameraSetIspOutFormat(_handle, actual == HuaTengPixelFormat.Mono8 ? CameraMediaTypeMono8 : CameraMediaTypeBgr8);
            ThrowIfFailed(status, "CameraSetIspOutFormat");
            _outputFormat = actual;
        }

        public void SetTriggerMode(HuaTengTriggerMode triggerMode)
        {
            var status = CameraSetTriggerMode(_handle, triggerMode == HuaTengTriggerMode.Triggered ? 1 : 0);
            ThrowIfFailed(status, "CameraSetTriggerMode");
            _triggerMode = triggerMode;
        }

        public void SetExposure(double? exposureUs)
        {
            if (!exposureUs.HasValue)
            {
                return;
            }

            var status = CameraSetExposureTime(_handle, exposureUs.Value);
            ThrowIfFailed(status, "CameraSetExposureTime");
            _exposureUs = GetExposure();
        }

        public double GetExposure()
        {
            var status = CameraGetExposureTime(_handle, out var exposureUs);
            ThrowIfFailed(status, "CameraGetExposureTime");
            _exposureUs = exposureUs;
            return exposureUs;
        }

        public HuaTengRoi? ApplyRoi(HuaTengRoi? roi)
        {
            if (!roi.HasValue)
            {
                _appliedRoi = null;
                return null;
            }

            var normalized = NormalizeRoi(roi.Value, _capability.MaxWidth, _capability.MaxHeight);
            var status = CameraGetImageResolution(_handle, out var resolution);
            ThrowIfFailed(status, "CameraGetImageResolution");

            resolution.Index = 0xFF;
            resolution.HOffsetFov = normalized.X;
            resolution.VOffsetFov = normalized.Y;
            resolution.WidthFov = normalized.Width;
            resolution.HeightFov = normalized.Height;
            resolution.Width = normalized.Width;
            resolution.Height = normalized.Height;
            resolution.WidthZoomHd = 0;
            resolution.HeightZoomHd = 0;
            resolution.WidthZoomSw = 0;
            resolution.HeightZoomSw = 0;

            status = CameraSetImageResolution(_handle, ref resolution);
            ThrowIfFailed(status, "CameraSetImageResolution");
            _appliedRoi = normalized;
            return normalized;
        }

        public void Play()
        {
            var status = CameraPlay(_handle);
            ThrowIfFailed(status, "CameraPlay");
        }

        public void SoftTrigger()
        {
            var status = CameraSoftTrigger(_handle);
            ThrowIfFailed(status, "CameraSoftTrigger");
        }

        public HuaTengRawFrame GetFrame(int timeoutMs)
        {
            var outputFormat = _outputFormat == HuaTengPixelFormat.Mono8 ? CameraMediaTypeMono8 : CameraMediaTypeBgr8;
            var status = CameraGetImageBufferEx3(_handle, _frameBuffer, outputFormat, out var width, out var height, out var timestampTenths, timeoutMs);
            ThrowIfFailed(status, "CameraGetImageBufferEx3");

            var bytesPerPixel = _outputFormat == HuaTengPixelFormat.Mono8 ? 1 : 3;
            var byteCount = checked(width * height * bytesPerPixel);
            var pixels = new byte[byteCount];
            Marshal.Copy(_frameBuffer, pixels, 0, byteCount);

            return new HuaTengRawFrame(width, height, pixels, timestampTenths, _exposureUs > 0 ? _exposureUs : GetExposure());
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_frameBuffer != IntPtr.Zero)
            {
                CameraAlignFree(_frameBuffer);
                _frameBuffer = IntPtr.Zero;
            }

            CameraUnInit(_handle);
        }

        private static HuaTengRoi NormalizeRoi(HuaTengRoi roi, int maxWidth, int maxHeight)
        {
            var x = Math.Max(0, roi.X);
            var y = Math.Max(0, roi.Y);
            var width = Math.Max(12, roi.Width);
            var height = Math.Max(12, roi.Height);

            x -= x % 2;
            y -= y % 2;
            width -= width % 12;
            height -= height % 12;
            width = Math.Max(12, width);
            height = Math.Max(12, height);

            width = Math.Min(width, maxWidth);
            height = Math.Min(height, maxHeight);
            x = Math.Min(x, maxWidth - width);
            y = Math.Min(y, maxHeight - height);

            return new HuaTengRoi(x, y, width, height);
        }

        private static void ThrowIfFailed(int statusCode, string operation)
        {
            if (statusCode != Success)
            {
                throw new InvalidOperationException($"{operation} failed with status {statusCode}.");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct SdkCameraDevInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ProductSeries;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ProductName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FriendlyName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string LinkName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DriverVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string SensorType;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string PortType;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string SerialNumber;

        public uint Instance;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct SdkImageResolution
    {
        public int Index;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Description;

        public uint BinSumMode;
        public uint BinAverageMode;
        public uint SkipMode;
        public uint ResampleMask;
        public int HOffsetFov;
        public int VOffsetFov;
        public int WidthFov;
        public int HeightFov;
        public int Width;
        public int Height;
        public int WidthZoomHd;
        public int HeightZoomHd;
        public int WidthZoomSw;
        public int HeightZoomSw;
    }

    [DllImport(DllName, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int CameraEnumerateDevice([Out] SdkCameraDevInfo[] cameraList, ref int count);

    [DllImport(DllName, CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int CameraInit(ref SdkCameraDevInfo cameraInfo, int paramLoadMode, int team, out int cameraHandle);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraUnInit(int cameraHandle);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraGetCapabilityEx2(int cameraHandle, out int maxWidth, out int maxHeight, out int colorCamera);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraGetImageResolution(int cameraHandle, out SdkImageResolution resolution);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraSetImageResolution(int cameraHandle, ref SdkImageResolution resolution);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraSetIspOutFormat(int cameraHandle, uint format);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraSetTriggerMode(int cameraHandle, int modeSelection);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraSetExposureTime(int cameraHandle, double exposureUs);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraGetExposureTime(int cameraHandle, out double exposureUs);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraPlay(int cameraHandle);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraSoftTrigger(int cameraHandle);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern int CameraGetImageBufferEx3(int cameraHandle, IntPtr imageData, uint outFormat, out int width, out int height, out uint timestampTenths, int timeoutMs);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern IntPtr CameraAlignMalloc(int size, int align);

    [DllImport(DllName, ExactSpelling = true)]
    private static extern void CameraAlignFree(IntPtr memoryBuffer);

    [DllImport(DllName, CharSet = CharSet.Ansi, ExactSpelling = true, EntryPoint = "CameraGetErrorString")]
    private static extern IntPtr CameraGetErrorStringNative(int statusCode);
}
