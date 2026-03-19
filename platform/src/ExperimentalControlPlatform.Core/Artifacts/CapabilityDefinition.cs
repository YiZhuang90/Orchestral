using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class CapabilityDefinition
{
    public CapabilityDefinition(ArtifactId id, string name, string description)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

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
