using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Devices.ControlCenter;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class FlowReynoldsDerivedStateSessionTests
{
    private static readonly DateTimeOffset RunStartedAt = new(2026, 3, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordPulseReadback_Computes_Filtered_Flow_And_Reynolds_From_Pulse_And_Temperature()
    {
        await using var session = new FlowReynoldsDerivedStateSession(CreateResolvedExperimentDefinition());

        session.RecordTemperatureSample(new Pt104Reading("pt104_01", 2, 20.8, RunStartedAt.AddSeconds(1), "LiveRead"));
        session.RecordTemperatureSample(new Pt104Reading("pt104_01", 4, 21.2, RunStartedAt.AddSeconds(1), "LiveRead"));
        session.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)));
        session.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 2.0, 14, RunStartedAt.AddSeconds(2)));

        var state = Assert.IsType<FlowReynoldsDerivedStateSnapshot>(session.State.Current);
        var expected = ComputeExpected(
            flowRateLitersPerMinute: 3.0,
            meanTemperatureC: 21.0,
            pipeDiameterMeters: 0.00403,
            pipeLengthMeters: 1.0,
            roughnessMeters: 0.0);

        Assert.Equal(3.0, state.RawFlowRateLitersPerMinute!.Value, 6);
        Assert.Equal(3.0, state.FilteredFlowRateLitersPerMinute!.Value, 6);
        Assert.Equal(21.0, state.MeanTemperatureC!.Value, 6);
        Assert.Equal(0.4, state.TemperatureDeltaC!.Value, 6);
        Assert.Equal(expected.BulkVelocityMetersPerSecond, state.BulkVelocityMetersPerSecond!.Value, 9);
        Assert.Equal(expected.ReynoldsNumber, state.ReynoldsNumber!.Value, 6);
        Assert.False(state.UsesFallbackTemperature);
        Assert.False(state.PulseTelemetryIsStale);
        Assert.False(state.TemperatureIsStale);
        Assert.Equal(1, state.SampleSequence);
    }

    [Fact]
    public async Task RecordPulseReadback_Uses_Fallback_Temperature_When_No_Pt104_Sample_Is_Available()
    {
        await using var session = new FlowReynoldsDerivedStateSession(CreateResolvedExperimentDefinition());

        session.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 10.0, 100, RunStartedAt.AddSeconds(10)));
        session.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 11.0, 101, RunStartedAt.AddSeconds(11)));

        var state = Assert.IsType<FlowReynoldsDerivedStateSnapshot>(session.State.Current);
        var expected = ComputeExpected(
            flowRateLitersPerMinute: 0.75,
            meanTemperatureC: 20.95,
            pipeDiameterMeters: 0.00403,
            pipeLengthMeters: 1.0,
            roughnessMeters: 0.0);

        Assert.True(state.UsesFallbackTemperature);
        Assert.Equal(20.95, state.MeanTemperatureC!.Value, 6);
        Assert.Equal(expected.ReynoldsNumber, state.ReynoldsNumber!.Value, 6);
    }

    [Fact]
    public async Task Samples_Stream_Can_Drive_ControllerUnit_Measured_Value()
    {
        var experiment = CreateResolvedExperimentDefinition();
        await using var derivedState = new FlowReynoldsDerivedStateSession(experiment);
        await using var controller = new ControllerUnitSession(
            experiment,
            FlowReynoldsArtifactIds.PrimaryControlTargetId,
            RunStartedAt);
        var applied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = derivedState.Samples.Subscribe(
            StreamDeliveryPolicy.Ordered(),
            async sample =>
            {
                await controller.SampleAsync(sample.ObservedAtUtc, sample.ReynoldsNumber);
                applied.TrySetResult();
            });

        derivedState.RecordTemperatureSample(new Pt104Reading("pt104_01", 2, 20.8, RunStartedAt.AddSeconds(1), "LiveRead"));
        derivedState.RecordTemperatureSample(new Pt104Reading("pt104_01", 4, 21.2, RunStartedAt.AddSeconds(1), "LiveRead"));
        derivedState.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)));
        derivedState.RecordPulseReadback(new ControlCenterPulseReadback("control_center_01", "Control Center", 2.0, 14, RunStartedAt.AddSeconds(2)));

        await applied.Task;

        var derivedSnapshot = Assert.IsType<FlowReynoldsDerivedStateSnapshot>(derivedState.State.Current);
        var controllerState = Assert.IsType<ControllerUnitState>(controller.State.Current);
        Assert.Equal(derivedSnapshot.ReynoldsNumber, controllerState.MeasuredValue);
        Assert.Equal(RunStartedAt.AddSeconds(2), controllerState.MeasuredValueObservedAtUtc);
    }

    [Fact]
    public async Task AttachRuntimeSourcesAsync_ConsumesExplicitFlowTelemetrySurface()
    {
        var service = new FakeControlCenterService(
            [
                new ControlCenterPulseReadback("control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)),
                new ControlCenterPulseReadback("control_center_01", "Control Center", 2.0, 14, RunStartedAt.AddSeconds(2))
            ]);
        await using var controlCenterSession = new ControlCenterSession(service, ControlCenterDeviceInfo.FromPortName("COM5"));
        await using var derivedState = new FlowReynoldsDerivedStateSession(CreateResolvedExperimentDefinition());

        await controlCenterSession.ConnectAsync();
        await derivedState.AttachRuntimeSourcesAsync(controlCenterSession, pt104Session: null);
        await controlCenterSession.ReadFlowTelemetryAsync();
        await controlCenterSession.ReadFlowTelemetryAsync();

        var flowTelemetry = Assert.IsType<ControlCenterFlowTelemetryState>(controlCenterSession.FlowTelemetry.Current);
        Assert.Equal(14, flowTelemetry.LastPulseCount);
        var state = Assert.IsType<FlowReynoldsDerivedStateSnapshot>(derivedState.State.Current);
        Assert.Equal(14, state.LatestPulseCount);
        Assert.NotNull(state.ReynoldsNumber);
    }

    private static ResolvedExperimentDefinition CreateResolvedExperimentDefinition()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe and hold Reynolds number near the requested target.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.control_center"),
                    "Control Center",
                    "Provides pulse telemetry and accepts control output.",
                    [new ArtifactId("cap.command_result")],
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
                    FlowReynoldsArtifactIds.FlowRateStreamId,
                    "Flow Rate",
                    "transform",
                    "scalar<double>",
                    "runtime"),
                new StreamDefinition(
                    FlowReynoldsArtifactIds.ReynoldsNumberStreamId,
                    "Reynolds Number",
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
                    new ArtifactId("role.control_center"),
                    "constant",
                    "closed_loop",
                    targetParameterId: FlowReynoldsArtifactIds.PrimaryControlTargetParameterId)
            ]);
        var device = new DeviceDefinition(
            new ArtifactId("device.control_center_01"),
            "Control Center",
            "control_center_01",
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
                new CapabilityDefinition(new ArtifactId("cap.command_result"), "command_result", "Produces command and telemetry readback.")
            ],
            [],
            "healthy");
        var binding = new RoleBindingDefinition(
            new ArtifactId("role.control_center"),
            device.Id,
            device.Protocol.Id,
            [new ArtifactId("cap.command_result")]);

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

    private static (double BulkVelocityMetersPerSecond, double ReynoldsNumber) ComputeExpected(
        double flowRateLitersPerMinute,
        double meanTemperatureC,
        double pipeDiameterMeters,
        double pipeLengthMeters,
        double roughnessMeters)
    {
        _ = pipeLengthMeters;
        _ = roughnessMeters;

        var density = (1 - Math.Pow(meanTemperatureC - 3.983035, 2) * (meanTemperatureC + 301.797) / (meanTemperatureC + 69.34881) / 522528.90) * 999.97495;
        var dynamicViscosity = Math.Exp(-3.7188 + 578.919 / (-137.546 + meanTemperatureC + 273.15)) / 1000;
        var kinematicViscosity = dynamicViscosity / density;
        var bulkVelocity = flowRateLitersPerMinute / 60 / 1000 / Math.PI / Math.Pow(pipeDiameterMeters, 2) * 4;
        var reynolds = bulkVelocity * pipeDiameterMeters / kinematicViscosity;
        return (bulkVelocity, reynolds);
    }

    private sealed class FakeControlCenterService(IReadOnlyList<ControlCenterPulseReadback> pulseReadbacks) : IControlCenterService
    {
        private readonly FakeControlCenterConnection _connection = new(ControlCenterDeviceInfo.FromPortName("COM5"));
        private int _pulseIndex;

        public IReadOnlyList<ControlCenterDeviceInfo> ListDevices() => [_connection.Device];

        public IControlCenterConnection Open(ControlCenterDeviceInfo device) => _connection;

        public void SendCommand(IControlCenterConnection connection, ControlCenterCommand command)
        {
        }

        public ControlCenterPulseReadback ReadPulseCount(IControlCenterConnection connection)
        {
            var index = Math.Min(_pulseIndex, pulseReadbacks.Count - 1);
            _pulseIndex++;
            return pulseReadbacks[index];
        }

        private sealed class FakeControlCenterConnection(ControlCenterDeviceInfo device) : IControlCenterConnection
        {
            public ControlCenterDeviceInfo Device { get; } = device;

            public void Dispose()
            {
            }
        }
    }
}
