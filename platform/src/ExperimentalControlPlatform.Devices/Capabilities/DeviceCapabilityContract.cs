using System;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.Devices.Capabilities;

public sealed record class DeviceCapabilityContract
{
    public DeviceCapabilityContract(
        ArtifactId id,
        CapabilityKind kind,
        string name,
        string description)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Kind = RequireDeclared(kind, nameof(kind));
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
    }

    public ArtifactId Id { get; }

    public CapabilityKind Kind { get; }

    public string Name { get; }

    public string Description { get; }

    public CapabilityDefinition ToDefinition() => new(Id, Name, Description);

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{ToDisplayName(paramName)} is required.", paramName);
        }

        return value.Trim();
    }

    private static CapabilityKind RequireDeclared(CapabilityKind value, string paramName)
    {
        if (value == CapabilityKind.Unknown)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{ToDisplayName(paramName)} must be a declared value.");
        }

        return value;
    }

    private static string ToDisplayName(string paramName) =>
        char.ToUpperInvariant(paramName[0]) + paramName[1..];
}
