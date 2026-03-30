using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public sealed class MainViewModel : IDisposable
{
    private readonly IRuntimeCoordinator _runtimeCoordinator;
    private readonly IRunRecorder _runRecorder;

    public MainViewModel(
        IRuntimeCoordinator runtimeCoordinator,
        IEnumerable<IDeviceTestPanelViewModel> devicePanels,
        IRunRecorder? runRecorder = null)
    {
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        ArgumentNullException.ThrowIfNull(devicePanels);
        _runRecorder = runRecorder ?? new NullRunRecorder();
        RuntimeStatus = new RuntimeStatusViewModel(_runtimeCoordinator.LatestSnapshot);
        DevicePanels = new ReadOnlyCollection<IDeviceTestPanelViewModel>(new List<IDeviceTestPanelViewModel>(devicePanels));
    }

    public string ProductName => "Orchestral";

    public string HeaderTitle => DevicePanels.Count == 0
        ? ProductName
        : $"{ProductName} -- {string.Join(" / ", DevicePanels.Select(panel => panel.Title))}";

    public RuntimeStatusViewModel RuntimeStatus { get; }

    public IReadOnlyList<IDeviceTestPanelViewModel> DevicePanels { get; }

    public IDeviceTestPanelViewModel? CurrentDevicePanel => DevicePanels.FirstOrDefault();

    public RunRecordingResult? LastRunRecording { get; private set; }

    public void StartRuntime()
    {
        try
        {
            var started = _runtimeCoordinator.Start();
            _runRecorder.BeginRun(started);
            RuntimeStatus.Update(started);
        }
        catch (Exception ex)
        {
            RuntimeStatus.ShowOperationError("Unable to start runtime.", ex.Message);
        }
    }

    public void StopRuntime()
    {
        _ = StopRuntimeAsync();
    }

    public Task StopRuntimeAsync() =>
        StopRuntimeCoreAsync(
            StopReason.UserRequested("Stopped from app shell placeholder control."),
            ensureStopped: false,
            errorPrefix: "Unable to stop runtime.");

    public Task EnsureRuntimeStoppedAsync(string reasonMessage) =>
        StopRuntimeCoreAsync(
            StopReason.UserRequested(reasonMessage),
            ensureStopped: true,
            errorPrefix: "Unable to finalize runtime stop.");

    public void FinalizeRunRecordingForLatestStoppedRun()
    {
        var snapshot = _runtimeCoordinator.LatestSnapshot;
        if (snapshot.State is not RunState.Idle || snapshot.StoppedAtUtc is null)
        {
            return;
        }

        FinalizeRunRecording(snapshot);
    }

    private async Task StopRuntimeCoreAsync(StopReason reason, bool ensureStopped, string errorPrefix)
    {
        try
        {
            var stopTask = ensureStopped
                ? _runtimeCoordinator.EnsureStoppedAsync(reason)
                : _runtimeCoordinator.RequestStopAsync(reason);
            RuntimeStatus.Update(_runtimeCoordinator.LatestSnapshot);
            var stopped = await stopTask;
            RuntimeStatus.Update(stopped);
            FinalizeRunRecording(stopped);
        }
        catch (Exception ex)
        {
            RuntimeStatus.Update(_runtimeCoordinator.LatestSnapshot);
            RuntimeStatus.ShowOperationError(errorPrefix, ex.Message);
        }
    }

    private void FinalizeRunRecording(RuntimeRunContext stopped)
    {
        if (LastRunRecording?.RunId == stopped.RunId)
        {
            return;
        }

        LastRunRecording = _runRecorder.CompleteRun(stopped, DevicePanels);
    }

    public void Dispose()
    {
        foreach (var panel in DevicePanels)
        {
            panel.Dispose();
        }
    }

    private sealed class NullRunRecorder : IRunRecorder
    {
        public void BeginRun(RuntimeRunContext snapshot)
        {
        }

        public RunRecordingResult? CompleteRun(RuntimeRunContext snapshot, IReadOnlyList<IDeviceTestPanelViewModel> panels) => null;
    }
}
