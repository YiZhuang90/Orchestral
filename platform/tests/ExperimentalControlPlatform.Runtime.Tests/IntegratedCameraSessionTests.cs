using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class IntegratedCameraSessionTests
{
    private static readonly IntegratedCameraCaptureSettings TestSettings = new("uvc-1", 0, "Integrated Camera", 20, true);

    [Fact]
    public async Task ConnectAsync_PublishesConnectedStateAndFrame()
    {
        var service = new FakeIntegratedCameraService
        {
            SnapshotFrames = [CreateFrame(320, 240, 1)]
        };
        var session = new IntegratedCameraSession(service, TestSettings.DeviceId, TestSettings.CameraIndex, TestSettings.DisplayName);

        await session.ConnectAsync(TestSettings);

        var state = Assert.IsType<IntegratedCameraSessionState>(session.State.Current);
        Assert.True(state.Connected);
        Assert.False(state.LivePreviewing);
        Assert.Equal(TestSettings, session.AppliedSettings.Current);
        var frame = Assert.IsType<IntegratedCameraFrame>(session.LatestFrame.Current);
        Assert.Equal(320, frame.Width);
    }

    [Fact]
    public async Task StartLiveAsync_PublishesFramesUntilStopped()
    {
        var service = new FakeIntegratedCameraService
        {
            SnapshotFrames = [CreateFrame(320, 240, 1)],
            StreamFrames = [CreateFrame(320, 240, 2), CreateFrame(320, 240, 3)]
        };
        var session = new IntegratedCameraSession(service, TestSettings.DeviceId, TestSettings.CameraIndex, TestSettings.DisplayName);
        var received = new List<IntegratedCameraFrame>();
        session.Frames.Produced += received.Add;

        await session.ConnectAsync(TestSettings);
        await session.StartLiveAsync();
        await WaitForConditionAsync(() => received.Count >= 3);
        await session.StopLiveAsync();

        var state = Assert.IsType<IntegratedCameraSessionState>(session.State.Current);
        Assert.False(state.LivePreviewing);
        Assert.True(received.Count >= 3);
    }

    [Fact]
    public async Task ConnectAsync_WhenCaptureFails_PublishesFailureState()
    {
        var service = new FakeIntegratedCameraService
        {
            SnapshotFrames = [CreateFrame(320, 240, 1)],
            ThrowOnSnapshot = new InvalidOperationException("camera busy")
        };
        var session = new IntegratedCameraSession(service, TestSettings.DeviceId, TestSettings.CameraIndex, TestSettings.DisplayName);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ConnectAsync(TestSettings));

        Assert.Contains("camera busy", exception.Message, StringComparison.OrdinalIgnoreCase);
        var state = Assert.IsType<IntegratedCameraSessionState>(session.State.Current);
        Assert.False(state.Connected);
        Assert.Contains("Unable to connect", state.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSettings_ReturnsInvalidResult_WhenFrameRateIsNonPositive()
    {
        var service = new FakeIntegratedCameraService
        {
            SnapshotFrames = [CreateFrame(320, 240, 1)]
        };
        var session = new IntegratedCameraSession(service, TestSettings.DeviceId, TestSettings.CameraIndex, TestSettings.DisplayName);
        var invalidSettings = TestSettings with { TargetFrameRate = 0 };

        var result = session.ValidateSettings(invalidSettings);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == "TargetFrameRate");
    }

    private static IntegratedCameraFrame CreateFrame(int width, int height, byte seed)
    {
        return new IntegratedCameraFrame(TestSettings.DeviceId, TestSettings.DisplayName, width, height, [seed, seed, seed], true, DateTime.UtcNow.Ticks);
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        var started = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - started > TimeSpan.FromSeconds(3))
            {
                throw new TimeoutException("Condition was not reached before timeout.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class FakeIntegratedCameraService : IIntegratedCameraRuntimeService
    {
        private int _snapshotIndex;

        public required IReadOnlyList<IntegratedCameraFrame> SnapshotFrames { get; init; }

        public IReadOnlyList<IntegratedCameraFrame> StreamFrames { get; init; } = [];

        public Exception? ThrowOnSnapshot { get; init; }

        public IntegratedCameraFrame CaptureSnapshot(IntegratedCameraCaptureSettings settings)
        {
            if (ThrowOnSnapshot is not null)
            {
                throw ThrowOnSnapshot;
            }

            var index = Math.Min(_snapshotIndex, SnapshotFrames.Count - 1);
            _snapshotIndex++;
            return SnapshotFrames[index];
        }

        public async Task StreamFramesAsync(
            IntegratedCameraCaptureSettings settings,
            Func<IntegratedCameraFrame, Task> onFrame,
            CancellationToken cancellationToken = default)
        {
            foreach (var frame in StreamFrames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await onFrame(frame);
            }

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
