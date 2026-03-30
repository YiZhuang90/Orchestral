using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;
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
        var controller = new ControllerUnitSession(
            experiment,
            new ArtifactId("control.re_primary"),
            started.StartedAtUtc!.Value);
        var monitor = new ExperimentMonitorSession(
            coordinator,
            [],
            clock: () => RunStartedAt.AddSeconds(5));

        monitor.AttachController(controller);
        await controller.SampleAsync(RunStartedAt.AddSeconds(5), 1588);

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
        var controller = new ControllerUnitSession(
            experiment,
            new ArtifactId("control.re_primary"),
            started.StartedAtUtc!.Value);
        var monitor = new ExperimentMonitorSession(
            coordinator,
            [],
            clock: () => RunStartedAt.AddSeconds(6));

        monitor.AttachController(controller);
        await controller.SampleAsync(RunStartedAt.AddSeconds(5), 1588);
        await controller.SampleAsync(RunStartedAt.AddSeconds(6));

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
                    new ArtifactId("param.re_target"),
                    "Re Target",
                    "float",
                    "experiment",
                    unit: "dimensionless")
            ],
            [
                new StreamDefinition(
                    new ArtifactId("stream.reynolds_number"),
                    "Reynolds Number",
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
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))
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
                [new ArtifactId("param.re_target")] = "1600"
            });
    }
}
