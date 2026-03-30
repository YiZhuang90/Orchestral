using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class RunContextDefinition
{
    public RunContextDefinition(
        ArtifactId id,
        ResolvedExperimentDefinition experiment,
        string? displayName,
        string? operatorNote,
        IReadOnlyDictionary<ArtifactId, string> metadataValues,
        IReadOnlyDictionary<ArtifactId, ArtifactId> appliedSettingsSnapshotIds,
        IReadOnlyList<ArtifactId> decisionLogEntryIds,
        IReadOnlyList<ArtifactId> artifactReferenceIds)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Experiment = experiment ?? throw new ArgumentNullException(nameof(experiment));
        DisplayName = NormalizeOptional(displayName);
        OperatorNote = NormalizeOptional(operatorNote);
        MetadataValues = CopyArtifactKeyedStrings(metadataValues, nameof(metadataValues));
        AppliedSettingsSnapshotIds = CopyArtifactDictionary(appliedSettingsSnapshotIds, nameof(appliedSettingsSnapshotIds));
        DecisionLogEntryIds = CopyArtifactList(decisionLogEntryIds, nameof(decisionLogEntryIds));
        ArtifactReferenceIds = CopyArtifactList(artifactReferenceIds, nameof(artifactReferenceIds));
    }

    public ArtifactId Id { get; }

    public ResolvedExperimentDefinition Experiment { get; }

    public ArtifactId ExperimentId => Experiment.Experiment.Id;

    public string? DisplayName { get; }

    public string? OperatorNote { get; }

    public IReadOnlyDictionary<ArtifactId, string> MetadataValues { get; }

    public IReadOnlyDictionary<ArtifactId, ArtifactId> AppliedSettingsSnapshotIds { get; }

    public IReadOnlyList<ArtifactId> DecisionLogEntryIds { get; }

    public IReadOnlyList<ArtifactId> ArtifactReferenceIds { get; }

    private static IReadOnlyDictionary<ArtifactId, string> CopyArtifactKeyedStrings(
        IReadOnlyDictionary<ArtifactId, string> values,
        string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToDictionary(
            entry => ArtifactId.Require(entry.Key, paramName),
            entry => RequireText(entry.Value, paramName));
    }

    private static IReadOnlyDictionary<ArtifactId, ArtifactId> CopyArtifactDictionary(
        IReadOnlyDictionary<ArtifactId, ArtifactId> values,
        string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToDictionary(
            entry => ArtifactId.Require(entry.Key, paramName),
            entry => ArtifactId.Require(entry.Value, paramName));
    }

    private static IReadOnlyList<ArtifactId> CopyArtifactList(IReadOnlyList<ArtifactId> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.Select(value => ArtifactId.Require(value, paramName)).ToArray();
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{ToDisplayName(paramName)} is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ToDisplayName(string paramName) =>
        char.ToUpperInvariant(paramName[0]) + paramName[1..];
}
