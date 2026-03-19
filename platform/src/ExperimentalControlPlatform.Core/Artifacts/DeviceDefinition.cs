using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class DeviceDefinition
{
    public DeviceDefinition(
        ArtifactId id,
        string name,
        string identity,
        ProtocolDefinition protocol,
        IReadOnlyList<CapabilityDefinition> capabilities,
        IReadOnlyList<ParameterDefinition> parameters,
        string healthStatus)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Identity = RequireText(identity, nameof(identity));
        Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        Capabilities = CopyRequired(capabilities, nameof(capabilities));
        Parameters = CopyOptional(parameters, nameof(parameters));
        HealthStatus = RequireText(healthStatus, nameof(healthStatus));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Identity { get; }

    public ProtocolDefinition Protocol { get; }

    public IReadOnlyList<CapabilityDefinition> Capabilities { get; }

    public IReadOnlyList<ParameterDefinition> Parameters { get; }

    public string HealthStatus { get; }

    private static IReadOnlyList<T> CopyRequired<T>(IReadOnlyList<T> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (values.Count == 0)
        {
            throw new ArgumentException($"{ToDisplayName(paramName)} must contain at least one item.", paramName);
        }

        return values.ToArray();
    }

    private static IReadOnlyList<T> CopyOptional<T>(IReadOnlyList<T> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToArray();
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
