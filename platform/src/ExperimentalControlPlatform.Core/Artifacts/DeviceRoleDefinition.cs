using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class DeviceRoleDefinition
{
    public DeviceRoleDefinition(
        ArtifactId id,
        string name,
        string description,
        IReadOnlyList<ArtifactId> requiredCapabilityIds,
        ArtifactId? expectedProtocolId)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
        RequiredCapabilityIds = CopyRequired(requiredCapabilityIds, nameof(requiredCapabilityIds));
        ExpectedProtocolId = NormalizeOptional(expectedProtocolId, nameof(expectedProtocolId));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Description { get; }

    public IReadOnlyList<ArtifactId> RequiredCapabilityIds { get; }

    public ArtifactId? ExpectedProtocolId { get; }

    private static IReadOnlyList<ArtifactId> CopyRequired(IReadOnlyList<ArtifactId> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("RequiredCapabilityIds must contain at least one item.", paramName);
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

    private static ArtifactId? NormalizeOptional(ArtifactId? value, string paramName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return ArtifactId.Require(value.Value, paramName);
    }

    private static string ToDisplayName(string paramName) =>
        char.ToUpperInvariant(paramName[0]) + paramName[1..];
}
