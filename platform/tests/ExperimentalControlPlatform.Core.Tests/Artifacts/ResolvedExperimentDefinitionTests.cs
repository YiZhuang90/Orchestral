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
}
