using System;
using System.Collections.Generic;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public sealed class RunRecorder : IRunRecorder
{
    private readonly object _syncRoot = new();
    private readonly RunArtifactWriter _writer;
    private Guid? _activeRunId;
    private List<string> _runtimeEvents = [];

    public RunRecorder(string rootDirectory)
        : this(new RunArtifactWriter(rootDirectory))
    {
    }

    public RunRecorder(RunArtifactWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void BeginRun(RuntimeRunContext snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_syncRoot)
        {
            _activeRunId = snapshot.RunId;
            _runtimeEvents =
            [
                BuildStartedEvent(snapshot)
            ];
        }
    }

    public RunRecordingResult? CompleteRun(RuntimeRunContext snapshot, IReadOnlyList<IDeviceTestPanelViewModel> panels)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(panels);

        List<string> runtimeEvents;
        lock (_syncRoot)
        {
            runtimeEvents = _activeRunId == snapshot.RunId
                ? new List<string>(_runtimeEvents)
                : [];

            runtimeEvents.Add(BuildStoppedEvent(snapshot));
            _activeRunId = null;
            _runtimeEvents = [];
        }

        return _writer.Write(snapshot, panels, runtimeEvents);
    }

    private static string BuildStartedEvent(RuntimeRunContext snapshot)
    {
        var label = snapshot.RunContext?.DisplayName
            ?? snapshot.Experiment?.Experiment.Name
            ?? "runtime run";
        var startedAt = snapshot.StartedAtUtc?.ToString("O") ?? "unknown-start-time";
        return $"Started {label} at {startedAt}.";
    }

    private static string BuildStoppedEvent(RuntimeRunContext snapshot)
    {
        var stoppedAt = snapshot.StoppedAtUtc?.ToString("O") ?? "unknown-stop-time";
        return snapshot.StopReason is null
            ? $"Stopped run at {stoppedAt}."
            : $"Stopped run at {stoppedAt}: {snapshot.StopReason.Code}: {snapshot.StopReason.Message}";
    }
}
