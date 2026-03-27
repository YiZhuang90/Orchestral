using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ExperimentalControlPlatform.Runtime;
using ExperimentalControlPlatform.Devices.HuaTeng;

namespace ExperimentalControlPlatform.App.DevicePanels.HuaTeng;

using DeviceCameraInfo = ExperimentalControlPlatform.Devices.HuaTeng.HuaTengCameraInfo;
using AppCameraInfo = ExperimentalControlPlatform.App.DevicePanels.HuaTeng.HuaTengCameraInfo;

public sealed class HuaTengCameraProbeClient : IHuaTengCameraRuntimeService
{
    private readonly HuaTengNativeCameraService _service;

    public HuaTengCameraProbeClient()
        : this(new HuaTengNativeCameraService(new NativeHuaTengSdk()))
    {
    }

    internal HuaTengCameraProbeClient(HuaTengNativeCameraService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<HuaTengProbeResult> ListCamerasAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var cameras = _service.ListCameras().Select(MapCamera).ToArray();
            return Task.FromResult(new HuaTengProbeResult
            {
                Ok = true,
                Summary = cameras.Length == 0
                    ? "No HuaTeng camera was found over USB."
                    : $"Found {cameras.Length} HuaTeng camera{(cameras.Length == 1 ? string.Empty : "s")}.",
                Cameras = cameras,
                Diagnostics = []
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HuaTengProbeResult
            {
                Ok = false,
                Summary = $"Camera discovery failed: {ex.Message}",
                Cameras = [],
                Diagnostics = [ex.Message],
                Exception = ex.ToString()
            });
        }
    }

    public Task<HuaTengFrameResult> CaptureSnapshotAsync(
        int index,
        string pixelFormat,
        string triggerMode,
        double? exposureUs,
        Rect? roi = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var settings = CreateSettings(index, pixelFormat, triggerMode, exposureUs, 1.0, roi);
            var frame = _service.CaptureSnapshot(settings);
            return Task.FromResult(MapFrame("Frame captured.", frame));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HuaTengFrameResult
            {
                Ok = false,
                Summary = $"Snapshot failed: {ex.Message}",
                Diagnostics = [ex.Message],
                Exception = ex.ToString()
            });
        }
    }

    public Task StreamFramesAsync(
        int index,
        string pixelFormat,
        string triggerMode,
        double? exposureUs,
        double targetFps,
        Rect? roi,
        Func<HuaTengFrameResult, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onFrame);

        var settings = CreateSettings(index, pixelFormat, triggerMode, exposureUs, targetFps, roi);
        return _service.StreamFramesAsync(
            settings,
            frame => onFrame(MapFrame("Streaming.", frame)),
            cancellationToken);
    }

    async Task<HuaTengFrame> IHuaTengCameraRuntimeService.CaptureSnapshotAsync(HuaTengCaptureSettings settings, CancellationToken cancellationToken)
    {
        var result = await CaptureSnapshotAsync(
            settings.CameraIndex,
            settings.PixelFormat,
            settings.TriggerMode,
            settings.ExposureUs,
            ToRect(settings.Roi),
            cancellationToken).ConfigureAwait(false);
        return ToRuntimeFrame(settings, result);
    }

    Task IHuaTengCameraRuntimeService.StreamFramesAsync(
        HuaTengCaptureSettings settings,
        Func<HuaTengFrame, Task> onFrame,
        CancellationToken cancellationToken)
    {
        return StreamFramesAsync(
            settings.CameraIndex,
            settings.PixelFormat,
            settings.TriggerMode,
            settings.ExposureUs,
            settings.TargetFrameRate,
            ToRect(settings.Roi),
            frame => onFrame(ToRuntimeFrame(settings, frame)),
            cancellationToken);
    }

    private static HuaTengAcquisitionSettings CreateSettings(
        int index,
        string pixelFormat,
        string triggerMode,
        double? exposureUs,
        double targetFps,
        Rect? roi)
    {
        return new HuaTengAcquisitionSettings(
            index,
            ParsePixelFormat(pixelFormat),
            ParseTriggerMode(triggerMode),
            exposureUs,
            targetFps,
            roi.HasValue
                ? new HuaTengRoi(
                    (int)Math.Round(roi.Value.X),
                    (int)Math.Round(roi.Value.Y),
                    Math.Max(2, (int)Math.Round(roi.Value.Width)),
                    Math.Max(2, (int)Math.Round(roi.Value.Height)))
                : null);
    }

    private static HuaTengFrameResult MapFrame(string summary, HuaTengCapturedFrame frame)
    {
        return new HuaTengFrameResult
        {
            Ok = true,
            Summary = summary,
            Camera = MapCamera(frame.Camera),
            Width = frame.Width,
            Height = frame.Height,
            PixelData = frame.PixelData,
            PixelFormat = FormatPixelFormat(frame.PixelFormat),
            TriggerMode = FormatTriggerMode(frame.TriggerMode),
            ExposureUs = frame.ExposureUs,
            IsMono = frame.IsMono,
            TimestampTenthsOfMilliseconds = unchecked((int)frame.TimestampTenthsOfMilliseconds),
            Roi = frame.Roi is null
                ? null
                : new Rect(frame.Roi.Value.X, frame.Roi.Value.Y, frame.Roi.Value.Width, frame.Roi.Value.Height),
            Diagnostics = []
        };
    }

    private static AppCameraInfo MapCamera(DeviceCameraInfo camera)
    {
        return new AppCameraInfo
        {
            Index = camera.Index,
            ProductName = camera.ProductName,
            FriendlyName = camera.FriendlyName,
            PortType = camera.PortType,
            SerialNumber = camera.SerialNumber,
            SensorType = camera.SensorType,
            Instance = camera.Instance
        };
    }

    private static HuaTengPixelFormat ParsePixelFormat(string pixelFormat)
    {
        return pixelFormat.Trim().ToLowerInvariant() switch
        {
            "mono8" => HuaTengPixelFormat.Mono8,
            "bgr8" => HuaTengPixelFormat.Bgr8,
            _ => HuaTengPixelFormat.Auto
        };
    }

    private static HuaTengTriggerMode ParseTriggerMode(string triggerMode)
    {
        return triggerMode.Trim().ToLowerInvariant() switch
        {
            "continuous" => HuaTengTriggerMode.Continuous,
            _ => HuaTengTriggerMode.Triggered
        };
    }

    private static string FormatPixelFormat(HuaTengPixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            HuaTengPixelFormat.Mono8 => "Mono8",
            HuaTengPixelFormat.Bgr8 => "Bgr8",
            _ => "Auto"
        };
    }

    private static string FormatTriggerMode(HuaTengTriggerMode triggerMode)
    {
        return triggerMode switch
        {
            HuaTengTriggerMode.Triggered => "Triggered",
            _ => "Continuous"
        };
    }

    private static Rect? ToRect(CaptureRegion? region)
    {
        return region is null ? null : new Rect(region.X, region.Y, region.Width, region.Height);
    }

    private static CaptureRegion? ToRegion(Rect? rect)
    {
        return rect is null ? null : new CaptureRegion(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height);
    }

    private static HuaTengFrame ToRuntimeFrame(HuaTengCaptureSettings settings, HuaTengFrameResult result)
    {
        return new HuaTengFrame(
            settings.DeviceId,
            settings.DisplayName,
            result.Ok,
            result.Summary,
            result.Width,
            result.Height,
            result.PixelData,
            result.PixelFormat,
            result.TriggerMode,
            settings.ColorTone,
            result.ExposureUs,
            result.IsMono,
            result.TimestampTenthsOfMilliseconds,
            ToRegion(result.Roi),
            result.Diagnostics);
    }
}
