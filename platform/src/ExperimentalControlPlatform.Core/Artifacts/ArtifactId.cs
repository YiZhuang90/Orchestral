using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public readonly record struct ArtifactId
{
    private readonly string? _value;

    public ArtifactId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ArtifactId value is required.", nameof(value));
        }

        _value = value.Trim();
    }

    public string Value =>
        _value ?? throw new InvalidOperationException("The default ArtifactId value is invalid.");

    public bool IsDefined => _value is not null;

    public static ArtifactId Require(ArtifactId value, string paramName)
    {
        if (!value.IsDefined)
        {
            throw new ArgumentException("ArtifactId must not be the default value.", paramName);
        }

        return value;
    }

    public override string ToString() => Value;
}
