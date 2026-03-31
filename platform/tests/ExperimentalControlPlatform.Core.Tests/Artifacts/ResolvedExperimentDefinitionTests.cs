using System;
using System.Collections.Generic;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ResolvedExperimentDefinitionTests
{
    [Fact]
    public void Constructor_Captures_Resolved_Experiment_With_Concrete_Role_Bindings()
    {
        var experiment = CreateExperimentDefinition();
        var upstreamCamera = CreateCameraDeviceDefinition("device.camera_01");
        var controller = CreateControllerDeviceDefinition("device.controller_01");
        var binding = new RoleBindingDefinition(
            experiment.Roles[0].Id,
            upstreamCamera.Id,
            upstreamCamera.Protocol.Id,
            experiment.Roles[0].RequiredCapabilityIds);
        var controllerBinding = new RoleBindingDefinition(
            experiment.Roles[1].Id,
            controller.Id,
            controller.Protocol.Id,
            experiment.Roles[1].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.actuator_pulse_ms")] = "2.0"
            });

        var resolved = new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [upstreamCamera, controller],
            [binding, controllerBinding],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.camera_exposure_ms")] = "0.35"
            });

        Assert.Equal("1.0.0", resolved.Version);
        Assert.Equal(experiment.Id, resolved.Experiment.Id);
        Assert.Equal(2, resolved.RoleBindings.Count);
        Assert.True(resolved.TryGetBinding(experiment.Roles[0].Id, out var upstreamResolvedBinding));
        Assert.Equal(upstreamCamera.Id, upstreamResolvedBinding!.DeviceId);
    }

    [Fact]
    public void Validate_Returns_Issue_For_Missing_Required_Role_Binding()
    {
        var experiment = CreateExperimentDefinition();
        var upstreamCamera = CreateCameraDeviceDefinition("device.camera_01");
        var binding = new RoleBindingDefinition(
            experiment.Roles[0].Id,
            upstreamCamera.Id,
            upstreamCamera.Protocol.Id,
            experiment.Roles[0].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>());

        var validation = ResolvedExperimentDefinition.Validate(
            experiment,
            [upstreamCamera],
            [binding],
            new Dictionary<ArtifactId, string>());

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "missing_role_binding");
    }

    [Fact]
    public void Constructor_Rejects_Capability_Mismatch()
    {
        var experiment = CreateExperimentDefinition();
        var incompleteCamera = new DeviceDefinition(
            new ArtifactId("device.camera_01"),
            "Camera 01",
            "camera_01",
            CreateCameraProtocol(),
            [new CapabilityDefinition(new ArtifactId("cap.frame_stream"), "frame_stream", "Produces image frames.")],
            [],
            "healthy");
        var binding = new RoleBindingDefinition(
            experiment.Roles[0].Id,
            incompleteCamera.Id,
            incompleteCamera.Protocol.Id,
            [new ArtifactId("cap.frame_stream")],
            new Dictionary<ArtifactId, string>());
        var controller = CreateControllerDeviceDefinition("device.controller_01");
        var controllerBinding = new RoleBindingDefinition(
            experiment.Roles[1].Id,
            controller.Id,
            controller.Protocol.Id,
            experiment.Roles[1].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.actuator_pulse_ms")] = "2.0"
            });

        var exception = Assert.Throws<ArgumentException>(() =>
            new ResolvedExperimentDefinition(
                experiment,
                "1.0.0",
                [incompleteCamera, controller],
                [binding, controllerBinding],
                new Dictionary<ArtifactId, string>
                {
                    [new ArtifactId("param.camera_exposure_ms")] = "0.35"
                }));

        Assert.Contains("capability", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_Rejects_Protocol_Mismatch()
    {
        var experiment = CreateExperimentDefinition();
        var upstreamCamera = CreateCameraDeviceDefinition("device.camera_01");
        var controller = CreateControllerDeviceDefinition("device.controller_01");
        var binding = new RoleBindingDefinition(
            experiment.Roles[0].Id,
            upstreamCamera.Id,
            new ArtifactId("protocol.serial_ascii_v1"),
            experiment.Roles[0].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>());
        var controllerBinding = new RoleBindingDefinition(
            experiment.Roles[1].Id,
            controller.Id,
            controller.Protocol.Id,
            experiment.Roles[1].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.actuator_pulse_ms")] = "2.0"
            });

        var exception = Assert.Throws<ArgumentException>(() =>
            new ResolvedExperimentDefinition(
                experiment,
                "1.0.0",
                [upstreamCamera, controller],
                [binding, controllerBinding],
                new Dictionary<ArtifactId, string>
                {
                    [new ArtifactId("param.camera_exposure_ms")] = "0.35"
                }));

        Assert.Contains("protocol", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_Returns_Issue_For_Missing_Required_Experiment_Parameter()
    {
        var experiment = CreateExperimentDefinition();
        var upstreamCamera = CreateCameraDeviceDefinition("device.camera_01");
        var controller = CreateControllerDeviceDefinition("device.controller_01");
        var upstreamBinding = new RoleBindingDefinition(
            experiment.Roles[0].Id,
            upstreamCamera.Id,
            upstreamCamera.Protocol.Id,
            experiment.Roles[0].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>());
        var controllerBinding = new RoleBindingDefinition(
            experiment.Roles[1].Id,
            controller.Id,
            controller.Protocol.Id,
            experiment.Roles[1].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.actuator_pulse_ms")] = "2.0"
            });

        var validation = ResolvedExperimentDefinition.Validate(
            experiment,
            [upstreamCamera, controller],
            [upstreamBinding, controllerBinding],
            new Dictionary<ArtifactId, string>());

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "missing_required_experiment_parameter");
    }

    [Fact]
    public void Constructor_Rejects_Missing_Required_Device_Binding_Parameter()
    {
        var experiment = CreateExperimentDefinition();
        var upstreamCamera = CreateCameraDeviceDefinition("device.camera_01");
        var controller = CreateControllerDeviceDefinition("device.controller_01");
        var upstreamBinding = new RoleBindingDefinition(
            experiment.Roles[0].Id,
            upstreamCamera.Id,
            upstreamCamera.Protocol.Id,
            experiment.Roles[0].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>());
        var controllerBinding = new RoleBindingDefinition(
            experiment.Roles[1].Id,
            controller.Id,
            controller.Protocol.Id,
            experiment.Roles[1].RequiredCapabilityIds,
            new Dictionary<ArtifactId, string>());

        var exception = Assert.Throws<ArgumentException>(() =>
            new ResolvedExperimentDefinition(
                experiment,
                "1.0.0",
                [upstreamCamera, controller],
                [upstreamBinding, controllerBinding],
                new Dictionary<ArtifactId, string>
                {
                    [new ArtifactId("param.camera_exposure_ms")] = "0.35"
                }));

        Assert.Contains("binding parameter", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCrossSession_Returns_Issue_For_Duplicate_Shared_Device_Capability_Claim()
    {
        var resolved = CreateResolvedExperimentWithDuplicateSharedDeviceCapabilityClaim();

        var validation = resolved.ValidateCrossSession();

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "duplicate_shared_device_capability_claim");
    }

    [Fact]
    public void ValidateCrossSession_Returns_Issue_For_Conflicting_Control_Target_Command_Role()
    {
        var resolved = CreateResolvedExperimentWithConflictingControlTargets();

        var validation = resolved.ValidateCrossSession();

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "conflicting_control_target_command_role");
    }

    [Fact]
    public void ValidateCrossSession_Returns_Valid_For_Clean_Resolved_Experiment()
    {
        var resolved = new ResolvedExperimentDefinition(
            CreateExperimentDefinition(),
            "1.0.0",
            [CreateCameraDeviceDefinition("device.camera_01"), CreateControllerDeviceDefinition("device.controller_01")],
            [
                new RoleBindingDefinition(
                    new ArtifactId("role.camera_upstream"),
                    new ArtifactId("device.camera_01"),
                    new ArtifactId("protocol.vendor_sdk_camera_v1"),
                    [new ArtifactId("cap.frame_stream"), new ArtifactId("cap.exposure_control")]),
                new RoleBindingDefinition(
                    new ArtifactId("role.flow_actuator"),
                    new ArtifactId("device.controller_01"),
                    new ArtifactId("protocol.serial_ascii_v1"),
                    [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")],
                    new Dictionary<ArtifactId, string>
                    {
                        [new ArtifactId("param.actuator_pulse_ms")] = "2.0"
                    })
            ],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.camera_exposure_ms")] = "0.35"
            });

        var validation = resolved.ValidateCrossSession();

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Issues);
    }

    private static ExperimentDefinition CreateExperimentDefinition()
    {
        return new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe the flow with an upstream camera and a trigger controller.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.camera_upstream"),
                    "Upstream Camera",
                    "Captures the upstream flow region.",
                    [new ArtifactId("cap.frame_stream"), new ArtifactId("cap.exposure_control")],
                    new ArtifactId("protocol.vendor_sdk_camera_v1")),
                new DeviceRoleDefinition(
                    new ArtifactId("role.flow_actuator"),
                    "Flow Actuator",
                    "Issues trigger pulses to the downstream actuator.",
                    [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")],
                    new ArtifactId("protocol.serial_ascii_v1"))
            ],
            [
                new ParameterDefinition(
                    new ArtifactId("param.camera_exposure_ms"),
                    "Camera Exposure",
                    "float",
                    "experiment",
                    unit: "ms")
            ],
            [],
            [],
            [],
            [],
            []);
    }

    private static DeviceDefinition CreateCameraDeviceDefinition(string deviceId) =>
        new(
            new ArtifactId(deviceId),
            deviceId,
            deviceId,
            CreateCameraProtocol(),
            [
                new CapabilityDefinition(new ArtifactId("cap.frame_stream"), "frame_stream", "Produces image frames."),
                new CapabilityDefinition(new ArtifactId("cap.exposure_control"), "exposure_control", "Adjusts camera exposure.")
            ],
            [],
            "healthy");

    private static DeviceDefinition CreateControllerDeviceDefinition(string deviceId) =>
        new(
            new ArtifactId(deviceId),
            deviceId,
            deviceId,
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
            [
                new ParameterDefinition(
                    new ArtifactId("param.actuator_pulse_ms"),
                    "Actuator Pulse",
                    "float",
                    "device-binding",
                    unit: "ms")
            ],
            "healthy");

    private static ProtocolDefinition CreateCameraProtocol() =>
        new(
            new ArtifactId("protocol.vendor_sdk_camera_v1"),
            "Vendor SDK Camera",
            "sdk",
            "request-response",
            "session",
            "best-effort",
            "fail-fast",
            "manual");

    private static ResolvedExperimentDefinition CreateResolvedExperimentWithDuplicateSharedDeviceCapabilityClaim()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.shared_device_capability_conflict"),
            "Shared Device Capability Conflict",
            "Exercise cross-session validation for ambiguous capability ownership on one concrete device.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.laser_control"),
                    "Laser Control",
                    "Controls the laser output.",
                    [new ArtifactId("cap.laser_control")],
                    new ArtifactId("protocol.serial_ascii_v1")),
                new DeviceRoleDefinition(
                    new ArtifactId("role.puff_actuation"),
                    "Puff Actuation",
                    "Controls puff actuation output.",
                    [new ArtifactId("cap.pulse_actuation")],
                    new ArtifactId("protocol.serial_ascii_v1"))
            ],
            [],
            [],
            [],
            [],
            [],
            []);

        var sharedDevice = new DeviceDefinition(
            new ArtifactId("device.control_center_01"),
            "Control Center 01",
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
                new CapabilityDefinition(new ArtifactId("cap.laser_control"), "laser_control", "Controls the laser."),
                new CapabilityDefinition(new ArtifactId("cap.pulse_actuation"), "pulse_actuation", "Issues puff pulses.")
            ],
            [],
            "healthy");

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [sharedDevice],
            [
                new RoleBindingDefinition(
                    new ArtifactId("role.laser_control"),
                    sharedDevice.Id,
                    sharedDevice.Protocol.Id,
                    [new ArtifactId("cap.laser_control"), new ArtifactId("cap.pulse_actuation")]),
                new RoleBindingDefinition(
                    new ArtifactId("role.puff_actuation"),
                    sharedDevice.Id,
                    sharedDevice.Protocol.Id,
                    [new ArtifactId("cap.pulse_actuation")])
            ],
            new Dictionary<ArtifactId, string>());
    }

    private static ResolvedExperimentDefinition CreateResolvedExperimentWithConflictingControlTargets()
    {
        var commandRoleId = new ArtifactId("role.control_center");
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.control_target_conflict"),
            "Control Target Conflict",
            "Exercise cross-session validation for multiple control targets on the same command role.",
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

        var controller = CreateControllerDeviceDefinition("device.controller_01");
        var binding = new RoleBindingDefinition(
            commandRoleId,
            controller.Id,
            controller.Protocol.Id,
            [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.actuator_pulse_ms")] = "2.0"
            });

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [controller],
            [binding],
            new Dictionary<ArtifactId, string>());
    }
}
