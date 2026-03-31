using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class RuntimeCoordinatorTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());

        Assert.Equal(RunState.Idle, coordinator.LatestSnapshot.State);
    }

    [Fact]
    public void Start_TransitionsRuntimeToRunning()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        RuntimeRunContext? changedSnapshot = null;
        coordinator.SnapshotChanged += snapshot => changedSnapshot = snapshot;

        var context = coordinator.Start();

        Assert.Equal(RunState.Running, context.State);
        Assert.Equal(RunState.Running, coordinator.LatestSnapshot.State);
        Assert.Equal(context, changedSnapshot);
    }

    [Fact]
    public void Start_WithResolvedExperiment_Captures_Experiment_Identity()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var experiment = CreateResolvedExperimentDefinition();

        var context = coordinator.Start(experiment);

        Assert.Equal(experiment, context.Experiment);
        Assert.Equal(experiment, coordinator.LatestSnapshot.Experiment);
    }

    [Fact]
    public async Task Start_WithRunContext_Captures_Run_Metadata_And_Preserves_It_Through_Stop()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var runContext = CreateRunContextDefinition();

        var started = coordinator.Start(runContext);
        var stopped = await coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run."));

        Assert.Equal(runContext, started.RunContext);
        Assert.Equal(runContext, coordinator.LatestSnapshot.RunContext);
        Assert.Equal(runContext.Experiment, started.Experiment);
        Assert.Equal(runContext, stopped.RunContext);
    }

    [Fact]
    public void ValidateStart_Returns_CrossSession_Issues_For_Invalid_Resolved_Experiment()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var experiment = CreateCrossSessionInvalidResolvedExperimentDefinition();

        var validation = coordinator.ValidateStart(experiment);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "conflicting_control_target_command_role");
    }

    [Fact]
    public void Start_WithRunContext_Rejects_CrossSession_Invalid_Experiment()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var runContext = CreateRunContextDefinition(CreateCrossSessionInvalidResolvedExperimentDefinition());

        var exception = Assert.Throws<InvalidOperationException>(() => coordinator.Start(runContext));

        Assert.Contains("cross-session validation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Start_WithResolvedExperiment_Rejects_CrossSession_Invalid_Experiment()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var experiment = CreateCrossSessionInvalidResolvedExperimentDefinition();

        var exception = Assert.Throws<InvalidOperationException>(() => coordinator.Start(experiment));

        Assert.Contains("cross-session validation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestStopAsync_TransitionsThroughStoppingAndThenReturnsCompletedStopSnapshot()
    {
        var registry = new FakeRegistry(blockStopUntilReleased: true);
        var coordinator = new RuntimeCoordinator(registry);
        var started = coordinator.Start();
        var reason = StopReason.UserRequested("Operator stopped the run.");

        var stopTask = coordinator.RequestStopAsync(reason);
        await registry.StopEntered.Task;

        Assert.Equal(RunState.Stopping, coordinator.LatestSnapshot.State);
        Assert.Equal(reason, coordinator.LatestSnapshot.StopReason);
        Assert.Null(coordinator.LatestSnapshot.StoppedAtUtc);

        registry.ReleaseStop();
        var stopped = await stopTask;

        Assert.Equal(RunState.Idle, stopped.State);
        Assert.Equal(started.RunId, stopped.RunId);
        Assert.NotNull(stopped.StartedAtUtc);
        Assert.NotNull(stopped.StoppedAtUtc);
        Assert.Equal(reason, stopped.StopReason);
        Assert.Equal(stopped, coordinator.LatestSnapshot);
    }

    [Fact]
    public async Task SnapshotChanged_Fires_For_Stopping_And_Stopped_Snapshots()
    {
        var registry = new FakeRegistry(blockStopUntilReleased: true);
        var coordinator = new RuntimeCoordinator(registry);
        var seenStates = new List<RunState>();
        coordinator.SnapshotChanged += snapshot => seenStates.Add(snapshot.State);
        coordinator.Start();

        var stopTask = coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run."));
        await registry.StopEntered.Task;
        registry.ReleaseStop();
        await stopTask;

        Assert.Contains(RunState.Stopping, seenStates);
        Assert.Contains(RunState.Idle, seenStates);
    }

    [Fact]
    public void Start_RejectsDuplicateStartWhileAlreadyActive()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        coordinator.Start();

        var exception = Assert.Throws<InvalidOperationException>(() => coordinator.Start());

        Assert.Contains("already active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Start_RejectsDuplicateStartWhileStopIsInProgress()
    {
        var registry = new FakeRegistry(blockStopUntilReleased: true);
        var coordinator = new RuntimeCoordinator(registry);
        coordinator.Start();

        var stopTask = coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run."));
        await registry.StopEntered.Task;

        var exception = Assert.Throws<InvalidOperationException>(() => coordinator.Start());

        Assert.Contains("already active", exception.Message, StringComparison.OrdinalIgnoreCase);

        registry.ReleaseStop();
        await stopTask;
    }

    [Fact]
    public async Task RequestStopAsync_RejectsRequestWhileIdle()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run.")));

        Assert.Contains("not active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestStopAsync_RejectsDuplicateStopWhileAlreadyStopping()
    {
        var registry = new FakeRegistry(blockStopUntilReleased: true);
        var coordinator = new RuntimeCoordinator(registry);
        coordinator.Start();

        var stopTask = coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run."));
        await registry.StopEntered.Task;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RequestStopAsync(StopReason.UserRequested("Second stop request.")));

        Assert.Contains("already in progress", exception.Message, StringComparison.OrdinalIgnoreCase);

        registry.ReleaseStop();
        await stopTask;
    }

    [Fact]
    public async Task EnsureStoppedAsync_ReusesInFlightStopPathWhileStopping()
    {
        var registry = new FakeRegistry(blockStopUntilReleased: true);
        var coordinator = new RuntimeCoordinator(registry);
        coordinator.Start();
        var reason = StopReason.UserRequested("Operator stopped the run.");

        var firstStopTask = coordinator.RequestStopAsync(reason);
        await registry.StopEntered.Task;

        var ensuredStopTask = coordinator.EnsureStoppedAsync(StopReason.UserRequested("Application shutdown."));

        registry.ReleaseStop();
        var firstResult = await firstStopTask;
        var finalSnapshot = await ensuredStopTask;

        Assert.Equal(1, registry.StopAllCallCount);
        Assert.Equal(firstResult, finalSnapshot);
        Assert.Equal(RunState.Idle, finalSnapshot.State);
        Assert.Equal(reason, finalSnapshot.StopReason);
    }

    [Fact]
    public async Task RequestStopAsync_InvokesRegistryStopAllWithProvidedReason()
    {
        var registry = new FakeRegistry();
        var coordinator = new RuntimeCoordinator(registry);
        coordinator.Start();
        var reason = StopReason.UserRequested("Operator requested global stop.");

        await coordinator.RequestStopAsync(reason);

        Assert.Equal(1, registry.StopAllCallCount);
        Assert.Equal(reason, registry.LastReason);
    }

    [Fact]
    public async Task Start_AfterStop_BeginsNewRunAndClearsPreviousStopRequest()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var firstRun = coordinator.Start();
        await coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run."));

        var restarted = coordinator.Start();

        Assert.Equal(RunState.Running, restarted.State);
        Assert.NotEqual(firstRun.RunId, restarted.RunId);
        Assert.Null(restarted.StoppedAtUtc);
        Assert.Null(restarted.StopReason);
    }

    [Fact]
    public async Task LatestSnapshot_RetainsStoppedRunDetailsAfterStopCompletes()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var started = coordinator.Start();

        var stopped = await coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run."));

        Assert.Equal(started.RunId, coordinator.LatestSnapshot.RunId);
        Assert.Equal(stopped.StoppedAtUtc, coordinator.LatestSnapshot.StoppedAtUtc);
        Assert.Equal(stopped.StopReason, coordinator.LatestSnapshot.StopReason);
    }

    [Fact]
    public async Task RequestStopAsync_FinalizesStoppedSnapshotEvenWhenRegistryStopFails()
    {
        var registry = new FakeRegistry(stopFailure: new InvalidOperationException("Stop failed."));
        var coordinator = new RuntimeCoordinator(registry);
        coordinator.Start();
        var reason = StopReason.UserRequested("Operator stopped the run.");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RequestStopAsync(reason));

        Assert.Equal("Stop failed.", exception.Message);
        Assert.Equal(RunState.Idle, coordinator.LatestSnapshot.State);
        Assert.NotNull(coordinator.LatestSnapshot.StoppedAtUtc);
        Assert.Equal(reason, coordinator.LatestSnapshot.StopReason);
    }

    [Fact]
    public async Task Start_AfterFailedStop_BeginsNewRunAndClearsPreviousStopRequest()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry(stopFailure: new InvalidOperationException("Stop failed.")));
        coordinator.Start();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RequestStopAsync(StopReason.UserRequested("Operator stopped the run.")));

        var restarted = coordinator.Start();

        Assert.Equal(RunState.Running, restarted.State);
        Assert.Null(restarted.StoppedAtUtc);
        Assert.Null(restarted.StopReason);
    }

    [Fact]
    public async Task RequestStopAsync_CancellationStillFinalizesStoppedSnapshot()
    {
        var registry = new FakeRegistry(blockStopUntilReleased: true);
        var coordinator = new RuntimeCoordinator(registry);
        coordinator.Start();
        using var cancellationSource = new CancellationTokenSource();

        var stopTask = coordinator.RequestStopAsync(
            StopReason.UserRequested("Operator stopped the run."),
            cancellationSource.Token);

        await registry.StopEntered.Task;
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopTask);

        Assert.Equal(RunState.Idle, coordinator.LatestSnapshot.State);
        Assert.NotNull(coordinator.LatestSnapshot.StoppedAtUtc);
        Assert.NotNull(coordinator.LatestSnapshot.StopReason);
    }

    private sealed class FakeRegistry : IDeviceSessionRegistry
    {
#pragma warning disable CS0067
        private readonly TaskCompletionSource<object?>? _stopRelease;
        private readonly Exception? _stopFailure;

        public FakeRegistry(bool blockStopUntilReleased = false, Exception? stopFailure = null)
        {
            _stopFailure = stopFailure;
            if (blockStopUntilReleased)
            {
                _stopRelease = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public event Action? SessionsChanged;
#pragma warning restore CS0067

        public IReadOnlyCollection<IDeviceSession> Sessions => Array.Empty<IDeviceSession>();

        public int StopAllCallCount { get; private set; }

        public StopReason? LastReason { get; private set; }

        public TaskCompletionSource<object?> StopEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TSession GetOrAdd<TSession>(DeviceSessionId sessionId, Func<TSession> factory)
            where TSession : class, IDeviceSession => throw new NotSupportedException();

        public bool TryGet<TSession>(DeviceSessionId sessionId, out TSession? session)
            where TSession : class, IDeviceSession
        {
            session = null;
            return false;
        }

        public bool Remove(DeviceSessionId sessionId) => false;

        public async Task StopAllAsync(StopReason reason, CancellationToken cancellationToken = default)
        {
            StopAllCallCount++;
            LastReason = reason;
            StopEntered.TrySetResult(null);

            if (_stopFailure is not null)
            {
                throw _stopFailure;
            }

            if (_stopRelease is not null)
            {
                await _stopRelease.Task.WaitAsync(cancellationToken);
            }
        }

        public void ReleaseStop()
        {
            _stopRelease?.TrySetResult(null);
        }
    }

    private static ResolvedExperimentDefinition CreateResolvedExperimentDefinition()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe the flow with a camera and a controller.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.camera_upstream"),
                    "Upstream Camera",
                    "Captures the upstream flow region.",
                    [new ArtifactId("cap.frame_stream"), new ArtifactId("cap.exposure_control")],
                    new ArtifactId("protocol.vendor_sdk_camera_v1"))
            ],
            [],
            [],
            [],
            [],
            [],
            []);
        var device = new DeviceDefinition(
            new ArtifactId("device.camera_01"),
            "Camera 01",
            "camera_01",
            new ProtocolDefinition(
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                "Vendor SDK Camera",
                "sdk",
                "request-response",
                "session",
                "best-effort",
                "fail-fast",
                "manual"),
            [
                new CapabilityDefinition(new ArtifactId("cap.frame_stream"), "frame_stream", "Produces image frames."),
                new CapabilityDefinition(new ArtifactId("cap.exposure_control"), "exposure_control", "Adjusts camera exposure.")
            ],
            [],
            "healthy");
        var binding = new RoleBindingDefinition(
            new ArtifactId("role.camera_upstream"),
            device.Id,
            device.Protocol.Id,
            [new ArtifactId("cap.frame_stream"), new ArtifactId("cap.exposure_control")]);

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [device],
            [binding],
            new Dictionary<ArtifactId, string>());
    }

    private static RunContextDefinition CreateRunContextDefinition() =>
        CreateRunContextDefinition(CreateResolvedExperimentDefinition());

    private static RunContextDefinition CreateRunContextDefinition(ResolvedExperimentDefinition experiment) =>
        new(
            new ArtifactId("runctx.transition_demo_001"),
            experiment,
            "transition-demo-001",
            "looping mode, RR=0.36",
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("meta.operator")] = "yi"
            },
            new Dictionary<ArtifactId, ArtifactId>
            {
                [new ArtifactId("device.camera_01")] = new ArtifactId("artifact.settings.camera_01")
            },
            [new ArtifactId("decision.run_001")],
            [new ArtifactId("artifact.run_manifest_001")]);

    private static ResolvedExperimentDefinition CreateCrossSessionInvalidResolvedExperimentDefinition()
    {
        var commandRoleId = new ArtifactId("role.control_center");
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.control_target_conflict"),
            "Control Target Conflict",
            "Exercise cross-session validation for conflicting command-role ownership.",
            [
                new DeviceRoleDefinition(
                    commandRoleId,
                    "Control Center",
                    "Provides actuation and status output.",
                    [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")],
                    new ArtifactId("protocol.serial_ascii_v1"))
            ],
            [
                new ParameterDefinition(
                    new ArtifactId("param.re_target_primary"),
                    "Primary Re Target",
                    "float",
                    "experiment",
                    defaultValue: "1600"),
                new ParameterDefinition(
                    new ArtifactId("param.re_target_secondary"),
                    "Secondary Re Target",
                    "float",
                    "experiment",
                    defaultValue: "1700")
            ],
            [
                new StreamDefinition(
                    new ArtifactId("stream.reynolds_number"),
                    "Reynolds Number",
                    "transform",
                    "scalar<double>",
                    "runtime"),
                new StreamDefinition(
                    new ArtifactId("stream.flow_rate"),
                    "Flow Rate",
                    "transform",
                    "scalar<double>",
                    "runtime")
            ],
            [],
            [],
            [],
            [],
            [
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re",
                    "Maintains the main Reynolds target.",
                    new ArtifactId("stream.reynolds_number"),
                    commandRoleId,
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target_primary")),
                new ControlTargetDefinition(
                    new ArtifactId("control.re_secondary"),
                    "Secondary Re",
                    "Competes for the same command role in V1.",
                    new ArtifactId("stream.flow_rate"),
                    commandRoleId,
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target_secondary"))
            ]);
        var controllerDevice = new DeviceDefinition(
            new ArtifactId("device.controller_01"),
            "Controller 01",
            "controller_01",
            new ProtocolDefinition(
                new ArtifactId("protocol.serial_ascii_v1"),
                "Serial ASCII",
                "serial",
                "request-response",
                "session",
                "best-effort",
                "fail-fast",
                "manual"),
            [
                new CapabilityDefinition(new ArtifactId("cap.pulse_actuation"), "pulse_actuation", "Issues trigger pulses."),
                new CapabilityDefinition(new ArtifactId("cap.status_report"), "status_report", "Reports actuator state.")
            ],
            [],
            "healthy");
        var binding = new RoleBindingDefinition(
            commandRoleId,
            controllerDevice.Id,
            controllerDevice.Protocol.Id,
            [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")]);

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [controllerDevice],
            [binding],
            new Dictionary<ArtifactId, string>());
    }
}
