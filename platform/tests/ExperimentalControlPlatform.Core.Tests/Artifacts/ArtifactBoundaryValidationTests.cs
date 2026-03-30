using System;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ArtifactBoundaryValidationTests
{
    [Fact]
    public void Public_Artifact_Constructors_Reject_Default_Id()
    {
        Assert.Throws<ArgumentException>(() => new CapabilityDefinition(default, "frame_stream", "Produces image frames."));
        Assert.Throws<ArgumentException>(() => new ParameterDefinition(default, "Sample Period", "integer", "experiment"));
        Assert.Throws<ArgumentException>(() => new ProtocolDefinition(default, "SDK Camera", "sdk", "request-response", "session", "low-latency", "fault", "retry"));
        Assert.Throws<ArgumentException>(() => new StreamDefinition(default, "temperature_inlet.samples", "device", "scalar_sample_stream", "sample"));
        Assert.Throws<ArgumentException>(() => new OutputDefinition(default, "Run Manifest", "run_manifest", "Structured summary."));
        Assert.Throws<ArgumentException>(() => new StopConditionDefinition(default, "Over Temperature", "safety-based", "x > y", "stop", "limit exceeded"));
        Assert.Throws<ArgumentException>(() => new TransformDefinition(default, "Transform", "Computes derived values.", [new ArtifactId("stream.raw")], [new ArtifactId("stream.derived")]));
        Assert.Throws<ArgumentException>(() => new ExperimentDefinition(default, "Experiment", "Purpose", [CreateRole()], [], [], [], [], [], []));
        Assert.Throws<ArgumentException>(() => new DeviceDefinition(default, "Camera 01", "camera_01", CreateProtocol(), [CreateCapability()], [], "healthy"));
    }

    [Fact]
    public void TransformDefinition_Rejects_Default_Stream_Ids_In_Collections()
    {
        Assert.Throws<ArgumentException>(() =>
            new TransformDefinition(
                new ArtifactId("transform.compute"),
                "Compute",
                "Computes derived values.",
                [default],
                [new ArtifactId("stream.derived")]));

        Assert.Throws<ArgumentException>(() =>
            new TransformDefinition(
                new ArtifactId("transform.compute"),
                "Compute",
                "Computes derived values.",
                [new ArtifactId("stream.raw")],
                [default]));
    }

    [Fact]
    public void RunManifestDefinition_Rejects_Default_Ids_In_Artifacts_And_Collections()
    {
        Assert.Throws<ArgumentException>(() =>
            new RunManifestDefinition(
                new ArtifactId("manifest.run_001"),
                new ArtifactId("exp.turbulence_transition_v1"),
                "1.0.0",
                new Dictionary<ArtifactId, ArtifactId> { [default] = new ArtifactId("device.camera_01") },
                new Dictionary<ArtifactId, ArtifactId> { [new ArtifactId("role.upstream_camera")] = new ArtifactId("protocol.sdk_camera_v1") },
                new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>(),
                new Dictionary<ArtifactId, string>(),
                DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
                null,
                null,
                null,
                [],
                [],
                []));

        Assert.Throws<ArgumentException>(() =>
            new RunManifestDefinition(
                new ArtifactId("manifest.run_001"),
                new ArtifactId("exp.turbulence_transition_v1"),
                "1.0.0",
                new Dictionary<ArtifactId, ArtifactId> { [new ArtifactId("role.upstream_camera")] = default },
                new Dictionary<ArtifactId, ArtifactId> { [new ArtifactId("role.upstream_camera")] = new ArtifactId("protocol.sdk_camera_v1") },
                new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>(),
                new Dictionary<ArtifactId, string>(),
                DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
                null,
                null,
                null,
                [],
                [],
                []));

        Assert.Throws<ArgumentException>(() =>
            new RunManifestDefinition(
                new ArtifactId("manifest.run_001"),
                new ArtifactId("exp.turbulence_transition_v1"),
                "1.0.0",
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, ArtifactId> { [new ArtifactId("role.upstream_camera")] = default },
                new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>(),
                new Dictionary<ArtifactId, string>(),
                DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
                null,
                null,
                null,
                [],
                [],
                []));

        Assert.Throws<ArgumentException>(() =>
            new RunManifestDefinition(
                new ArtifactId("manifest.run_001"),
                new ArtifactId("exp.turbulence_transition_v1"),
                "1.0.0",
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>(),
                new Dictionary<ArtifactId, string>(),
                DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
                null,
                default,
                null,
                [default],
                [],
                []));

        Assert.Throws<ArgumentException>(() =>
            new RunManifestDefinition(
                new ArtifactId("manifest.run_001"),
                new ArtifactId("exp.turbulence_transition_v1"),
                "1.0.0",
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>(),
                new Dictionary<ArtifactId, string> { [default] = "50" },
                DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
                null,
                null,
                null,
                [],
                [],
                []));

        Assert.Throws<ArgumentException>(() =>
            new RunManifestDefinition(
                new ArtifactId("manifest.run_001"),
                new ArtifactId("exp.turbulence_transition_v1"),
                "1.0.0",
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>
                {
                    [default] = new Dictionary<ArtifactId, string>()
                },
                new Dictionary<ArtifactId, string>(),
                DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
                null,
                null,
                null,
                [],
                [],
                []));

        Assert.Throws<ArgumentException>(() =>
            new RunManifestDefinition(
                new ArtifactId("manifest.run_001"),
                new ArtifactId("exp.turbulence_transition_v1"),
                "1.0.0",
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, ArtifactId>(),
                new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>
                {
                    [new ArtifactId("role.upstream_camera")] = new Dictionary<ArtifactId, string>
                    {
                        [default] = "0.35"
                    }
                },
                new Dictionary<ArtifactId, string>(),
                DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
                null,
                null,
                null,
                [],
                [],
                []));
    }

    private static CapabilityDefinition CreateCapability() =>
        new(new ArtifactId("cap.frame_stream"), "frame_stream", "Produces image frames.");

    private static ProtocolDefinition CreateProtocol() =>
        new(
            new ArtifactId("protocol.sdk_camera_v1"),
            "SDK Camera",
            "sdk",
            "request-response",
            "session",
            "low-latency",
            "fault",
            "retry");

    private static DeviceRoleDefinition CreateRole() =>
        new(
            new ArtifactId("role.upstream_camera"),
            "Upstream Camera",
            "Captures the upstream flow view.",
            [new ArtifactId("cap.frame_stream")],
            new ArtifactId("protocol.sdk_camera_v1"));
}
