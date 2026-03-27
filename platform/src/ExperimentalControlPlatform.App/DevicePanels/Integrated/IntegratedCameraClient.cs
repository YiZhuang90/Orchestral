using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Runtime;
using ExperimentalControlPlatform.Devices.Uvc;

namespace ExperimentalControlPlatform.App.DevicePanels.Integrated;

public sealed class IntegratedCameraClient : IIntegratedCameraRuntimeService
{
    private readonly IUvcCameraService _service;

    public IntegratedCameraClient(IUvcCameraService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public IReadOnlyList<UvcCameraInfo> ListCameras() => _service.ListCameras();

    public IntegratedFrameResult CaptureSnapshot(
        int cameraIndex,
        double targetFrameRate,
        bool color)
    {
        var frame = _service.CaptureSnapshot(new UvcAcquisitionSettings(cameraIndex, targetFrameRate, color));
        return ToResult(frame);
    }

    public Task StreamFramesAsync(
        int cameraIndex,
        double targetFrameRate,
        bool color,
        Func<IntegratedFrameResult, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        return _service.StreamFramesAsync(
            new UvcAcquisitionSettings(cameraIndex, targetFrameRate, color),
            frame => onFrame(ToResult(frame)),
            cancellationToken);
    }

    IntegratedCameraFrame IIntegratedCameraRuntimeService.CaptureSnapshot(IntegratedCameraCaptureSettings settings)
    {
        var frame = _service.CaptureSnapshot(new UvcAcquisitionSettings(settings.CameraIndex, settings.TargetFrameRate, settings.ColorEnabled));
        return new IntegratedCameraFrame(
            settings.DeviceId,
            settings.DisplayName,
            frame.Width,
            frame.Height,
            frame.PixelData,
            frame.IsColor,
            frame.TimestampTicks);
    }

    Task IIntegratedCameraRuntimeService.StreamFramesAsync(
        IntegratedCameraCaptureSettings settings,
        Func<IntegratedCameraFrame, Task> onFrame,
        CancellationToken cancellationToken)
    {
        return _service.StreamFramesAsync(
            new UvcAcquisitionSettings(settings.CameraIndex, settings.TargetFrameRate, settings.ColorEnabled),
            frame => onFrame(new IntegratedCameraFrame(
                settings.DeviceId,
                settings.DisplayName,
                frame.Width,
                frame.Height,
                frame.PixelData,
                frame.IsColor,
                frame.TimestampTicks)),
            cancellationToken);
    }

    private static IntegratedFrameResult ToResult(UvcFrame frame)
    {
        return new IntegratedFrameResult(
            frame.Width,
            frame.Height,
            frame.PixelData,
            frame.IsColor,
            frame.TimestampTicks);
    }
}
