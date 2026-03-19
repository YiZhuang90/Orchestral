using System.Linq;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Devices.Capabilities;
using ExperimentalControlPlatform.Protocols;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class DeviceDefinitionTests
{
    [Fact]
    public void Constructor_Allows_Protocol_Kind_Capabilities_And_Health_Status_Expectation()
    {
        var protocolBehavior = new ProtocolSessionBehavior(
            ProtocolKind.VendorSdk,
            TransportKind.InProcess,
            requiresPersistentSession: true);
        var capabilityContracts = new[]
        {
            new DeviceCapabilityContract(
                new ArtifactId("cap.frame_stream"),
                CapabilityKind.FrameStream,
                "frame_stream",
                "Produces image frames."),
            new DeviceCapabilityContract(
                new ArtifactId("cap.health_status"),
                CapabilityKind.HealthStatus,
                "health_status",
                "Reports device health state.")
        };

        var device = new DeviceDefinition(
            new ArtifactId("device.camera_01"),
            "Camera 01",
            "camera_01",
            protocolBehavior.ToDefinition(
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                "Vendor SDK Camera",
                messageStyle: "request-response",
                timingExpectation: "best-effort",
                failureBehavior: "fail-fast",
                retryExpectation: "manual"),
            capabilityContracts.Select(static contract => contract.ToDefinition()).ToArray(),
            [],
            "heartbeat_required");

        Assert.Equal("Vendor SDK Camera", device.Protocol.Name);
        Assert.Equal(TransportKind.InProcess.ToString(), device.Protocol.TransportFamily);
        Assert.Equal("persistent", device.Protocol.ConnectionLifecycle);
        Assert.Equal("heartbeat_required", device.HealthStatus);
        Assert.Equal(2, device.Capabilities.Count);
        Assert.Equal(
            capabilityContracts.Select(static contract => contract.Name),
            device.Capabilities.Select(static capability => capability.Name));
    }

    [Fact]
    public void ProtocolSessionBehavior_Rejects_Unknown_Kind_And_Transport()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProtocolSessionBehavior(
                ProtocolKind.Unknown,
                TransportKind.InProcess,
                requiresPersistentSession: true));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProtocolSessionBehavior(
                ProtocolKind.VendorSdk,
                TransportKind.Unknown,
                requiresPersistentSession: true));
    }

    [Fact]
    public void DeviceCapabilityContract_Rejects_Unknown_Kind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DeviceCapabilityContract(
                new ArtifactId("cap.frame_stream"),
                CapabilityKind.Unknown,
                "frame_stream",
                "Produces image frames."));
    }
}
