using System;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class RunManifestDefinitionTests
{
    [Fact]
    public void Constructor_Captures_Protocol_Identifiers_For_Role_Bindings()
    {
        var manifest = new RunManifestDefinition(
            new ArtifactId("manifest.run_001"),
            new ArtifactId("exp.turbulence_transition_v1"),
            "1.0.0",
            new Dictionary<ArtifactId, ArtifactId>
            {
                [new ArtifactId("role.upstream_camera")] = new ArtifactId("device.camera_01")
            },
            new Dictionary<ArtifactId, ArtifactId>
            {
                [new ArtifactId("role.upstream_camera")] = new ArtifactId("protocol.sdk_camera_v1")
            },
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.sample_period_ms")] = "50"
            },
            DateTimeOffset.Parse("2026-03-19T12:00:00+01:00"),
            DateTimeOffset.Parse("2026-03-19T12:05:00+01:00"),
            new ArtifactId("stop.over_temperature"),
            "temperature limit exceeded",
            [new ArtifactId("output.run_manifest")],
            ["run started", "run stopped"],
            []);

        Assert.Single(manifest.ProtocolBindings);
        Assert.Equal(
            new ArtifactId("protocol.sdk_camera_v1"),
            manifest.ProtocolBindings[new ArtifactId("role.upstream_camera")]);
    }
}
