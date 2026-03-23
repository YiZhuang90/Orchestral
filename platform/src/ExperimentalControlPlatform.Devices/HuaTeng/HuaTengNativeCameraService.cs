using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Devices.HuaTeng;

public sealed class HuaTengNativeCameraService
{
    private readonly IHuaTengSdk _sdk;

    public HuaTengNativeCameraService(IHuaTengSdk sdk)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
    }

    public IReadOnlyList<HuaTengCameraInfo> ListCameras() => _sdk.EnumerateDevices();

    public HuaTengCapturedFrame CaptureSnapshot(HuaTengAcquisitionSettings settings)
    {
        var camera = ResolveCamera(settings.CameraIndex);
        using var session = _sdk.OpenCamera(camera);
        var appliedRoi = ConfigureSession(session, settings);
        session.Play();
        session.SoftTrigger();
        var frame = session.GetFrame(timeoutMs: 2000);
        return ToCapturedFrame(session, settings, appliedRoi, frame);
    }

    public async Task StreamFramesAsync(
        HuaTengAcquisitionSettings settings,
        Func<HuaTengCapturedFrame, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onFrame);

        var camera = ResolveCamera(settings.CameraIndex);
        using var session = _sdk.OpenCamera(camera);
        var appliedRoi = ConfigureSession(session, settings);
        session.Play();

        if (settings.TriggerMode == HuaTengTriggerMode.Continuous)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var raw = session.GetFrame(timeoutMs: 2000);
                await onFrame(ToCapturedFrame(session, settings, appliedRoi, raw)).ConfigureAwait(false);
            }

            return;
        }

        var targetFrameRate = settings.TargetFrameRate > 0 ? settings.TargetFrameRate : 1.0;
        var targetIntervalSeconds = 1.0 / targetFrameRate;
        var nextDeadline = Stopwatch.GetTimestamp();

        while (!cancellationToken.IsCancellationRequested)
        {
            session.SoftTrigger();
            var raw = session.GetFrame(timeoutMs: 2000);
            await onFrame(ToCapturedFrame(session, settings, appliedRoi, raw)).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            nextDeadline += (long)(targetIntervalSeconds * Stopwatch.Frequency);
            var remainingTicks = nextDeadline - Stopwatch.GetTimestamp();
            if (remainingTicks > 0)
            {
                var remaining = TimeSpan.FromSeconds(remainingTicks / (double)Stopwatch.Frequency);
                if (remaining > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            else
            {
                nextDeadline = Stopwatch.GetTimestamp();
            }
        }
    }

    private HuaTengCameraInfo ResolveCamera(int cameraIndex)
    {
        var camera = _sdk.EnumerateDevices().FirstOrDefault(item => item.Index == cameraIndex);
        if (camera is null)
        {
            throw new InvalidOperationException($"HuaTeng camera index {cameraIndex} was not found.");
        }

        return camera;
    }

    private static HuaTengRoi? ConfigureSession(IHuaTengCameraSession session, HuaTengAcquisitionSettings settings)
    {
        session.SetOutputFormat(settings.PixelFormat);
        session.SetTriggerMode(settings.TriggerMode);
        session.SetExposure(settings.ExposureUs);
        return session.ApplyRoi(settings.Roi);
    }

    private static HuaTengCapturedFrame ToCapturedFrame(
        IHuaTengCameraSession session,
        HuaTengAcquisitionSettings settings,
        HuaTengRoi? appliedRoi,
        HuaTengRawFrame frame)
    {
        return new HuaTengCapturedFrame(
            session.CameraInfo,
            settings.PixelFormat,
            settings.TriggerMode,
            frame.Width,
            frame.Height,
            frame.PixelData,
            session.Capability.IsMonoSensor || settings.PixelFormat == HuaTengPixelFormat.Mono8,
            frame.TimestampTenthsOfMilliseconds,
            frame.ExposureUs,
            appliedRoi);
    }
}
