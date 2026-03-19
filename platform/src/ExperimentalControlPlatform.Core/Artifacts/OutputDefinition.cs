using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class OutputDefinition
{
    public OutputDefinition(ArtifactId id, string name, string kind, string description)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Kind = RequireText(kind, nameof(kind));
        Description = RequireText(description, nameof(description));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Kind { get; }

    public string Description { get; }

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
