using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class HuaTengCameraSessionTests
{
    private static readonly HuaTengCaptureSettings TestSettings = new(
        "ht-1",
        0,
        "HuaTeng",
        "Mono8",
        "Triggered",
        "Neutral",
        1000,
        3,
        null);

    [Fact]
    public async Task ConnectAsync_PublishesConnectedStateAndFrame()
    {
        var service = new FakeHuaTengService
        {
            SnapshotFrames = [CreateFrame(ok: true, summary: "Connected", timestampTenths: 10)]
        };
        var session = new HuaTengCameraSession(service, TestSettings.DeviceId, TestSettings.CameraIndex, TestSettings.DisplayName);

        await session.ConnectAsync(TestSettings);

        var state = Assert.IsType<HuaTengSessionState>(session.State.Current);
        Assert.True(state.Connected);
        Assert.Equal("Mono8", state.PixelFormat);
        var frame = Assert.IsType<HuaTengFrame>(session.LatestFrame.Current);
        Assert.True(frame.Ok);
    }

    [Fact]
    public async Task ApplyRoiAsync_UpdatesAppliedRoi()
    {
        var service = new FakeHuaTengService
        {
            SnapshotFrames =
            [
                CreateFrame(ok: true, summary: "Connected", timestampTenths: 10),
                CreateFrame(ok: true, summary: "ROI applied", timestampTenths: 20, roi: new CaptureRegion(10, 20, 30, 40))
            ]
        };
        var session = new HuaTengCameraSession(service, TestSettings.DeviceId, TestSettings.CameraIndex, TestSettings.DisplayName);

        await session.ConnectAsync(TestSettings);
        await session.ApplyRoiAsync(new CaptureRegion(10, 20, 30, 40));

        var applied = Assert.IsType<HuaTengCaptureSettings>(session.AppliedSettings.Current);
        Assert.NotNull(applied.Roi);
        Assert.Equal(10, applied.Roi!.X);
        var state = Assert.IsType<HuaTengSessionState>(session.State.Current);
        Assert.NotNull(state.AppliedRoi);
        Assert.Equal(40, state.AppliedRoi!.Height);
    }

    private static HuaTengFrame CreateFrame(bool ok, string summary, int timestampTenths, CaptureRegion? roi = null)
    {
        return new HuaTengFrame(
            TestSettings.DeviceId,
            TestSettings.DisplayName,
            ok,
            summary,
            320,
            240,
            [1, 2, 3],
            "Mono8",
            TestSettings.TriggerMode,
            TestSettings.ColorTone,
            TestSettings.ExposureUs ?? 0,
            true,
            timestampTenths,
            roi,
            ["ok"]);
    }

    private sealed class FakeHuaTengService : IHuaTengCameraRuntimeService
    {
        private int _snapshotIndex;

        public required IReadOnlyList<HuaTengFrame> SnapshotFrames { get; init; }

        public IReadOnlyList<HuaTengFrame> StreamFrames { get; init; } = [];

        public Task<HuaTengFrame> CaptureSnapshotAsync(HuaTengCaptureSettings settings, CancellationToken cancellationToken = default)
        {
            var index = Math.Min(_snapshotIndex, SnapshotFrames.Count - 1);
            _snapshotIndex++;
            return Task.FromResult(SnapshotFrames[index]);
        }

        public async Task StreamFramesAsync(
            HuaTengCaptureSettings settings,
            Func<HuaTengFrame, Task> onFrame,
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
