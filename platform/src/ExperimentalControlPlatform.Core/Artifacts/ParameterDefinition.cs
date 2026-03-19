using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ParameterDefinition
{
    public ParameterDefinition(
        ArtifactId id,
        string name,
        string valueType,
        string scope,
        string? unit = null,
        string? defaultValue = null,
        string? allowedValues = null,
        string? validationRule = null)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        ValueType = RequireText(valueType, nameof(valueType));
        Scope = RequireText(scope, nameof(scope));
        Unit = NormalizeOptional(unit);
        DefaultValue = NormalizeOptional(defaultValue);
        AllowedValues = NormalizeOptional(allowedValues);
        ValidationRule = NormalizeOptional(validationRule);
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string ValueType { get; }

    public string Scope { get; }

    public string? Unit { get; }

    public string? DefaultValue { get; }

    public string? AllowedValues { get; }

    public string? ValidationRule { get; }

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
