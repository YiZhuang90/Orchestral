using System;
using System.Collections.Generic;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class RoleBindingDefinitionTests
{
    [Fact]
    public void Constructor_Captures_Role_Device_Protocol_And_Satisfied_Capabilities()
    {
        var binding = new RoleBindingDefinition(
            new ArtifactId("role.camera_upstream"),
            new ArtifactId("device.camera_01"),
            new ArtifactId("protocol.vendor_sdk_camera_v1"),
            [new ArtifactId("cap.frame_stream"), new ArtifactId("cap.exposure_control")],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.camera_exposure_ms")] = "0.35"
            });

        Assert.Equal(new ArtifactId("role.camera_upstream"), binding.RoleId);
        Assert.Equal(new ArtifactId("device.camera_01"), binding.DeviceId);
        Assert.Equal(new ArtifactId("protocol.vendor_sdk_camera_v1"), binding.ProtocolId);
        Assert.Equal(2, binding.SatisfiedCapabilityIds.Count);
        Assert.Equal("0.35", binding.ParameterValues[new ArtifactId("param.camera_exposure_ms")]);
    }

    [Fact]
    public void Constructor_Rejects_Default_Ids_And_Empty_Satisfied_Capabilities()
    {
        Assert.Throws<ArgumentException>(() =>
            new RoleBindingDefinition(
                default,
                new ArtifactId("device.camera_01"),
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                [new ArtifactId("cap.frame_stream")],
                new Dictionary<ArtifactId, string>()));

        Assert.Throws<ArgumentException>(() =>
            new RoleBindingDefinition(
                new ArtifactId("role.camera_upstream"),
                default,
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                [new ArtifactId("cap.frame_stream")],
                new Dictionary<ArtifactId, string>()));

        Assert.Throws<ArgumentException>(() =>
            new RoleBindingDefinition(
                new ArtifactId("role.camera_upstream"),
                new ArtifactId("device.camera_01"),
                default,
                [new ArtifactId("cap.frame_stream")],
                new Dictionary<ArtifactId, string>()));

        var capabilityException = Assert.Throws<ArgumentException>(() =>
            new RoleBindingDefinition(
                new ArtifactId("role.camera_upstream"),
                new ArtifactId("device.camera_01"),
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                [],
                new Dictionary<ArtifactId, string>()));

        Assert.Contains("SatisfiedCapabilityIds", capabilityException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_Rejects_Default_Parameter_Key()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RoleBindingDefinition(
                new ArtifactId("role.camera_upstream"),
                new ArtifactId("device.camera_01"),
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                [new ArtifactId("cap.frame_stream")],
                new Dictionary<ArtifactId, string>
                {
                    [default] = "0.35"
                }));

        Assert.Contains("parameterValues", exception.ParamName, StringComparison.OrdinalIgnoreCase);
    }
}
