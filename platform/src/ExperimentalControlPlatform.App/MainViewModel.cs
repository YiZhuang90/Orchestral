using System.Globalization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.ExperimentMonitor;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public sealed class MainViewModel : IDisposable
{
    private readonly IDeviceSessionRegistry _sessionRegistry;
    private readonly IRuntimeCoordinator _runtimeCoordinator;
    private readonly IRunRecorder _runRecorder;
    private readonly AdHocRunDefinitionFactory _adHocRunDefinitionFactory;
    private readonly ExperimentMonitorSession _monitorSession;
    private readonly ExperimentMonitorPanelViewModel _experimentMonitorPanel;
    private ControllerUnitSession? _controllerUnitSession;
    private RunContextDefinition? _preparedRunContext;

    public MainViewModel(
        IRuntimeCoordinator runtimeCoordinator,
        IDeviceSessionRegistry sessionRegistry,
        IEnumerable<IDeviceTestPanelViewModel> devicePanels,
        IRunRecorder? runRecorder = null,
        AdHocRunDefinitionFactory? adHocRunDefinitionFactory = null)
    {
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        ArgumentNullException.ThrowIfNull(devicePanels);
        _runRecorder = runRecorder ?? new NullRunRecorder();
        _adHocRunDefinitionFactory = adHocRunDefinitionFactory ?? new AdHocRunDefinitionFactory();
        RuntimeStatus = new RuntimeStatusViewModel(_runtimeCoordinator.LatestSnapshot);
        DevicePanels = new ReadOnlyCollection<IDeviceTestPanelViewModel>(new List<IDeviceTestPanelViewModel>(devicePanels));
        _monitorSession = new ExperimentMonitorSession(_runtimeCoordinator, _sessionRegistry);
        _experimentMonitorPanel = new ExperimentMonitorPanelViewModel(
            _monitorSession.Snapshot,
            InitializeRuntimeAsync,
            StartRuntimeAsync,
            StopRuntimeAsync,
            () => EnsureRuntimeStoppedAsync("Experiment monitor closed."));
    }

    public string ProductName => "Orchestral";

    public string HeaderTitle => $"{ProductName} -- {_experimentMonitorPanel.Title}";

    public RuntimeStatusViewModel RuntimeStatus { get; }

    public IReadOnlyList<IDeviceTestPanelViewModel> DevicePanels { get; }

    public ExperimentMonitorPanelViewModel ExperimentMonitorPanel => _experimentMonitorPanel;

    public IDeviceTestPanelViewModel CurrentDevicePanel => _experimentMonitorPanel;

    public RunRecordingResult? LastRunRecording { get; private set; }

    public void StartRuntime()
    {
        _ = StartRuntimeAsync();
    }

    public Task<string?> InitializeRuntimeAsync(string runIndex, string? primaryTargetDraft, string? operatorNote)
    {
        var runContext = BuildRunContext(runIndex, primaryTargetDraft, operatorNote);
        _preparedRunContext = runContext;
        return Task.FromResult<string?>($"Initialized {runContext.DisplayName ?? runContext.Id.Value}.");
    }

    public void StopRuntime()
    {
        _ = StopRuntimeAsync();
    }

    public Task StartRuntimeAsync() => StartRuntimeCoreAsync();

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

    private async Task StartRuntimeCoreAsync()
    {
        try
        {
            var runContext = _preparedRunContext ?? BuildRunContext(
                $"run-{DateTimeOffset.Now:yyyyMMdd-HHmmss}",
                null,
                null);
            _preparedRunContext = null;
            var started = _runtimeCoordinator.Start(runContext);
            _runRecorder.BeginRun(started);
            RuntimeStatus.Update(started);
            await AttachControllerForRunAsync(started).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RuntimeStatus.ShowOperationError("Unable to start runtime.", ex.Message);
        }
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
            await StopControllerAsync(reason, stopped.StoppedAtUtc).ConfigureAwait(false);
            RuntimeStatus.Update(stopped);
            _preparedRunContext = null;
            FinalizeRunRecording(stopped);
        }
        catch (Exception ex)
        {
            _preparedRunContext = null;
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

        LastRunRecording = _runRecorder.CompleteRun(stopped, DevicePanels, _monitorSession.Snapshot.Current);
    }

    public void Dispose()
    {
        _experimentMonitorPanel.Dispose();
        RunAsyncCleanup(() => _monitorSession.DisposeAsync().AsTask());
        if (_controllerUnitSession is not null)
        {
            RunAsyncCleanup(() => _controllerUnitSession.DisposeAsync().AsTask());
        }

        foreach (var panel in DevicePanels)
        {
            panel.Dispose();
        }
    }

    private RunContextDefinition BuildRunContext(string runIndex, string? primaryTargetDraft, string? operatorNote)
    {
        var normalizedRunIndex = string.IsNullOrWhiteSpace(runIndex)
            ? $"run-{DateTimeOffset.Now:yyyyMMdd-HHmmss}"
            : runIndex.Trim();
        double? primaryTargetValue = null;
        if (!string.IsNullOrWhiteSpace(primaryTargetDraft))
        {
            if (!double.TryParse(primaryTargetDraft, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
                || double.IsNaN(parsed)
                || double.IsInfinity(parsed))
            {
                throw new InvalidOperationException("Primary target must be a finite numeric value.");
            }

            primaryTargetValue = parsed;
        }

        var experiment = _adHocRunDefinitionFactory.Create(DevicePanels, primaryTargetValue);
        var runContextId = new ArtifactId($"runctx.{Slugify(normalizedRunIndex, "ad_hoc_runtime")}");
        var metadata = new Dictionary<ArtifactId, string>
        {
            [new ArtifactId("meta.run_index")] = normalizedRunIndex
        };
        if (primaryTargetValue.HasValue)
        {
            metadata[new ArtifactId("meta.primary_target")] = primaryTargetValue.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        return new RunContextDefinition(
            runContextId,
            experiment,
            normalizedRunIndex,
            operatorNote,
            metadata,
            new Dictionary<ArtifactId, ArtifactId>(),
            [],
            []);
    }

    private async Task AttachControllerForRunAsync(RuntimeRunContext started)
    {
        if (!started.StartedAtUtc.HasValue || started.Experiment?.ControlTargets.Count is not > 0)
        {
            await ReplaceControllerAsync(null).ConfigureAwait(false);
            return;
        }

        var controlTarget = started.Experiment.ControlTargets[0];
        var controller = new ControllerUnitSession(
            started.Experiment,
            controlTarget.Id,
            started.StartedAtUtc.Value);
        await ReplaceControllerAsync(controller).ConfigureAwait(false);
        await controller.SampleAsync(started.StartedAtUtc.Value).ConfigureAwait(false);
    }

    private async Task ReplaceControllerAsync(ControllerUnitSession? controller)
    {
        var previous = _controllerUnitSession;
        _controllerUnitSession = controller;
        _monitorSession.AttachController(controller);
        if (previous is not null)
        {
            await previous.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task StopControllerAsync(StopReason reason, DateTimeOffset? stoppedAtUtc)
    {
        if (_controllerUnitSession is null)
        {
            return;
        }

        await _controllerUnitSession.StopAsync(reason, stoppedAtUtc).ConfigureAwait(false);
    }

    private static string Slugify(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(static character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();
        var slug = new string(characters);
        while (slug.Contains("__", StringComparison.Ordinal))
        {
            slug = slug.Replace("__", "_", StringComparison.Ordinal);
        }

        slug = slug.Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? fallback : slug;
    }

    private static void RunAsyncCleanup(Func<Task> cleanup)
    {
        Task.Run(cleanup).GetAwaiter().GetResult();
    }

    private sealed class NullRunRecorder : IRunRecorder
    {
        public void BeginRun(RuntimeRunContext snapshot)
        {
        }

        public RunRecordingResult? CompleteRun(
            RuntimeRunContext snapshot,
            IReadOnlyList<IDeviceTestPanelViewModel> panels,
            ExperimentMonitorSnapshot? monitorSnapshot = null) => null;
    }
}
