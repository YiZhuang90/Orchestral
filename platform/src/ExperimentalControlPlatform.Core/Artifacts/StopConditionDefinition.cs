using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class StopConditionDefinition
{
    public StopConditionDefinition(
        ArtifactId id,
        string name,
        string kind,
        string condition,
        string action,
        string reason)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Kind = RequireText(kind, nameof(kind));
        Condition = RequireText(condition, nameof(condition));
        Action = RequireText(action, nameof(action));
        Reason = RequireText(reason, nameof(reason));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Kind { get; }

    public string Condition { get; }

    public string Action { get; }

    public string Reason { get; }

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
