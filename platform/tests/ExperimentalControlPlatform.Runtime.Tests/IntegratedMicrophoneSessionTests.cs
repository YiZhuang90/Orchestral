using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.Audio;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class IntegratedMicrophoneSessionTests
{
    private static readonly MicrophoneDeviceInfo TestDevice = new(
        0,
        "Integrated microphone",
        "mic-1",
        48000,
        2);

    [Fact]
    public async Task ConnectAsync_CapturesInitialFrameAndPublishesConnectedState()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var settings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 50, MicrophoneChannelMode.MonoMix);

        await session.ConnectAsync(settings);

        var currentState = Assert.IsType<IntegratedMicrophoneSessionState>(session.State.Current);
        Assert.True(currentState.Connected);
        Assert.False(currentState.LiveReading);
        Assert.Equal(settings, session.AppliedSettings.Current);
        var latestFrame = Assert.IsType<MicrophoneFrame>(session.LatestFrame.Current);
        Assert.Equal(-24.5, latestFrame.RmsDbfs, 1);
        var diagnostics = Assert.IsType<DeviceDiagnosticsSnapshot>(session.Diagnostics.Current);
        Assert.Equal("Connect integrated microphone session", diagnostics.LastCommand);
        Assert.Equal(1, service.CaptureSnapshotCallCount);
    }

    [Fact]
    public async Task StartLiveAsync_PublishesFramesUntilStopped()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50)
            ],
            StreamFrames =
            [
                CreateFrame(rmsDbfs: -20.0, peakDbfs: -9.0, windowMilliseconds: 50),
                CreateFrame(rmsDbfs: -19.0, peakDbfs: -8.0, windowMilliseconds: 50)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var settings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 50, MicrophoneChannelMode.MonoMix);
        var receivedFrames = new List<MicrophoneFrame>();
        session.Frames.Produced += receivedFrames.Add;

        await session.ConnectAsync(settings);
        await session.StartLiveAsync();
        await WaitForConditionAsync(() => receivedFrames.Count >= 3);
        await session.StopLiveAsync();

        var currentState = Assert.IsType<IntegratedMicrophoneSessionState>(session.State.Current);
        Assert.False(currentState.LiveReading);
        Assert.True(receivedFrames.Count >= 3);
        Assert.Equal(-19.0, receivedFrames.Last().RmsDbfs, 1);
    }

    [Fact]
    public async Task DisconnectAsync_PublishesSessionEndSnapshot()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50)
            ],
            StreamFrames =
            [
                CreateFrame(rmsDbfs: -20.0, peakDbfs: -9.0, windowMilliseconds: 50)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var settings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 50, MicrophoneChannelMode.MonoMix);

        await session.ConnectAsync(settings);
        await session.StartLiveAsync();
        await WaitForConditionAsync(() => session.State.Current?.LiveReading == true);
        await session.DisconnectAsync(StopReason.UserRequested("Operator disconnected microphone."));

        var currentState = Assert.IsType<IntegratedMicrophoneSessionState>(session.State.Current);
        Assert.False(currentState.Connected);
        Assert.False(currentState.LiveReading);
        var sessionEnd = Assert.IsType<DeviceSessionEndSnapshot>(session.SessionEnd.Current);
        Assert.Equal("UserRequested", sessionEnd.ReasonCode);
        Assert.True(sessionEnd.LiveStopped);
    }

    [Fact]
    public async Task FrameJournal_TracksFramesIndependentlyOfPanelConsumers()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50)
            ],
            StreamFrames =
            [
                CreateFrame(rmsDbfs: -20.0, peakDbfs: -9.0, windowMilliseconds: 50),
                CreateFrame(rmsDbfs: -19.0, peakDbfs: -8.0, windowMilliseconds: 50)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        using var journal = new MicrophoneFrameJournal(session.Frames);
        var settings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 50, MicrophoneChannelMode.MonoMix);

        await session.ConnectAsync(settings);
        await session.StartLiveAsync();
        await WaitForConditionAsync(() => journal.FrameCount >= 3);
        await session.StopLiveAsync();

        Assert.True(journal.FrameCount >= 3);
        Assert.NotNull(journal.LastFrame);
        Assert.Equal(-19.0, journal.LastFrame!.RmsDbfs, 1);
    }

    [Fact]
    public async Task ReadOnceAsync_CapturesFrameAndPublishesState()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50),
                CreateFrame(rmsDbfs: -18.0, peakDbfs: -7.0, windowMilliseconds: 50)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var settings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 50, MicrophoneChannelMode.MonoMix);

        await session.ConnectAsync(settings);
        await session.ReadOnceAsync();

        Assert.Equal(2, service.CaptureSnapshotCallCount);
        var latestFrame = Assert.IsType<MicrophoneFrame>(session.LatestFrame.Current);
        Assert.Equal(-18.0, latestFrame.RmsDbfs, 1);
        var diagnostics = Assert.IsType<DeviceDiagnosticsSnapshot>(session.Diagnostics.Current);
        Assert.Equal("Captured microphone snapshot", diagnostics.LastStateTransition);
    }

    [Fact]
    public async Task ApplySettingsAsync_WhileConnected_CapturesConfirmationFrameAndPublishesAppliedSettings()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50),
                CreateFrame(rmsDbfs: -18.0, peakDbfs: -7.0, windowMilliseconds: 100)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var initialSettings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 50, MicrophoneChannelMode.MonoMix);
        var appliedSettings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 40, 100, MicrophoneChannelMode.Left);

        await session.ConnectAsync(initialSettings);
        await session.ApplySettingsAsync(appliedSettings);

        Assert.Equal(2, service.CaptureSnapshotCallCount);
        Assert.Equal(appliedSettings, session.AppliedSettings.Current);
        var currentState = Assert.IsType<IntegratedMicrophoneSessionState>(session.State.Current);
        Assert.Equal(40, currentState.TargetUpdateRateHz);
        Assert.Equal(100, currentState.WindowMilliseconds);
        Assert.Equal(MicrophoneChannelMode.Left, currentState.ChannelMode);
        var latestFrame = Assert.IsType<MicrophoneFrame>(session.LatestFrame.Current);
        Assert.Equal(-18.0, latestFrame.RmsDbfs, 1);
    }

    [Fact]
    public async Task ApplySettingsAsync_CaptureFailure_DoesNotLeaveIncorrectAppliedSettings()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50)
            ],
            FailOnSnapshotCallNumber = 2
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var initialSettings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 50, MicrophoneChannelMode.MonoMix);
        var rejectedSettings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 40, 100, MicrophoneChannelMode.Left);

        await session.ConnectAsync(initialSettings);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplySettingsAsync(rejectedSettings));

        Assert.Equal(initialSettings, session.AppliedSettings.Current);
        var currentState = Assert.IsType<IntegratedMicrophoneSessionState>(session.State.Current);
        Assert.Equal(20, currentState.TargetUpdateRateHz);
        Assert.Equal(50, currentState.WindowMilliseconds);
        Assert.Equal(MicrophoneChannelMode.MonoMix, currentState.ChannelMode);
        Assert.Contains("failed", currentState.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplySettingsAsync_WhenDisconnectedStagesSettings()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var stagedSettings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 40, 100, MicrophoneChannelMode.Right);

        await session.ApplySettingsAsync(stagedSettings);

        Assert.Equal(stagedSettings, session.AppliedSettings.Current);
        var currentState = Assert.IsType<IntegratedMicrophoneSessionState>(session.State.Current);
        Assert.Equal(40, currentState.TargetUpdateRateHz);
        Assert.Equal(100, currentState.WindowMilliseconds);
        Assert.Equal(MicrophoneChannelMode.Right, currentState.ChannelMode);
        Assert.Contains("staged", currentState.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, service.CaptureSnapshotCallCount);
    }

    [Fact]
    public void ValidateSettings_ReturnsInvalidResult_WhenWindowIsNonPositive()
    {
        var service = new FakeMicrophoneService
        {
            SnapshotFrames =
            [
                CreateFrame(rmsDbfs: -24.5, peakDbfs: -12.0, windowMilliseconds: 50)
            ]
        };
        var session = new IntegratedMicrophoneSession(service, TestDevice);
        var invalidSettings = new MicrophoneCaptureSettings(TestDevice.DeviceId, 20, 0, MicrophoneChannelMode.MonoMix);

        var result = session.ValidateSettings(invalidSettings);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == "WindowMilliseconds");
    }

    private static MicrophoneFrame CreateFrame(double rmsDbfs, double peakDbfs, int windowMilliseconds)
    {
        return new MicrophoneFrame(
            TestDevice.DeviceId,
            TestDevice.DisplayName,
            TestDevice.SampleRate,
            TestDevice.Channels,
            MicrophoneChannelMode.MonoMix,
            [0.1f, -0.2f, 0.3f, -0.4f],
            rmsDbfs,
            peakDbfs,
            false,
            DateTime.UtcNow.Ticks,
            TimeSpan.FromMilliseconds(windowMilliseconds));
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        var started = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - started > TimeSpan.FromSeconds(2))
            {
                throw new TimeoutException("Condition was not reached before timeout.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeMicrophoneService : IMicrophoneService
    {
        private int _snapshotIndex;

        public required IReadOnlyList<MicrophoneFrame> SnapshotFrames { get; init; }

        public IReadOnlyList<MicrophoneFrame> StreamFrames { get; init; } = [];

        public int? FailOnSnapshotCallNumber { get; init; }

        public int CaptureSnapshotCallCount { get; private set; }

        public IReadOnlyList<MicrophoneDeviceInfo> ListCaptureDevices() => [TestDevice];

        public MicrophoneFrame CaptureSnapshot(MicrophoneCaptureSettings settings)
        {
            CaptureSnapshotCallCount++;
            if (FailOnSnapshotCallNumber == CaptureSnapshotCallCount)
            {
                throw new InvalidOperationException($"Snapshot failure on call {CaptureSnapshotCallCount}.");
            }

            var index = Math.Min(_snapshotIndex, SnapshotFrames.Count - 1);
            _snapshotIndex++;
            return SnapshotFrames[index];
        }

        public async Task StreamFramesAsync(
            MicrophoneCaptureSettings settings,
            Func<MicrophoneFrame, Task> onFrame,
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
