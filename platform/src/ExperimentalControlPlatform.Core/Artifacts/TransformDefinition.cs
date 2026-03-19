using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class TransformDefinition
{
    public TransformDefinition(
        ArtifactId id,
        string name,
        string description,
        IReadOnlyList<ArtifactId> inputStreamIds,
        IReadOnlyList<ArtifactId> outputStreamIds)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
        InputStreamIds = CopyRequired(inputStreamIds, nameof(inputStreamIds));
        OutputStreamIds = CopyRequired(outputStreamIds, nameof(outputStreamIds));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Description { get; }

    public IReadOnlyList<ArtifactId> InputStreamIds { get; }

    public IReadOnlyList<ArtifactId> OutputStreamIds { get; }

    private static IReadOnlyList<ArtifactId> CopyRequired(IReadOnlyList<ArtifactId> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (values.Count == 0)
        {
            throw new ArgumentException($"{ToDisplayName(paramName)} must contain at least one item.", paramName);
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

    private static string ToDisplayName(string paramName) =>
        char.ToUpperInvariant(paramName[0]) + paramName[1..];
}
