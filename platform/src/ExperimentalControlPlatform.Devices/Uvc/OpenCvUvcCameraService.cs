using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace ExperimentalControlPlatform.Devices.Uvc;

[SupportedOSPlatform("windows")]
public sealed class OpenCvUvcCameraService : IUvcCameraService
{
    private const int MaxProbeIndex = 5;

    public IReadOnlyList<UvcCameraInfo> ListCameras()
    {
        var names = GetCameraNames();
        var cameras = new List<UvcCameraInfo>();
        var nameIndex = 0;

        for (var index = 0; index <= MaxProbeIndex; index++)
        {
            using var capture = OpenCapture(index);
            if (!capture.IsOpened())
            {
                continue;
            }

            var displayName = nameIndex < names.Count
                ? names[nameIndex++].DisplayName
                : $"Camera {index}";

            cameras.Add(new UvcCameraInfo(index, displayName));
        }

        return cameras;
    }

    public UvcFrame CaptureSnapshot(UvcAcquisitionSettings settings)
    {
        using var capture = OpenConfiguredCapture(settings);
        return ReadStableFrame(capture);
    }

    public async Task StreamFramesAsync(
        UvcAcquisitionSettings settings,
        Func<UvcFrame, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onFrame);

        using var capture = OpenConfiguredCapture(settings);
        var targetFrameRate = settings.TargetFrameRate > 0 ? settings.TargetFrameRate : 20.0;
        var targetIntervalSeconds = 1.0 / targetFrameRate;
        var nextDeadline = Stopwatch.GetTimestamp();

        while (!cancellationToken.IsCancellationRequested)
        {
            var frame = ReadFrame(capture);
            await onFrame(frame).ConfigureAwait(false);

            nextDeadline += (long)(targetIntervalSeconds * Stopwatch.Frequency);
            var remainingTicks = nextDeadline - Stopwatch.GetTimestamp();
            if (remainingTicks > 0)
            {
                var remaining = TimeSpan.FromSeconds(remainingTicks / (double)Stopwatch.Frequency);
                try
                {
                    await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
            else
            {
                nextDeadline = Stopwatch.GetTimestamp();
            }
        }
    }

    private static VideoCapture OpenConfiguredCapture(UvcAcquisitionSettings settings)
    {
        var capture = OpenCapture(settings.CameraIndex);
        if (!capture.IsOpened())
        {
            capture.Dispose();
            throw new InvalidOperationException($"Integrated camera index {settings.CameraIndex} could not be opened.");
        }

        if (settings.TargetFrameRate > 0)
        {
            capture.Set(VideoCaptureProperties.Fps, settings.TargetFrameRate);
        }

        capture.Set(VideoCaptureProperties.ConvertRgb, settings.Color ? 1 : 0);
        return capture;
    }

    private static VideoCapture OpenCapture(int index)
    {
        var capture = new VideoCapture(index, VideoCaptureAPIs.DSHOW);
        if (!capture.IsOpened())
        {
            capture.Dispose();
            capture = new VideoCapture(index, VideoCaptureAPIs.MSMF);
        }

        return capture;
    }

    private static UvcFrame ReadStableFrame(VideoCapture capture)
    {
        UvcFrame? latest = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            latest = ReadFrame(capture);
            if (latest.Width > 0 && latest.Height > 0)
            {
                return latest;
            }
        }

        throw new InvalidOperationException("Integrated camera did not return a valid frame.");
    }

    private static UvcFrame ReadFrame(VideoCapture capture)
    {
        using var mat = new Mat();
        if (!capture.Read(mat) || mat.Empty())
        {
            throw new InvalidOperationException("Integrated camera frame read failed.");
        }

        Mat output = mat;
        if (mat.Channels() == 3)
        {
            output = new Mat();
            Cv2.CvtColor(mat, output, ColorConversionCodes.BGR2RGB);
        }
        else if (mat.Channels() == 1)
        {
            output = mat.Clone();
        }

        try
        {
            var bytes = new byte[checked(output.Rows * output.Cols * output.ElemSize())];
            Marshal.Copy(output.Data, bytes, 0, bytes.Length);
            return new UvcFrame(output.Cols, output.Rows, bytes, output.Channels() != 1, Stopwatch.GetTimestamp());
        }
        finally
        {
            if (!ReferenceEquals(output, mat))
            {
                output.Dispose();
            }
        }
    }

    private static IReadOnlyList<UvcCameraInfo> GetCameraNames()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DeviceID, Manufacturer, PNPClass FROM Win32_PnPEntity WHERE PNPClass='Camera'");

            return searcher.Get()
                .Cast<ManagementObject>()
                .Select(item => new UvcCameraInfo(
                    Index: -1,
                    DisplayName: item["Name"]?.ToString() ?? "Camera",
                    InstanceId: item["DeviceID"]?.ToString(),
                    Manufacturer: item["Manufacturer"]?.ToString()))
                .Where(item => !item.DisplayName.Contains("IR Camera", StringComparison.OrdinalIgnoreCase))
                .Where(item => !item.DisplayName.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
