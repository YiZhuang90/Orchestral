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
    private readonly IRunDefinitionFactory _runDefinitionFactory;
    private readonly ExperimentMonitorSession _monitorSession;
    private readonly ExperimentMonitorPanelViewModel _experimentMonitorPanel;
    private ControllerUnitSession? _controllerUnitSession;
    private FlowReynoldsDerivedStateSession? _derivedStateSession;
    private IStreamDeliverySubscription? _derivedStateControllerSubscription;
    private RunContextDefinition? _preparedRunContext;

    public MainViewModel(
        IRuntimeCoordinator runtimeCoordinator,
        IDeviceSessionRegistry sessionRegistry,
        IEnumerable<IDeviceTestPanelViewModel> devicePanels,
        IRunRecorder? runRecorder = null,
        IRunDefinitionFactory? adHocRunDefinitionFactory = null)
    {
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        ArgumentNullException.ThrowIfNull(devicePanels);
        _runRecorder = runRecorder ?? new NullRunRecorder();
        _runDefinitionFactory = adHocRunDefinitionFactory ?? new AdHocRunDefinitionFactory();
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

    public Task<ExperimentMonitorInitializationResult> InitializeRuntimeAsync(string runIndex, string? primaryTargetDraft, string? operatorNote)
    {
        try
        {
            var runContext = BuildRunContext(runIndex, primaryTargetDraft, operatorNote);
            var initializationValidation = ValidateRunContextForInitialization(runContext);
            if (!initializationValidation.IsReady)
            {
                _preparedRunContext = null;
                return Task.FromResult(initializationValidation);
            }

            _preparedRunContext = runContext;
            return Task.FromResult(ExperimentMonitorInitializationResult.Ready(
                $"Initialized {runContext.DisplayName ?? runContext.Id.Value}."));
        }
        catch (Exception ex)
        {
            _preparedRunContext = null;
            return Task.FromResult(ExperimentMonitorInitializationResult.Blocked(
                ex.Message,
                [
                    new ExperimentMonitorItem(
                        "alarm.initialize",
                        ExperimentMonitorSeverity.Alarm,
                        "initialize",
                        ex.Message,
                        DateTimeOffset.UtcNow)
                ]));
        }
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
            var initializationValidation = ValidateRunContextForInitialization(runContext);
            if (!initializationValidation.IsReady)
            {
                throw new InvalidOperationException(initializationValidation.StatusMessage);
            }

            _preparedRunContext = null;
            var started = _runtimeCoordinator.Start(runContext);
            _runRecorder.BeginRun(started);
            RuntimeStatus.Update(started);
            await AttachExperimentPlaneSessionsForRunAsync(started).ConfigureAwait(false);
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
            await StopExperimentPlaneSessionsAsync(reason, stopped.StoppedAtUtc).ConfigureAwait(false);
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

        LastRunRecording = _runRecorder.CompleteRun(
            stopped,
            DevicePanels,
            _derivedStateSession?.State.Current,
            _derivedStateSession?.RecordedSamples,
            _monitorSession.Snapshot.Current);
    }

    public void Dispose()
    {
        _experimentMonitorPanel.Dispose();
        RunAsyncCleanup(() => _monitorSession.DisposeAsync().AsTask());
        RunAsyncCleanup(DisposeExperimentPlaneSessionsAsync);

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

        var experiment = _runDefinitionFactory.Create(DevicePanels, primaryTargetValue);
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

    private ExperimentMonitorInitializationResult ValidateRunContextForInitialization(RunContextDefinition runContext)
    {
        var lint = runContext.Experiment.Experiment.Lint();
        if (!lint.IsValid)
        {
            return ExperimentMonitorInitializationResult.Blocked(
                "Initialization blocked by experiment-definition linting.",
                BuildInitializationLintItems(lint));
        }

        var validation = _runtimeCoordinator.ValidateStart(runContext.Experiment);
        if (!validation.IsValid)
        {
            return ExperimentMonitorInitializationResult.Blocked(
                "Initialization blocked by cross-session validation.",
                BuildInitializationValidationItems(validation));
        }

        return ExperimentMonitorInitializationResult.Ready(
            $"Initialized {runContext.DisplayName ?? runContext.Id.Value}.");
    }

    private async Task AttachExperimentPlaneSessionsForRunAsync(RuntimeRunContext started)
    {
        if (!started.StartedAtUtc.HasValue || started.Experiment is null)
        {
            await ReplaceExperimentPlaneSessionsAsync(null, null, null).ConfigureAwait(false);
            return;
        }

        var experiment = started.Experiment;
        ControllerUnitSession? controller = null;
        FlowReynoldsDerivedStateSession? derivedState = null;
        IStreamDeliverySubscription? derivedStateControllerSubscription = null;
        try
        {
            if (experiment.ControlTargets.Count > 0)
            {
                var controlTarget = experiment.ControlTargets[0];
                controller = new ControllerUnitSession(
                    experiment,
                    controlTarget.Id,
                    started.StartedAtUtc.Value);
                await controller.SampleAsync(started.StartedAtUtc.Value).ConfigureAwait(false);
            }

            if (RequiresFlowReynoldsDerivedState(experiment))
            {
                var controlCenterSession = _sessionRegistry.Sessions.OfType<ControlCenterSession>().FirstOrDefault();
                if (controlCenterSession is not null)
                {
                    var pt104Session = _sessionRegistry.Sessions.OfType<Pt104Session>().FirstOrDefault();
                    derivedState = new FlowReynoldsDerivedStateSession(experiment);
                    if (controller is not null)
                    {
                        derivedStateControllerSubscription = derivedState.Samples.Subscribe(
                            StreamDeliveryPolicy.LatestOnly(),
                            sample => new ValueTask(controller.SampleAsync(sample.ObservedAtUtc, sample.ReynoldsNumber)));
                    }

                    await derivedState.AttachRuntimeSourcesAsync(controlCenterSession, pt104Session).ConfigureAwait(false);
                }
            }

            await ReplaceExperimentPlaneSessionsAsync(controller, derivedState, derivedStateControllerSubscription).ConfigureAwait(false);
        }
        catch
        {
            derivedStateControllerSubscription?.Dispose();
            if (derivedState is not null)
            {
                await derivedState.DisposeAsync().ConfigureAwait(false);
            }

            if (controller is not null)
            {
                await controller.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    private async Task ReplaceExperimentPlaneSessionsAsync(
        ControllerUnitSession? controller,
        FlowReynoldsDerivedStateSession? derivedState,
        IStreamDeliverySubscription? derivedStateControllerSubscription)
    {
        var previousController = _controllerUnitSession;
        var previousDerivedState = _derivedStateSession;
        var previousDerivedStateControllerSubscription = _derivedStateControllerSubscription;

        _controllerUnitSession = controller;
        _derivedStateSession = derivedState;
        _derivedStateControllerSubscription = derivedStateControllerSubscription;

        _monitorSession.AttachController(controller);
        _monitorSession.AttachDerivedState(derivedState);

        previousDerivedStateControllerSubscription?.Dispose();
        if (previousDerivedState is not null)
        {
            await previousDerivedState.DisposeAsync().ConfigureAwait(false);
        }

        if (previousController is not null)
        {
            await previousController.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task StopExperimentPlaneSessionsAsync(StopReason reason, DateTimeOffset? stoppedAtUtc)
    {
        if (_derivedStateSession is not null)
        {
            await _derivedStateSession.StopAsync(reason).ConfigureAwait(false);
        }

        _derivedStateControllerSubscription?.Dispose();
        _derivedStateControllerSubscription = null;

        if (_controllerUnitSession is not null)
        {
            await _controllerUnitSession.StopAsync(reason, stoppedAtUtc).ConfigureAwait(false);
        }
    }

    private async Task DisposeExperimentPlaneSessionsAsync()
    {
        var controller = _controllerUnitSession;
        var derivedState = _derivedStateSession;
        var subscription = _derivedStateControllerSubscription;

        _controllerUnitSession = null;
        _derivedStateSession = null;
        _derivedStateControllerSubscription = null;

        _monitorSession.AttachController(null);
        _monitorSession.AttachDerivedState(null);

        subscription?.Dispose();
        if (derivedState is not null)
        {
            await derivedState.DisposeAsync().ConfigureAwait(false);
        }

        if (controller is not null)
        {
            await controller.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<ExperimentMonitorItem> BuildInitializationValidationItems(CrossSessionValidationResult validation)
    {
        return validation.Issues
            .Select((issue, index) => new ExperimentMonitorItem(
                $"alarm.cross_session_validation.{index + 1}",
                ExperimentMonitorSeverity.Alarm,
                BuildInitializationValidationSource(issue),
                issue.Message,
                DateTimeOffset.UtcNow))
            .ToArray();
    }

    private static IReadOnlyList<ExperimentMonitorItem> BuildInitializationLintItems(ExperimentDefinitionLintResult lint)
    {
        return lint.Issues
            .Where(static issue => issue.Severity == ExperimentDefinitionLintSeverity.Error)
            .Select((issue, index) => new ExperimentMonitorItem(
                $"alarm.experiment_definition_linting.{index + 1}",
                ExperimentMonitorSeverity.Alarm,
                "experiment-definition linting",
                issue.Message,
                DateTimeOffset.UtcNow))
            .ToArray();
    }

    private static string BuildInitializationValidationSource(CrossSessionValidationIssue issue)
    {
        if (issue.CommandRoleId.HasValue)
        {
            return issue.CommandRoleId.Value.Value;
        }

        if (issue.DeviceId.HasValue)
        {
            return issue.DeviceId.Value.Value;
        }

        return "cross-session validation";
    }

    private static bool RequiresFlowReynoldsDerivedState(ResolvedExperimentDefinition experiment)
    {
        return experiment.Experiment.Streams.Any(static stream => stream.Id == FlowReynoldsArtifactIds.ReynoldsNumberStreamId);
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
            FlowReynoldsDerivedStateSnapshot? derivedStateSnapshot = null,
            IReadOnlyList<FlowReynoldsDerivedStateSample>? derivedStateSamples = null,
            ExperimentMonitorSnapshot? monitorSnapshot = null) => null;
    }
}
