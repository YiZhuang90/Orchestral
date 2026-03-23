using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.HuaTeng;
using Xunit;

namespace ExperimentalControlPlatform.Devices.Tests.HuaTeng;

public sealed class HuaTengNativeCameraServiceTests
{
    [Fact]
    public void CaptureSnapshot_UsesSingleSoftwareTrigger_AndReturnsFrame()
    {
        var camera = new HuaTengCameraInfo(0, "MV-SUA40GC", "HT-SUA40GC-T1V-C", "USB3.0", "044051820064", "CMOS 0.4M", 0);
        var session = new FakeCameraSession(camera, new HuaTengCameraCapability(720, 540, false));
        session.EnqueueFrame(new HuaTengRawFrame(320, 240, CreateImageBytes(320, 240), 1234, 1000));

        var service = new HuaTengNativeCameraService(new FakeSdk([camera], session));

        var result = service.CaptureSnapshot(new HuaTengAcquisitionSettings(
            camera.Index,
            HuaTengPixelFormat.Bgr8,
            HuaTengTriggerMode.Triggered,
            ExposureUs: 1000,
            TargetFrameRate: 30,
            Roi: null));

        Assert.Equal(1, session.SoftTriggerCount);
        Assert.Equal(1, session.GetFrameCount);
        Assert.Equal(320, result.Width);
        Assert.Equal(240, result.Height);
        Assert.Equal(HuaTengPixelFormat.Bgr8, result.PixelFormat);
    }

    [Fact]
    public async Task StreamFramesAsync_ReusesSession_AndTriggersUntilCancelled()
    {
        var camera = new HuaTengCameraInfo(0, "MV-SUA40GC", "HT-SUA40GC-T1V-C", "USB3.0", "044051820064", "CMOS 0.4M", 0);
        var session = new FakeCameraSession(camera, new HuaTengCameraCapability(720, 540, false));
        session.EnqueueFrame(new HuaTengRawFrame(320, 240, CreateImageBytes(320, 240), 1000, 1000));
        session.EnqueueFrame(new HuaTengRawFrame(320, 240, CreateImageBytes(320, 240), 2000, 1000));
        session.EnqueueFrame(new HuaTengRawFrame(320, 240, CreateImageBytes(320, 240), 3000, 1000));

        var service = new HuaTengNativeCameraService(new FakeSdk([camera], session));
        using var cancellation = new CancellationTokenSource();
        var frames = new List<HuaTengCapturedFrame>();

        await service.StreamFramesAsync(
            new HuaTengAcquisitionSettings(
                camera.Index,
                HuaTengPixelFormat.Bgr8,
                HuaTengTriggerMode.Triggered,
                ExposureUs: 1000,
                TargetFrameRate: 1000,
                Roi: null),
            frame =>
            {
                frames.Add(frame);
                if (frames.Count == 3)
                {
                    cancellation.Cancel();
                }

                return Task.CompletedTask;
            },
            cancellation.Token);

        Assert.Equal(3, frames.Count);
        Assert.Equal(3, session.SoftTriggerCount);
        Assert.Equal(1, session.PlayCount);
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public async Task StreamFramesAsync_InContinuousMode_DrainsFramesWithoutSoftwareTrigger()
    {
        var camera = new HuaTengCameraInfo(0, "MV-SUA40GC", "HT-SUA40GC-T1V-C", "USB3.0", "044051820064", "CMOS 0.4M", 0);
        var session = new FakeCameraSession(camera, new HuaTengCameraCapability(720, 540, false));
        session.EnqueueFrame(new HuaTengRawFrame(320, 240, CreateImageBytes(320, 240), 1000, 1000));
        session.EnqueueFrame(new HuaTengRawFrame(320, 240, CreateImageBytes(320, 240), 2000, 1000));

        var service = new HuaTengNativeCameraService(new FakeSdk([camera], session));
        using var cancellation = new CancellationTokenSource();
        var frames = new List<HuaTengCapturedFrame>();

        await service.StreamFramesAsync(
            new HuaTengAcquisitionSettings(
                camera.Index,
                HuaTengPixelFormat.Bgr8,
                HuaTengTriggerMode.Continuous,
                ExposureUs: 1000,
                TargetFrameRate: 1000,
                Roi: null),
            frame =>
            {
                frames.Add(frame);
                if (frames.Count == 2)
                {
                    cancellation.Cancel();
                }

                return Task.CompletedTask;
            },
            cancellation.Token);

        Assert.Equal(2, frames.Count);
        Assert.Equal(0, session.SoftTriggerCount);
        Assert.Equal(2, session.GetFrameCount);
        Assert.Equal(1, session.PlayCount);
        Assert.Equal(1, session.DisposeCount);
    }

    private static byte[] CreateImageBytes(int width, int height)
    {
        return Enumerable.Repeat((byte)127, width * height * 3).ToArray();
    }

    private sealed class FakeSdk : IHuaTengSdk
    {
        private readonly IReadOnlyList<HuaTengCameraInfo> _cameras;
        private readonly FakeCameraSession _session;

        public FakeSdk(IReadOnlyList<HuaTengCameraInfo> cameras, FakeCameraSession session)
        {
            _cameras = cameras;
            _session = session;
        }

        public IReadOnlyList<HuaTengCameraInfo> EnumerateDevices() => _cameras;

        public IHuaTengCameraSession OpenCamera(HuaTengCameraInfo camera) => _session;

        public string GetErrorString(int statusCode) => $"status:{statusCode}";
    }

    private sealed class FakeCameraSession : IHuaTengCameraSession
    {
        private readonly Queue<HuaTengRawFrame> _frames = new();

        public FakeCameraSession(HuaTengCameraInfo cameraInfo, HuaTengCameraCapability capability)
        {
            CameraInfo = cameraInfo;
            Capability = capability;
            AppliedRoi = null;
        }

        public HuaTengCameraInfo CameraInfo { get; }

        public HuaTengCameraCapability Capability { get; }

        public int SoftTriggerCount { get; private set; }

        public int GetFrameCount { get; private set; }

        public int PlayCount { get; private set; }

        public int DisposeCount { get; private set; }

        public HuaTengPixelFormat OutputFormat { get; private set; }

        public HuaTengTriggerMode TriggerMode { get; private set; }

        public double ExposureUs { get; private set; }

        public HuaTengRoi? AppliedRoi { get; private set; }

        public void EnqueueFrame(HuaTengRawFrame frame) => _frames.Enqueue(frame);

        public void SetOutputFormat(HuaTengPixelFormat pixelFormat) => OutputFormat = pixelFormat;

        public void SetTriggerMode(HuaTengTriggerMode triggerMode) => TriggerMode = triggerMode;

        public void SetExposure(double? exposureUs) => ExposureUs = exposureUs ?? 0;

        public double GetExposure() => ExposureUs;

        public HuaTengRoi? ApplyRoi(HuaTengRoi? roi)
        {
            AppliedRoi = roi;
            return roi;
        }

        public void Play() => PlayCount++;

        public void SoftTrigger() => SoftTriggerCount++;

        public HuaTengRawFrame GetFrame(int timeoutMs)
        {
            GetFrameCount++;
            return _frames.Dequeue();
        }

        public void Dispose() => DisposeCount++;
    }
}
