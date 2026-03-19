using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class MonitorDefinition
{
    private static readonly string[] AllowedSemanticShapes =
    [
        "display-only",
        "derived-state",
        "signal-producing"
    ];

    public MonitorDefinition(
        ArtifactId id,
        string name,
        string description,
        string semanticShape,
        IReadOnlyList<ArtifactId> observedStreamIds)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
        SemanticShape = RequireShape(semanticShape, nameof(semanticShape));
        ObservedStreamIds = CopyObservedStreamIds(observedStreamIds, nameof(observedStreamIds));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Description { get; }

    public string SemanticShape { get; }

    public IReadOnlyList<ArtifactId> ObservedStreamIds { get; }

    private static IReadOnlyList<ArtifactId> CopyObservedStreamIds(IReadOnlyList<ArtifactId> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.Select(value => ArtifactId.Require(value, paramName)).ToArray();
    }

    private static string RequireShape(string value, string paramName)
    {
        var normalized = RequireText(value, paramName);
        if (!AllowedSemanticShapes.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ArgumentException("semanticShape must be one of the V1 monitor semantic shapes.", paramName);
        }

        return normalized;
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
