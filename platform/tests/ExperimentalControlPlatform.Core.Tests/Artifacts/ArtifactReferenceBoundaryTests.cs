using System;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ArtifactReferenceBoundaryTests
{
    [Fact]
    public void DeviceRoleDefinition_Uses_ArtifactId_For_Expected_Protocol()
    {
        var expectedProtocolId = new ArtifactId("protocol.sdk_camera_v1");

        var role = new DeviceRoleDefinition(
            new ArtifactId("role.upstream_camera"),
            "Upstream Camera",
            "Captures the upstream flow view.",
            [new ArtifactId("cap.frame_stream")],
            expectedProtocolId);

        Assert.Equal(expectedProtocolId, role.ExpectedProtocolId);
    }

    [Fact]
    public void MonitorDefinition_Uses_Stream_Artifact_Identifiers_For_Observed_Sources()
    {
        var streamId = new ArtifactId("stream.temperature_inlet_samples");

        var monitor = new MonitorDefinition(
            new ArtifactId("monitor.temperature_monitor"),
            "Temperature Monitor",
            "Shows live inlet temperature.",
            "derived-state",
            [streamId]);

        Assert.Single(monitor.ObservedStreamIds);
        Assert.Equal(streamId, monitor.ObservedStreamIds[0]);
    }

    [Fact]
    public void MonitorDefinition_Rejects_Default_Stream_Artifact_Identifiers()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new MonitorDefinition(
                new ArtifactId("monitor.temperature_monitor"),
                "Temperature Monitor",
                "Shows live inlet temperature.",
                "derived-state",
                [default]));

        Assert.Contains("default", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
