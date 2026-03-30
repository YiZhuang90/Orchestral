using System.Collections.Generic;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class RunContextDefinitionTests
{
    [Fact]
    public void Constructor_Captures_Run_Metadata_And_Artifact_References()
    {
        var context = new RunContextDefinition(
            new ArtifactId("runctx.transition_demo_001"),
            CreateResolvedExperimentDefinition(),
            "transition-demo-001",
            "looping mode, RR=0.36",
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("meta.operator")] = "yi",
                [new ArtifactId("meta.session_kind")] = "turbulence"
            },
            new Dictionary<ArtifactId, ArtifactId>
            {
                [new ArtifactId("device.camera_01")] = new ArtifactId("artifact.settings.camera_01"),
                [new ArtifactId("device.pt104_01")] = new ArtifactId("artifact.settings.pt104_01")
            },
            [new ArtifactId("decision.run_001")],
            [new ArtifactId("artifact.run_manifest_001")]);

        Assert.Equal(new ArtifactId("runctx.transition_demo_001"), context.Id);
        Assert.Equal(new ArtifactId("exp.turbulence_transition_v1"), context.ExperimentId);
        Assert.Equal("transition-demo-001", context.DisplayName);
        Assert.Equal("looping mode, RR=0.36", context.OperatorNote);
        Assert.Equal("yi", context.MetadataValues[new ArtifactId("meta.operator")]);
        Assert.Equal(
            new ArtifactId("artifact.settings.camera_01"),
            context.AppliedSettingsSnapshotIds[new ArtifactId("device.camera_01")]);
        Assert.Contains(new ArtifactId("decision.run_001"), context.DecisionLogEntryIds);
        Assert.Contains(new ArtifactId("artifact.run_manifest_001"), context.ArtifactReferenceIds);
    }

    [Fact]
    public void Constructor_Normalizes_Optional_Text_And_Copies_Metadata()
    {
        var originalMetadata = new Dictionary<ArtifactId, string>
        {
            [new ArtifactId("meta.operator")] = " yi "
        };
        var context = new RunContextDefinition(
            new ArtifactId("runctx.transition_demo_002"),
            CreateResolvedExperimentDefinition(),
            "  ",
            "   ",
            originalMetadata,
            new Dictionary<ArtifactId, ArtifactId>(),
            [],
            []);

        originalMetadata[new ArtifactId("meta.operator")] = "mutated";

        Assert.Null(context.DisplayName);
        Assert.Null(context.OperatorNote);
        Assert.Equal("yi", context.MetadataValues[new ArtifactId("meta.operator")]);
    }

    private static ResolvedExperimentDefinition CreateResolvedExperimentDefinition()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe the flow with a camera and a controller.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.camera_upstream"),
                    "Upstream Camera",
                    "Captures the upstream flow region.",
                    [new ArtifactId("cap.frame_stream")],
                    new ArtifactId("protocol.vendor_sdk_camera_v1"))
            ],
            [],
            [],
            [],
            [],
            [],
            []);
        var device = new DeviceDefinition(
            new ArtifactId("device.camera_01"),
            "Camera 01",
            "camera_01",
            new ProtocolDefinition(
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                "Vendor SDK Camera",
                "sdk",
                "request-response",
                "session",
                "best-effort",
                "fail-fast",
                "manual"),
            [
                new CapabilityDefinition(new ArtifactId("cap.frame_stream"), "frame_stream", "Produces image frames.")
            ],
            [],
            "healthy");
        var binding = new RoleBindingDefinition(
            new ArtifactId("role.camera_upstream"),
            device.Id,
            device.Protocol.Id,
            [new ArtifactId("cap.frame_stream")]);

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [device],
            [binding],
            new Dictionary<ArtifactId, string>());
    }
}
