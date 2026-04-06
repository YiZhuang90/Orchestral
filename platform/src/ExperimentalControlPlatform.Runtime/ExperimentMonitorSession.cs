using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Runtime;

public sealed class ExperimentMonitorSession : IAsyncDisposable
{
    private static readonly TimeSpan DefaultSourceStaleThreshold = TimeSpan.FromSeconds(3);
    private readonly object _syncRoot = new();
    private readonly IRuntimeCoordinator _runtimeCoordinator;
    private readonly IDeviceSessionRegistry? _sessionRegistry;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _sourceStaleThreshold;
    private readonly SnapshotOutputPort<ExperimentMonitorSnapshot> _snapshot = new(new ExperimentMonitorSnapshot());
    private IReadOnlyList<IExperimentMonitorSource> _sources;
    private ControllerUnitSession? _controller;
    private IDerivedStateSession? _derivedState;
    private bool _disposed;

    public ExperimentMonitorSession(
        IRuntimeCoordinator runtimeCoordinator,
        IEnumerable<IExperimentMonitorSource> sources,
        Func<DateTimeOffset>? clock = null,
        TimeSpan? sourceStaleThreshold = null)
    {
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        ArgumentNullException.ThrowIfNull(sources);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _sourceStaleThreshold = sourceStaleThreshold ?? DefaultSourceStaleThreshold;
        _sources = [];

        _runtimeCoordinator.SnapshotChanged += HandleCoordinatorChanged;
        ReplaceSources(sources.ToArray());
        RecomputeSnapshot();
    }

    public ExperimentMonitorSession(
        IRuntimeCoordinator runtimeCoordinator,
        IDeviceSessionRegistry sessionRegistry,
        Func<DateTimeOffset>? clock = null,
        TimeSpan? sourceStaleThreshold = null)
        : this(runtimeCoordinator, Array.Empty<IExperimentMonitorSource>(), clock, sourceStaleThreshold)
    {
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        _sessionRegistry.SessionsChanged += HandleRegistryChanged;
        ReplaceSources(CreateSources(_sessionRegistry.Sessions));
        RecomputeSnapshot();
    }

    public ISnapshotOutputPort<ExperimentMonitorSnapshot> Snapshot => _snapshot;

    public void AttachController(ControllerUnitSession? controller)
    {
        lock (_syncRoot)
        {
            if (ReferenceEquals(_controller, controller))
            {
                return;
            }

            if (_controller is not null)
            {
                _controller.State.Changed -= HandleControllerStateChanged;
            }

            _controller = controller;
            if (_controller is not null)
            {
                _controller.State.Changed += HandleControllerStateChanged;
            }
        }

        RecomputeSnapshot();
    }

    public void AttachDerivedState(IDerivedStateSession? derivedState)
    {
        lock (_syncRoot)
        {
            if (ReferenceEquals(_derivedState, derivedState))
            {
                return;
            }

            if (_derivedState is not null)
            {
                _derivedState.DerivedStateChanged -= HandleDerivedStateChanged;
            }

            _derivedState = derivedState;
            if (_derivedState is not null)
            {
                _derivedState.DerivedStateChanged += HandleDerivedStateChanged;
            }
        }

        RecomputeSnapshot();
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _runtimeCoordinator.SnapshotChanged -= HandleCoordinatorChanged;
        if (_sessionRegistry is not null)
        {
            _sessionRegistry.SessionsChanged -= HandleRegistryChanged;
        }

        lock (_syncRoot)
        {
            if (_controller is not null)
            {
                _controller.State.Changed -= HandleControllerStateChanged;
            }

            if (_derivedState is not null)
            {
                _derivedState.DerivedStateChanged -= HandleDerivedStateChanged;
            }
        }

        ReplaceSources(Array.Empty<IExperimentMonitorSource>());
        return ValueTask.CompletedTask;
    }

    private void HandleCoordinatorChanged(RuntimeRunContext _)
    {
        RecomputeSnapshot();
    }

    private void HandleRegistryChanged()
    {
        if (_sessionRegistry is null)
        {
            return;
        }

        ReplaceSources(CreateSources(_sessionRegistry.Sessions));
        RecomputeSnapshot();
    }

    private void HandleControllerStateChanged(ControllerUnitState _)
    {
        RecomputeSnapshot();
    }

    private void HandleDerivedStateChanged()
    {
        RecomputeSnapshot();
    }

    private void HandleSourceChanged()
    {
        RecomputeSnapshot();
    }

    private void ReplaceSources(IReadOnlyList<IExperimentMonitorSource> newSources)
    {
        IReadOnlyList<IExperimentMonitorSource> oldSources;
        lock (_syncRoot)
        {
            oldSources = _sources;
            foreach (var source in oldSources)
            {
                source.Changed -= HandleSourceChanged;
            }

            _sources = newSources;
            foreach (var source in _sources)
            {
                source.Changed += HandleSourceChanged;
            }
        }

        foreach (var source in oldSources)
        {
            source.Dispose();
        }
    }

    private void RecomputeSnapshot()
    {
        if (_disposed)
        {
            return;
        }

        ControllerUnitState? controllerState;
        IDerivedStateSnapshot? derivedStateSnapshot;
        IReadOnlyList<IExperimentMonitorSource> sources;
        lock (_syncRoot)
        {
            controllerState = _controller?.State.Current;
            derivedStateSnapshot = _derivedState?.CurrentDerivedState;
            sources = _sources;
        }

        var observedAtUtc = _clock();
        var runtimeSnapshot = _runtimeCoordinator.LatestSnapshot;
        var deviceSnapshots = sources
            .Select(static source => source.CreateSnapshot())
            .OrderBy(static snapshot => snapshot.DisplayName, StringComparer.Ordinal)
            .ToArray();
        var items = BuildItems(runtimeSnapshot, controllerState, derivedStateSnapshot, deviceSnapshots, observedAtUtc);
        var warningCount = items.Count(static item => item.Severity == ExperimentMonitorSeverity.Warning);
        var alarmCount = items.Count(static item => item.Severity == ExperimentMonitorSeverity.Alarm);
        var highestSeverity = items.Count == 0
            ? ExperimentMonitorSeverity.None
            : items.Max(static item => item.Severity);

        _snapshot.Publish(new ExperimentMonitorSnapshot
        {
            RunId = runtimeSnapshot.RunId,
            RunState = runtimeSnapshot.State,
            ObservedAtUtc = observedAtUtc,
            RunDisplayName = ResolveRunDisplayName(runtimeSnapshot),
            StateSummary = BuildStateSummary(runtimeSnapshot, highestSeverity, warningCount, alarmCount),
            HighestSeverity = highestSeverity,
            WarningCount = warningCount,
            AlarmCount = alarmCount,
            Items = items,
            Devices = deviceSnapshots,
            PrimaryControlLabel = controllerState?.ControlTargetName,
            PrimaryControlTargetValue = controllerState?.TargetValue,
            PrimaryControlMeasuredValue = controllerState?.MeasuredValue,
            PrimaryControlErrorValue = controllerState?.ErrorValue,
            PrimaryControlMeasuredValueIsStale = controllerState?.MeasuredValueIsStale ?? false,
            PrimaryControlSummary = BuildPrimaryControlSummary(controllerState),
            DerivedFlowRateLitersPerMinute = derivedStateSnapshot?.FilteredFlowRateLitersPerMinute,
            DerivedReynoldsNumber = derivedStateSnapshot?.ReynoldsNumber,
            DerivedMeanTemperatureC = derivedStateSnapshot?.MeanTemperatureC,
            DerivedStateIsStale = IsDerivedStateStale(derivedStateSnapshot, observedAtUtc),
            DerivedStateSummary = BuildDerivedStateSummary(derivedStateSnapshot, observedAtUtc)
        });
    }

    private IReadOnlyList<ExperimentMonitorItem> BuildItems(
        RuntimeRunContext runtimeSnapshot,
        ControllerUnitState? controllerState,
        IDerivedStateSnapshot? derivedStateSnapshot,
        IReadOnlyList<ExperimentMonitorDeviceSnapshot> deviceSnapshots,
        DateTimeOffset observedAtUtc)
    {
        var items = new List<ExperimentMonitorItem>();

        if (runtimeSnapshot.State == RunState.Stopping && runtimeSnapshot.StopReason is not null)
        {
            items.Add(new ExperimentMonitorItem(
                "info.runtime_stopping",
                ExperimentMonitorSeverity.Info,
                "runtime",
                runtimeSnapshot.StopReason.Message,
                observedAtUtc));
        }

        foreach (var device in deviceSnapshots)
        {
            if (runtimeSnapshot.State == RunState.Running && !device.Connected)
            {
                items.Add(new ExperimentMonitorItem(
                    $"{GetSeverityToken(device.IsCriticalControl)}.{device.SourceId}.disconnected",
                    device.IsCriticalControl ? ExperimentMonitorSeverity.Alarm : ExperimentMonitorSeverity.Warning,
                    device.SourceId,
                    $"{device.DisplayName} is disconnected while the run is active.",
                    observedAtUtc));
            }

            if (!string.IsNullOrWhiteSpace(device.LastError))
            {
                items.Add(new ExperimentMonitorItem(
                    $"{GetSeverityToken(device.IsCriticalControl)}.{device.SourceId}.error",
                    device.IsCriticalControl ? ExperimentMonitorSeverity.Alarm : ExperimentMonitorSeverity.Warning,
                    device.SourceId,
                    $"{device.DisplayName}: {device.LastError}",
                    observedAtUtc));
            }

            if (runtimeSnapshot.State == RunState.Running
                && device.LiveActive
                && device.LastObservedAtUtc.HasValue
                && observedAtUtc - device.LastObservedAtUtc.Value > _sourceStaleThreshold)
            {
                items.Add(new ExperimentMonitorItem(
                    $"warn.{device.SourceId}.stale",
                    ExperimentMonitorSeverity.Warning,
                    device.SourceId,
                    $"{device.DisplayName} stream is stale.",
                    observedAtUtc));
            }
        }

        if (runtimeSnapshot.State == RunState.Running
            && controllerState is not null
            && controllerState.MeasuredValueIsStale
            && controllerState.MeasuredValueObservedAtUtc.HasValue)
        {
            var controllerSource = BuildControllerSourceId(controllerState.ControlTargetId.Value);
            items.Add(new ExperimentMonitorItem(
                $"warn.{controllerSource}",
                ExperimentMonitorSeverity.Warning,
                controllerSource,
                $"{controllerState.ControlTargetName} measured value is stale.",
                observedAtUtc));
        }

        if (runtimeSnapshot.State == RunState.Running
            && IsDerivedStateStale(derivedStateSnapshot, observedAtUtc))
        {
            items.Add(new ExperimentMonitorItem(
                "warn.derived.reynolds_number.stale",
                ExperimentMonitorSeverity.Warning,
                "derived.reynolds_number",
                "Derived Reynolds state is stale.",
                observedAtUtc));
        }

        return items
            .OrderByDescending(static item => item.Severity)
            .ThenBy(static item => item.Source, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ResolveRunDisplayName(RuntimeRunContext runtimeSnapshot)
    {
        if (!string.IsNullOrWhiteSpace(runtimeSnapshot.RunContext?.DisplayName))
        {
            return runtimeSnapshot.RunContext.DisplayName!;
        }

        if (!string.IsNullOrWhiteSpace(runtimeSnapshot.Experiment?.Experiment.Name))
        {
            return runtimeSnapshot.Experiment.Experiment.Name;
        }

        return "Experiment Monitor";
    }

    private static string BuildStateSummary(
        RuntimeRunContext runtimeSnapshot,
        ExperimentMonitorSeverity highestSeverity,
        int warningCount,
        int alarmCount)
    {
        return runtimeSnapshot.State switch
        {
            RunState.Running when highestSeverity == ExperimentMonitorSeverity.Alarm => $"Running with {alarmCount} alarm(s).",
            RunState.Running when highestSeverity == ExperimentMonitorSeverity.Warning => $"Running with {warningCount} warning(s).",
            RunState.Running => "Running healthy.",
            RunState.Stopping when runtimeSnapshot.StopReason is not null => $"Stopping: {runtimeSnapshot.StopReason.Message}",
            RunState.Idle when runtimeSnapshot.StopReason is not null => $"Ready after stop: {runtimeSnapshot.StopReason.Message}",
            _ => "Ready to initialize."
        };
    }

    private static string BuildPrimaryControlSummary(ControllerUnitState? controllerState)
    {
        if (controllerState is null || !controllerState.TargetValue.HasValue)
        {
            return "No active control target.";
        }

        if (!controllerState.MeasuredValue.HasValue)
        {
            return $"{controllerState.ControlTargetName} {controllerState.TargetValue.Value:0.###} (awaiting measured value)";
        }

        return controllerState.MeasuredValueIsStale
            ? $"{controllerState.ControlTargetName} {controllerState.TargetValue.Value:0.###} +/- {controllerState.ErrorValue:0.###} (stale)"
            : $"{controllerState.ControlTargetName} {controllerState.TargetValue.Value:0.###} +/- {controllerState.ErrorValue:0.###}";
    }

    private string BuildDerivedStateSummary(IDerivedStateSnapshot? derivedStateSnapshot, DateTimeOffset observedAtUtc)
    {
        if (derivedStateSnapshot is null || !derivedStateSnapshot.ReynoldsNumber.HasValue)
        {
            return "No derived flow state.";
        }

        var staleSuffix = IsDerivedStateStale(derivedStateSnapshot, observedAtUtc) ? " (stale)" : string.Empty;
        var temperatureSuffix = derivedStateSnapshot.UsesFallbackTemperature ? " (fallback temperature)" : string.Empty;
        return $"Re {derivedStateSnapshot.ReynoldsNumber.Value:0.###}, {derivedStateSnapshot.FilteredFlowRateLitersPerMinute!.Value:0.###} L/min, {derivedStateSnapshot.MeanTemperatureC!.Value:0.###} C{temperatureSuffix}{staleSuffix}";
    }

    private bool IsDerivedStateStale(IDerivedStateSnapshot? derivedStateSnapshot, DateTimeOffset observedAtUtc)
    {
        if (derivedStateSnapshot?.ObservedAtUtc is null)
        {
            return false;
        }

        return observedAtUtc - derivedStateSnapshot.ObservedAtUtc.Value > _sourceStaleThreshold;
    }

    private static string GetSeverityToken(bool criticalControl) => criticalControl ? "alarm" : "warn";

    private static string BuildControllerSourceId(string controlTargetId)
    {
        const string prefix = "control.";
        if (controlTargetId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return $"controller.{controlTargetId[prefix.Length..]}";
        }

        return $"controller.{controlTargetId}";
    }

    private static IReadOnlyList<IExperimentMonitorSource> CreateSources(IEnumerable<IDeviceSession> sessions)
    {
        return sessions
            .Select(static session => CreateSource(session))
            .Where(static source => source is not null)
            .Cast<IExperimentMonitorSource>()
            .ToArray();
    }

    private static IExperimentMonitorSource? CreateSource(IDeviceSession session)
    {
        return session switch
        {
            ControlCenterSession controlCenter => new SnapshotMonitorSource<ControlCenterSessionState>(
                controlCenter.State,
                controlCenter.Diagnostics,
                static (state, diagnostics) => new ExperimentMonitorDeviceSnapshot
                {
                    SourceId = $"session.{buildSourceId(state.DeviceId)}",
                    DisplayName = state.DeviceName,
                    DeviceId = state.DeviceId,
                    SessionFamily = "ControlCenter",
                    Connected = state.Connected,
                    Busy = state.Busy,
                    LiveActive = false,
                    IsCriticalControl = true,
                    StatusMessage = state.StatusMessage,
                    LastError = diagnostics?.LastError,
                    LastObservedAtUtc = state.LastPulseCapturedAt
                }),
            Pt104Session pt104 => new SnapshotMonitorSource<Pt104SessionState>(
                pt104.State,
                pt104.Diagnostics,
                static (state, diagnostics) => new ExperimentMonitorDeviceSnapshot
                {
                    SourceId = $"session.{buildSourceId(state.DeviceId)}",
                    DisplayName = state.DeviceName,
                    DeviceId = state.DeviceId,
                    SessionFamily = "Pt104",
                    Connected = state.Connected,
                    Busy = state.Busy,
                    LiveActive = state.Channels.Values.Any(static channel => channel.LiveReading),
                    IsCriticalControl = false,
                    StatusMessage = state.StatusMessage,
                    LastError = diagnostics?.LastError,
                    LastObservedAtUtc = state.Channels.Values
                        .Where(static channel => channel.LastSampleTimestamp.HasValue)
                        .Select(static channel => channel.LastSampleTimestamp)
                        .Max()
                }),
            IntegratedCameraSession integratedCamera => new SnapshotMonitorSource<IntegratedCameraSessionState>(
                integratedCamera.State,
                integratedCamera.Diagnostics,
                static (state, diagnostics) => new ExperimentMonitorDeviceSnapshot
                {
                    SourceId = $"session.{buildSourceId(state.DeviceId)}",
                    DisplayName = state.DeviceName,
                    DeviceId = state.DeviceId,
                    SessionFamily = "IntegratedCamera",
                    Connected = state.Connected,
                    Busy = state.Busy,
                    LiveActive = state.LivePreviewing,
                    IsCriticalControl = false,
                    StatusMessage = state.StatusMessage,
                    LastError = diagnostics?.LastError,
                    LastObservedAtUtc = state.LastFrameCapturedAt
                }),
            HuaTengCameraSession huaTeng => new SnapshotMonitorSource<HuaTengSessionState>(
                huaTeng.State,
                huaTeng.Diagnostics,
                static (state, diagnostics) => new ExperimentMonitorDeviceSnapshot
                {
                    SourceId = $"session.{buildSourceId(state.DeviceId)}",
                    DisplayName = state.DeviceName,
                    DeviceId = state.DeviceId,
                    SessionFamily = "HuaTengCamera",
                    Connected = state.Connected,
                    Busy = state.Busy,
                    LiveActive = state.LivePreviewing,
                    IsCriticalControl = false,
                    StatusMessage = state.StatusMessage,
                    LastError = diagnostics?.LastError,
                    LastObservedAtUtc = state.LastFrameCapturedAt
                }),
            IntegratedMicrophoneSession microphone => new SnapshotMonitorSource<IntegratedMicrophoneSessionState>(
                microphone.State,
                microphone.Diagnostics,
                static (state, diagnostics) => new ExperimentMonitorDeviceSnapshot
                {
                    SourceId = $"session.{buildSourceId(state.DeviceId)}",
                    DisplayName = state.DeviceName,
                    DeviceId = state.DeviceId,
                    SessionFamily = "IntegratedMicrophone",
                    Connected = state.Connected,
                    Busy = state.Busy,
                    LiveActive = state.LiveReading,
                    IsCriticalControl = false,
                    StatusMessage = state.StatusMessage,
                    LastError = diagnostics?.LastError,
                    LastObservedAtUtc = state.LastFrameCapturedAt
                }),
            _ => null
        };

        static string buildSourceId(string value) =>
            value.Replace(" ", "_", StringComparison.Ordinal).ToLowerInvariant();
    }

    private sealed class SnapshotMonitorSource<TState> : IExperimentMonitorSource
    {
        private readonly ISnapshotOutputPort<TState> _statePort;
        private readonly ISnapshotOutputPort<DeviceDiagnosticsSnapshot> _diagnosticsPort;
        private readonly Func<TState, DeviceDiagnosticsSnapshot?, ExperimentMonitorDeviceSnapshot> _snapshotFactory;

        public SnapshotMonitorSource(
            ISnapshotOutputPort<TState> statePort,
            ISnapshotOutputPort<DeviceDiagnosticsSnapshot> diagnosticsPort,
            Func<TState, DeviceDiagnosticsSnapshot?, ExperimentMonitorDeviceSnapshot> snapshotFactory)
        {
            _statePort = statePort;
            _diagnosticsPort = diagnosticsPort;
            _snapshotFactory = snapshotFactory;
            _statePort.Changed += HandleChanged;
            _diagnosticsPort.Changed += HandleDiagnosticsChanged;
        }

        public event Action? Changed;

        public ExperimentMonitorDeviceSnapshot CreateSnapshot()
        {
            var state = _statePort.Current ?? throw new InvalidOperationException("Monitor source state is unavailable.");
            return _snapshotFactory(state, _diagnosticsPort.Current);
        }

        public void Dispose()
        {
            _statePort.Changed -= HandleChanged;
            _diagnosticsPort.Changed -= HandleDiagnosticsChanged;
        }

        private void HandleChanged(TState _)
        {
            Changed?.Invoke();
        }

        private void HandleDiagnosticsChanged(DeviceDiagnosticsSnapshot _)
        {
            Changed?.Invoke();
        }
    }
}
