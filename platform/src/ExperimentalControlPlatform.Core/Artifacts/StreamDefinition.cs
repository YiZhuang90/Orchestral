using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class StreamDefinition
{
    public StreamDefinition(
        ArtifactId id,
        string name,
        string producerKind,
        string schema,
        string timeBasis)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        ProducerKind = RequireText(producerKind, nameof(producerKind));
        Schema = RequireText(schema, nameof(schema));
        TimeBasis = RequireText(timeBasis, nameof(timeBasis));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string ProducerKind { get; }

    public string Schema { get; }

    public string TimeBasis { get; }

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
