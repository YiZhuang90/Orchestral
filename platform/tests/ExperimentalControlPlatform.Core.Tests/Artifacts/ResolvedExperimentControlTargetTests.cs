using System.Collections.Generic;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ResolvedExperimentControlTargetTests
{
    [Fact]
    public void Constructor_Captures_Control_Targets_From_Experiment_Definition()
    {
        var experiment = CreateExperimentDefinition(
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

        var resolved = new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [CreateControllerDeviceDefinition()],
            [CreateControllerBinding()],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.re_target")] = "1600",
                [new ArtifactId("param.re_schedule")] = "0:1600;10:1700"
            });

        Assert.Single(resolved.ControlTargets);
        Assert.Equal(new ArtifactId("control.re_primary"), resolved.ControlTargets[0].Id);
    }

    [Fact]
    public void Validate_Returns_Issue_For_Unknown_Control_Target_Command_Role()
    {
        var experiment = CreateExperimentDefinition(
            [
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.unknown_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))
            ]);

        var validation = ResolvedExperimentDefinition.Validate(
            experiment,
            [CreateControllerDeviceDefinition()],
            [CreateControllerBinding()],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.re_target")] = "1600",
                [new ArtifactId("param.re_schedule")] = "0:1600;10:1700"
            });

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "unknown_control_target_command_role");
    }

    [Fact]
    public void Validate_Returns_Issue_For_Missing_Control_Target_Parameter_Value()
    {
        var experiment = CreateExperimentDefinition(
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

        var validation = ResolvedExperimentDefinition.Validate(
            experiment,
            [CreateControllerDeviceDefinition()],
            [CreateControllerBinding()],
            new Dictionary<ArtifactId, string>());

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "missing_control_target_parameter_value");
    }

    [Fact]
    public void Validate_Returns_Issue_For_Unknown_Control_Target_Measured_Source()
    {
        var experiment = CreateExperimentDefinition(
            [
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.unknown_reynolds"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))
            ]);

        var validation = ResolvedExperimentDefinition.Validate(
            experiment,
            [CreateControllerDeviceDefinition()],
            [CreateControllerBinding()],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.re_target")] = "1600",
                [new ArtifactId("param.re_schedule")] = "0:1600;10:1700"
            });

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "unknown_control_target_measured_source");
    }

    [Fact]
    public void Validate_Returns_Issue_When_Control_Target_Uses_Monitor_Id_As_Measured_Source()
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
            [
                new MonitorDefinition(
                    new ArtifactId("monitor.live_re"),
                    "Live Re",
                    "display-only Re tracking surface.",
                    "display-only",
                    [new ArtifactId("stream.reynolds_number")])
            ],
            [],
            [],
            [
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("monitor.live_re"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))
            ]);

        var validation = ResolvedExperimentDefinition.Validate(
            experiment,
            [CreateControllerDeviceDefinition()],
            [CreateControllerBinding()],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.re_target")] = "1600",
                [new ArtifactId("param.re_schedule")] = "0:1600;10:1700"
            });

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "unknown_control_target_measured_source");
    }

    [Fact]
    public void Validate_Returns_Issue_For_Invalid_Control_Target_Schedule_Format()
    {
        var experiment = CreateExperimentDefinition(
            [
                new ControlTargetDefinition(
                    new ArtifactId("control.re_schedule"),
                    "Scheduled Re Control",
                    "Changes Reynolds number by run-time schedule.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "scheduled",
                    "closed_loop",
                    scheduleParameterId: new ArtifactId("param.re_schedule"))
            ]);

        var validation = ResolvedExperimentDefinition.Validate(
            experiment,
            [CreateControllerDeviceDefinition()],
            [CreateControllerBinding()],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.re_schedule")] = "not-a-schedule"
            });

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "invalid_control_target_schedule_format");
    }

    private static ExperimentDefinition CreateExperimentDefinition(IReadOnlyList<ControlTargetDefinition> controlTargets)
    {
        return new ExperimentDefinition(
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
                    unit: "dimensionless"),
                new ParameterDefinition(
                    new ArtifactId("param.re_schedule"),
                    "Re Schedule",
                    "time_series",
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
            controlTargets);
    }

    private static DeviceDefinition CreateControllerDeviceDefinition() =>
        new(
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

    private static RoleBindingDefinition CreateControllerBinding() =>
        new(
            new ArtifactId("role.flow_actuator"),
            new ArtifactId("device.controller_01"),
            new ArtifactId("protocol.serial_ascii_v1"),
            [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")]);
}
