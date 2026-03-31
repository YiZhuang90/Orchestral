using System;
using System.Collections.Generic;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ExperimentDefinitionLintingTests
{
    [Fact]
    public void Lint_Returns_Issue_For_Unknown_Transform_Stream_Reference()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.invalid_transform_reference"),
            "Invalid Transform Reference",
            "References a missing stream from a transform.",
            [CreateCameraRole()],
            [],
            [CreateFrameStream()],
            [
                new TransformDefinition(
                    new ArtifactId("transform.compute_signal"),
                    "Compute Signal",
                    "Consumes a missing stream in V1.",
                    [new ArtifactId("stream.missing_input")],
                    [new ArtifactId("stream.signal")])
            ],
            [],
            [],
            []);

        var lint = experiment.Lint();

        Assert.False(lint.IsValid);
        Assert.Contains(lint.Issues, issue => issue.Code == "unknown_transform_input_stream");
        Assert.Contains(lint.Issues, issue => issue.Code == "unknown_transform_output_stream");
    }

    [Fact]
    public void Lint_Returns_Issue_For_Unknown_Monitor_Stream_Reference()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.invalid_monitor_reference"),
            "Invalid Monitor Reference",
            "References a missing stream from a monitor.",
            [CreateCameraRole()],
            [],
            [CreateFrameStream()],
            [],
            [
                new MonitorDefinition(
                    new ArtifactId("monitor.live_signal"),
                    "Live Signal",
                    "Displays a missing signal.",
                    "display-only",
                    [new ArtifactId("stream.signal_missing")])
            ],
            [],
            []);

        var lint = experiment.Lint();

        Assert.False(lint.IsValid);
        Assert.Contains(lint.Issues, issue => issue.Code == "unknown_monitor_stream");
    }

    [Fact]
    public void Lint_Returns_Issue_For_Unknown_Control_Target_References()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.invalid_control_target_reference"),
            "Invalid Control Target Reference",
            "References missing role, stream, and parameter ids from a control target.",
            [CreateCameraRole()],
            [],
            [CreateFrameStream()],
            [],
            [],
            [],
            [],
            [
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re",
                    "Uses missing authored references.",
                    new ArtifactId("stream.reynolds_missing"),
                    new ArtifactId("role.control_center_missing"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target_missing"))
            ]);

        var lint = experiment.Lint();

        Assert.False(lint.IsValid);
        Assert.Contains(lint.Issues, issue => issue.Code == "unknown_control_target_measured_source");
        Assert.Contains(lint.Issues, issue => issue.Code == "unknown_control_target_command_role");
        Assert.Contains(lint.Issues, issue => issue.Code == "unknown_control_target_parameter");
    }

    [Fact]
    public void Lint_Returns_Issue_For_Duplicate_Stream_Id()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.duplicate_streams"),
            "Duplicate Streams",
            "Declares the same stream id twice.",
            [CreateCameraRole()],
            [],
            [
                CreateFrameStream(),
                new StreamDefinition(
                    new ArtifactId("stream.frame"),
                    "Frame Duplicate",
                    "device",
                    "image",
                    "sample")
            ],
            [],
            [],
            [],
            []);

        var lint = experiment.Lint();

        Assert.False(lint.IsValid);
        Assert.Contains(lint.Issues, issue => issue.Code == "duplicate_stream_definition");
    }

    [Fact]
    public void Lint_Returns_Issue_For_Duplicate_Role_Id()
    {
        var cameraRole = CreateCameraRole();
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.duplicate_roles"),
            "Duplicate Roles",
            "Declares the same role id twice.",
            [
                cameraRole,
                new DeviceRoleDefinition(
                    cameraRole.Id,
                    "Upstream Camera Duplicate",
                    "Duplicates the role id.",
                    [new ArtifactId("cap.frame_stream")],
                    new ArtifactId("protocol.vendor_sdk_camera_v1"))
            ],
            [],
            [CreateFrameStream()],
            [],
            [],
            [],
            []);

        var lint = experiment.Lint();

        Assert.False(lint.IsValid);
        Assert.Contains(lint.Issues, issue => issue.Code == "duplicate_role_definition");
    }

    [Fact]
    public void Lint_Returns_Issue_For_Unknown_Control_Target_Schedule_Parameter()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.invalid_control_target_schedule_parameter"),
            "Invalid Control Target Schedule Parameter",
            "References a missing schedule parameter from a scheduled control target.",
            [CreateControlRole()],
            [],
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
                    new ArtifactId("control.re_schedule"),
                    "Scheduled Re",
                    "Uses a missing schedule parameter.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.control_center"),
                    "scheduled",
                    "closed_loop",
                    scheduleParameterId: new ArtifactId("param.re_schedule_missing"))
            ]);

        var lint = experiment.Lint();

        Assert.False(lint.IsValid);
        Assert.Contains(lint.Issues, issue => issue.Code == "unknown_control_target_schedule_parameter");
    }

    [Fact]
    public void Lint_Returns_Valid_For_Clean_Experiment_Definition()
    {
        var frameStream = CreateFrameStream();
        var reynoldsStream = new StreamDefinition(
            new ArtifactId("stream.reynolds_number"),
            "Reynolds Number",
            "transform",
            "scalar<double>",
            "runtime");
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.clean_lint_case"),
            "Clean Lint Case",
            "A structurally valid authored experiment package.",
            [CreateCameraRole(), CreateControlRole()],
            [
                new ParameterDefinition(
                    new ArtifactId("param.re_target"),
                    "Re Target",
                    "float",
                    "experiment",
                    defaultValue: "1600")
            ],
            [frameStream, reynoldsStream],
            [
                new TransformDefinition(
                    new ArtifactId("transform.compute_reynolds"),
                    "Compute Reynolds",
                    "Produces a Reynolds stream from frames in this test fixture.",
                    [frameStream.Id],
                    [reynoldsStream.Id])
            ],
            [
                new MonitorDefinition(
                    new ArtifactId("monitor.reynolds"),
                    "Reynolds Monitor",
                    "Shows the derived Reynolds state.",
                    "derived-state",
                    [reynoldsStream.Id])
            ],
            [
                new StopConditionDefinition(
                    new ArtifactId("stop.manual"),
                    "Manual Stop",
                    "manual",
                    "operator stop",
                    "stop",
                    "manual stop")
            ],
            [
                new OutputDefinition(
                    new ArtifactId("output.run_manifest"),
                    "Run Manifest",
                    "run_manifest",
                    "Structured run output.")
            ],
            [
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re",
                    "Maintains the primary Reynolds target.",
                    reynoldsStream.Id,
                    new ArtifactId("role.control_center"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))
            ]);

        var lint = experiment.Lint();

        Assert.True(lint.IsValid);
        Assert.Empty(lint.Issues);
    }

    private static DeviceRoleDefinition CreateCameraRole() =>
        new(
            new ArtifactId("role.camera_upstream"),
            "Upstream Camera",
            "Captures the upstream view.",
            [new ArtifactId("cap.frame_stream")],
            new ArtifactId("protocol.vendor_sdk_camera_v1"));

    private static DeviceRoleDefinition CreateControlRole() =>
        new(
            new ArtifactId("role.control_center"),
            "Control Center",
            "Controls the experiment actuation path.",
            [new ArtifactId("cap.pulse_actuation")],
            new ArtifactId("protocol.serial_ascii_v1"));

    private static StreamDefinition CreateFrameStream() =>
        new(
            new ArtifactId("stream.frame"),
            "Frame",
            "device",
            "image",
            "sample");
}
