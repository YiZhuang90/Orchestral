using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Devices.ControlCenter;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class ExperimentMonitorSessionTests
{
    private static readonly DateTimeOffset RunStartedAt = new(2026, 3, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Snapshot_Tracks_Controller_Target_And_Error()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var experiment = CreateResolvedExperimentDefinition();
        var started = coordinator.Start(experiment);
        var sampleObservedAtUtc = started.StartedAtUtc!.Value.AddSeconds(5);
        await using var controller = new ControllerUnitSession(
            experiment,
            FlowReynoldsArtifactIds.PrimaryControlTargetId,
            started.StartedAtUtc!.Value);
        await using var monitor = new ExperimentMonitorSession(
            coordinator,
            [],
            clock: () => sampleObservedAtUtc);

        monitor.AttachController(controller);
        await controller.SampleAsync(sampleObservedAtUtc, 1588);

        var snapshot = Assert.IsType<ExperimentMonitorSnapshot>(monitor.Snapshot.Current);
        Assert.Equal(RunState.Running, snapshot.RunState);
        Assert.Equal("Primary Re Control", snapshot.PrimaryControlLabel);
        Assert.Equal(1600, snapshot.PrimaryControlTargetValue);
        Assert.Equal(1588, snapshot.PrimaryControlMeasuredValue);
        Assert.Equal(12, snapshot.PrimaryControlErrorValue);
        Assert.Equal(ExperimentMonitorSeverity.None, snapshot.HighestSeverity);
        Assert.Contains("1600", snapshot.PrimaryControlSummary);
        Assert.Contains("+/- 12", snapshot.PrimaryControlSummary);
    }

    [Fact]
    public async Task Snapshot_Raises_Warning_When_Controller_Measurement_Is_Stale()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        var experiment = CreateResolvedExperimentDefinition();
        var started = coordinator.Start(experiment);
        var firstObservedAtUtc = started.StartedAtUtc!.Value.AddSeconds(5);
        var secondObservedAtUtc = started.StartedAtUtc!.Value.AddSeconds(6);
        await using var controller = new ControllerUnitSession(
            experiment,
            FlowReynoldsArtifactIds.PrimaryControlTargetId,
            started.StartedAtUtc!.Value);
        await using var monitor = new ExperimentMonitorSession(
            coordinator,
            [],
            clock: () => secondObservedAtUtc);

        monitor.AttachController(controller);
        await controller.SampleAsync(firstObservedAtUtc, 1588);
        await controller.SampleAsync(secondObservedAtUtc);

        var snapshot = Assert.IsType<ExperimentMonitorSnapshot>(monitor.Snapshot.Current);
        Assert.Equal(ExperimentMonitorSeverity.Warning, snapshot.HighestSeverity);
        Assert.Equal(1, snapshot.WarningCount);
        Assert.Contains(snapshot.Items, item =>
            item.Severity == ExperimentMonitorSeverity.Warning
            && item.Source == "controller.re_primary"
            && item.Message.Contains("stale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Snapshot_Raises_Alarm_For_Disconnected_Critical_Control_Source()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        coordinator.Start(CreateResolvedExperimentDefinition());
        var source = new FakeMonitorSource(new ExperimentMonitorDeviceSnapshot
        {
            SourceId = "session.control_center_01",
            DisplayName = "Control Center",
            DeviceId = "control_center_01",
            SessionFamily = "ControlCenter",
            Connected = false,
            Busy = false,
            LiveActive = false,
            IsCriticalControl = true,
            StatusMessage = "Disconnected."
        });
        await using var monitor = new ExperimentMonitorSession(
            coordinator,
            [source],
            clock: () => RunStartedAt.AddSeconds(1));

        var snapshot = Assert.IsType<ExperimentMonitorSnapshot>(monitor.Snapshot.Current);

        Assert.Equal(ExperimentMonitorSeverity.Alarm, snapshot.HighestSeverity);
        Assert.Equal(1, snapshot.AlarmCount);
        Assert.Contains(snapshot.Items, item =>
            item.Severity == ExperimentMonitorSeverity.Alarm
            && item.Source == "session.control_center_01"
            && item.Message.Contains("disconnected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Snapshot_Includes_Derived_State_Summary_When_Attached()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        coordinator.Start(CreateResolvedExperimentDefinition());
        await using var derivedState = new FlowReynoldsDerivedStateSession(CreateResolvedExperimentDefinition());
        derivedState.RecordTemperatureSample(new Pt104Reading("pt104_01", 2, 20.9, RunStartedAt.AddSeconds(1), "LiveRead"));
        derivedState.RecordTemperatureSample(new Pt104Reading("pt104_01", 4, 21.1, RunStartedAt.AddSeconds(1), "LiveRead"));
        derivedState.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)));
        derivedState.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 2.0, 14, RunStartedAt.AddSeconds(2)));
        await using var monitor = new ExperimentMonitorSession(
            coordinator,
            [],
            clock: () => RunStartedAt.AddSeconds(3));

        monitor.AttachDerivedState(derivedState);

        var snapshot = Assert.IsType<ExperimentMonitorSnapshot>(monitor.Snapshot.Current);
        Assert.NotNull(snapshot.DerivedReynoldsNumber);
        Assert.NotNull(snapshot.DerivedFlowRateLitersPerMinute);
        Assert.Equal(21.0, snapshot.DerivedMeanTemperatureC!.Value, 6);
        Assert.Contains("Re", snapshot.DerivedStateSummary);
        Assert.Contains("L/min", snapshot.DerivedStateSummary);
    }

    [Fact]
    public async Task Snapshot_Raises_Warning_When_Derived_State_Is_Stale()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        coordinator.Start(CreateResolvedExperimentDefinition());
        await using var derivedState = new FlowReynoldsDerivedStateSession(CreateResolvedExperimentDefinition());
        derivedState.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)));
        derivedState.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 2.0, 11, RunStartedAt.AddSeconds(2)));
        await using var monitor = new ExperimentMonitorSession(
            coordinator,
            [],
            clock: () => RunStartedAt.AddSeconds(10));

        monitor.AttachDerivedState(derivedState);

        var snapshot = Assert.IsType<ExperimentMonitorSnapshot>(monitor.Snapshot.Current);
        Assert.Equal(ExperimentMonitorSeverity.Warning, snapshot.HighestSeverity);
        Assert.Contains(snapshot.Items, item =>
            item.Source == "derived.reynolds_number"
            && item.Message.Contains("stale", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeRegistry : IDeviceSessionRegistry
    {
        public event Action? SessionsChanged;

        public IReadOnlyCollection<IDeviceSession> Sessions => Array.Empty<IDeviceSession>();

        public TSession GetOrAdd<TSession>(DeviceSessionId sessionId, Func<TSession> factory)
            where TSession : class, IDeviceSession => throw new NotSupportedException();

        public bool TryGet<TSession>(DeviceSessionId sessionId, out TSession? session)
            where TSession : class, IDeviceSession
        {
            session = null;
            return false;
        }

        public bool Remove(DeviceSessionId sessionId) => false;

        public Task StopAllAsync(StopReason reason, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void RaiseChanged() => SessionsChanged?.Invoke();
    }

    private sealed class FakeMonitorSource : IExperimentMonitorSource
    {
        private readonly ExperimentMonitorDeviceSnapshot _snapshot;

        public FakeMonitorSource(ExperimentMonitorDeviceSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public event Action? Changed
        {
            add { }
            remove { }
        }

        public ExperimentMonitorDeviceSnapshot CreateSnapshot() => _snapshot;

        public void Dispose()
        {
        }
    }

    private static ResolvedExperimentDefinition CreateResolvedExperimentDefinition()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe and hold Reynolds number near the requested target.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.flow_actuator"),
                    "Flow Actuator",
                    "Controls the downstream actuator.",
                    [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")],
                    new ArtifactId("protocol.serial_ascii_v1"))
            ],
            [
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PrimaryControlTargetParameterId,
                    "Re Target",
                    "float",
                    "experiment",
                    unit: "dimensionless"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PulsesPerLiterParameterId,
                    "Pulses Per Liter",
                    "integer",
                    "experiment",
                    defaultValue: "80"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PipeInnerDiameterParameterId,
                    "Pipe Inner Diameter",
                    "float",
                    "experiment",
                    unit: "m",
                    defaultValue: "0.00403"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PipeLengthParameterId,
                    "Pipe Length",
                    "float",
                    "experiment",
                    unit: "m",
                    defaultValue: "1.0"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PipeRoughnessParameterId,
                    "Pipe Roughness",
                    "float",
                    "experiment",
                    unit: "m",
                    defaultValue: "0.0"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.ReferenceTemperatureParameterId,
                    "Reference Temperature",
                    "float",
                    "experiment",
                    unit: "C",
                    defaultValue: "20.95"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.FlowrateAverageCountParameterId,
                    "Flowrate Average Count",
                    "integer",
                    "experiment",
                    defaultValue: "100"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PulsePollIntervalMillisecondsParameterId,
                    "Pulse Poll Interval",
                    "integer",
                    "experiment",
                    unit: "ms",
                    defaultValue: "500")
            ],
            [
                new StreamDefinition(
                    FlowReynoldsArtifactIds.ReynoldsNumberStreamId,
                    "Reynolds Number",
                    "transform",
                    "scalar<double>",
                    "runtime"),
                new StreamDefinition(
                    FlowReynoldsArtifactIds.FlowRateStreamId,
                    "Flow Rate",
                    "transform",
                    "scalar<double>",
                    "runtime"),
                new StreamDefinition(
                    FlowReynoldsArtifactIds.MeanTemperatureStreamId,
                    "Mean Temperature",
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
                    FlowReynoldsArtifactIds.PrimaryControlTargetId,
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    FlowReynoldsArtifactIds.ReynoldsNumberStreamId,
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: FlowReynoldsArtifactIds.PrimaryControlTargetParameterId)
            ]);
        var device = new DeviceDefinition(
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
            new ArtifactId("role.flow_actuator"),
            device.Id,
            device.Protocol.Id,
            [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")]);

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [device],
            [binding],
            new Dictionary<ArtifactId, string>
            {
                [FlowReynoldsArtifactIds.PrimaryControlTargetParameterId] = "1600"
            });
    }
}
