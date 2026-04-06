using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Devices.ControlCenter;
using ExperimentalControlPlatform.ExperimentLogic;
using ExperimentalControlPlatform.Runtime;
using Xunit;

namespace ExperimentalControlPlatform.ExperimentLogic.Tests;

/// <summary>
/// Integration tests proving the L2/L3 boundary:
/// FlowReynoldsDerivedStateSession (L3) implements IDerivedStateSession (L2)
/// and derived state can be consumed through the interface contract.
/// </summary>
public sealed class FlowReynoldsIntegrationTests
{
    private static readonly DateTimeOffset RunStartedAt = new(2026, 3, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FlowReynoldsDerivedStateSession_Implements_IDerivedStateSession()
    {
        await using var session = CreateSession();
        Assert.IsAssignableFrom<IDerivedStateSession>(session);
    }

    [Fact]
    public async Task FlowReynoldsDerivedStateSnapshot_Implements_IDerivedStateSnapshot()
    {
        await using var session = CreateSession();
        var snapshot = session.State.Current;
        Assert.IsAssignableFrom<IDerivedStateSnapshot>(snapshot);
    }

    [Fact]
    public async Task CurrentDerivedState_Returns_Snapshot_Through_Interface()
    {
        await using var session = CreateSession();
        IDerivedStateSession interfaceSession = session;

        session.RecordPulseReadback(new ControlCenterPulseReadback(
            "control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)));
        session.RecordPulseReadback(new ControlCenterPulseReadback(
            "control_center_01", "Control Center", 2.0, 14, RunStartedAt.AddSeconds(2)));

        var derivedSnapshot = interfaceSession.CurrentDerivedState;
        Assert.NotNull(derivedSnapshot);
        Assert.NotNull(derivedSnapshot!.ReynoldsNumber);
        Assert.NotNull(derivedSnapshot.FilteredFlowRateLitersPerMinute);
        Assert.NotNull(derivedSnapshot.MeanTemperatureC);
        Assert.NotNull(derivedSnapshot.ObservedAtUtc);
    }

    [Fact]
    public async Task DerivedStateChanged_Fires_When_State_Updates()
    {
        await using var session = CreateSession();
        IDerivedStateSession interfaceSession = session;
        var changeCount = 0;
        interfaceSession.DerivedStateChanged += () => changeCount++;

        session.RecordTemperatureSample(new Pt104Reading("pt104_01", 2, 20.8, RunStartedAt.AddSeconds(1), "LiveRead"));
        var afterTemperature = changeCount;

        session.RecordPulseReadback(new ControlCenterPulseReadback(
            "control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)));
        var afterFirstPulse = changeCount;

        session.RecordPulseReadback(new ControlCenterPulseReadback(
            "control_center_01", "Control Center", 2.0, 14, RunStartedAt.AddSeconds(2)));
        var afterSecondPulse = changeCount;

        Assert.True(afterTemperature > 0, "DerivedStateChanged should fire on temperature update");
        Assert.True(afterFirstPulse > afterTemperature, "DerivedStateChanged should fire on pulse update");
        Assert.True(afterSecondPulse > afterFirstPulse, "DerivedStateChanged should fire on each pulse");
    }

    [Fact]
    public async Task Interface_Snapshot_Fields_Match_Concrete_Snapshot()
    {
        await using var session = CreateSession();
        IDerivedStateSession interfaceSession = session;

        session.RecordTemperatureSample(new Pt104Reading("pt104_01", 2, 20.8, RunStartedAt.AddSeconds(1), "LiveRead"));
        session.RecordTemperatureSample(new Pt104Reading("pt104_01", 4, 21.2, RunStartedAt.AddSeconds(1), "LiveRead"));
        session.RecordPulseReadback(new ControlCenterPulseReadback(
            "control_center_01", "Control Center", 1.0, 10, RunStartedAt.AddSeconds(1)));
        session.RecordPulseReadback(new ControlCenterPulseReadback(
            "control_center_01", "Control Center", 2.0, 14, RunStartedAt.AddSeconds(2)));

        var concrete = session.State.Current!;
        var throughInterface = interfaceSession.CurrentDerivedState!;

        Assert.Equal(concrete.ObservedAtUtc, throughInterface.ObservedAtUtc);
        Assert.Equal(concrete.FilteredFlowRateLitersPerMinute, throughInterface.FilteredFlowRateLitersPerMinute);
        Assert.Equal(concrete.ReynoldsNumber, throughInterface.ReynoldsNumber);
        Assert.Equal(concrete.MeanTemperatureC, throughInterface.MeanTemperatureC);
        Assert.Equal(concrete.UsesFallbackTemperature, throughInterface.UsesFallbackTemperature);
        Assert.Equal(concrete.PulseTelemetryIsStale, throughInterface.PulseTelemetryIsStale);
        Assert.Equal(concrete.TemperatureIsStale, throughInterface.TemperatureIsStale);
    }

    private static FlowReynoldsDerivedStateSession CreateSession()
    {
        return new FlowReynoldsDerivedStateSession(CreateResolvedExperimentDefinition());
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
                    "Re Target", "float", "experiment", unit: "dimensionless"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PulsesPerLiterParameterId,
                    "Pulses Per Liter", "integer", "experiment", defaultValue: "80"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PipeInnerDiameterParameterId,
                    "Pipe Inner Diameter", "float", "experiment", unit: "m", defaultValue: "0.00403"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PipeLengthParameterId,
                    "Pipe Length", "float", "experiment", unit: "m", defaultValue: "1.0"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PipeRoughnessParameterId,
                    "Pipe Roughness", "float", "experiment", unit: "m", defaultValue: "0.0"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.ReferenceTemperatureParameterId,
                    "Reference Temperature", "float", "experiment", unit: "C", defaultValue: "20.95"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.FlowrateAverageCountParameterId,
                    "Flowrate Average Count", "integer", "experiment", defaultValue: "100"),
                new ParameterDefinition(
                    FlowReynoldsArtifactIds.PulsePollIntervalMillisecondsParameterId,
                    "Pulse Poll Interval", "integer", "experiment", unit: "ms", defaultValue: "500")
            ],
            [
                new StreamDefinition(
                    FlowReynoldsArtifactIds.FlowRateStreamId,
                    "Flow Rate", "transform", "scalar<double>", "runtime"),
                new StreamDefinition(
                    FlowReynoldsArtifactIds.ReynoldsNumberStreamId,
                    "Reynolds Number", "transform", "scalar<double>", "runtime"),
                new StreamDefinition(
                    FlowReynoldsArtifactIds.MeanTemperatureStreamId,
                    "Mean Temperature", "transform", "scalar<double>", "runtime")
            ],
            [], [], [], [],
            [
                new ControlTargetDefinition(
                    FlowReynoldsArtifactIds.PrimaryControlTargetId,
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    FlowReynoldsArtifactIds.ReynoldsNumberStreamId,
                    new ArtifactId("role.control_center"),
                    "constant", "closed_loop",
                    targetParameterId: FlowReynoldsArtifactIds.PrimaryControlTargetParameterId)
            ]);
        var device = new DeviceDefinition(
            new ArtifactId("device.control_center_01"),
            "Control Center", "control_center_01",
            new ProtocolDefinition(
                new ArtifactId("protocol.serial_ascii_v1"),
                "Serial ASCII", "serial", "request-response",
                "session", "best-effort", "fail-fast", "manual"),
            [new CapabilityDefinition(new ArtifactId("cap.command_result"), "command_result", "Produces command and telemetry readback.")],
            [], "healthy");
        var binding = new RoleBindingDefinition(
            new ArtifactId("role.control_center"),
            device.Id, device.Protocol.Id,
            [new ArtifactId("cap.command_result")]);

        return new ResolvedExperimentDefinition(
            experiment, "1.0.0", [device], [binding],
            new Dictionary<ArtifactId, string>
            {
                [FlowReynoldsArtifactIds.PrimaryControlTargetParameterId] = "1600"
            });
    }
}
