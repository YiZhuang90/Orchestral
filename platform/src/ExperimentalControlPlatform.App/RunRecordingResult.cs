using System;
using System.Collections.Generic;
using System.Linq;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.App;

public sealed record class RunRecordingResult
{
    public RunRecordingResult(
        Guid runId,
        string runDirectoryPath,
        ArtifactId manifestId,
        string manifestPath,
        RunManifestDefinition manifest,
        IReadOnlyDictionary<ArtifactId, string> artifactPaths)
    {
        if (string.IsNullOrWhiteSpace(runDirectoryPath))
        {
            throw new ArgumentException("RunDirectoryPath is required.", nameof(runDirectoryPath));
        }

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("ManifestPath is required.", nameof(manifestPath));
        }

        RunId = runId;
        RunDirectoryPath = runDirectoryPath.Trim();
        ManifestId = ArtifactId.Require(manifestId, nameof(manifestId));
        ManifestPath = manifestPath.Trim();
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        ArtifactPaths = artifactPaths?.ToDictionary(
            entry => ArtifactId.Require(entry.Key, nameof(artifactPaths)),
            entry => string.IsNullOrWhiteSpace(entry.Value)
                ? throw new ArgumentException("Artifact path values are required.", nameof(artifactPaths))
                : entry.Value.Trim())
            ?? throw new ArgumentNullException(nameof(artifactPaths));
    }

    public Guid RunId { get; }

    public string RunDirectoryPath { get; }

    public ArtifactId ManifestId { get; }

    public string ManifestPath { get; }

    public RunManifestDefinition Manifest { get; }

    public IReadOnlyDictionary<ArtifactId, string> ArtifactPaths { get; }
}
