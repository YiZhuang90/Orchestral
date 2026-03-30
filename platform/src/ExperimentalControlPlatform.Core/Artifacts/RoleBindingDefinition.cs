using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class RoleBindingDefinition
{
    public RoleBindingDefinition(
        ArtifactId roleId,
        ArtifactId deviceId,
        ArtifactId protocolId,
        IReadOnlyList<ArtifactId> satisfiedCapabilityIds)
        : this(
            roleId,
            deviceId,
            protocolId,
            satisfiedCapabilityIds,
            new Dictionary<ArtifactId, string>())
    {
    }

    public RoleBindingDefinition(
        ArtifactId roleId,
        ArtifactId deviceId,
        ArtifactId protocolId,
        IReadOnlyList<ArtifactId> satisfiedCapabilityIds,
        IReadOnlyDictionary<ArtifactId, string> parameterValues)
    {
        RoleId = ArtifactId.Require(roleId, nameof(roleId));
        DeviceId = ArtifactId.Require(deviceId, nameof(deviceId));
        ProtocolId = ArtifactId.Require(protocolId, nameof(protocolId));
        SatisfiedCapabilityIds = CopyRequiredDistinctCapabilities(satisfiedCapabilityIds, nameof(satisfiedCapabilityIds));
        ParameterValues = CopyParameterValues(parameterValues, nameof(parameterValues));
    }

    public ArtifactId RoleId { get; }

    public ArtifactId DeviceId { get; }

    public ArtifactId ProtocolId { get; }

    public IReadOnlyList<ArtifactId> SatisfiedCapabilityIds { get; }

    public IReadOnlyDictionary<ArtifactId, string> ParameterValues { get; }

    private static IReadOnlyList<ArtifactId> CopyRequiredDistinctCapabilities(
        IReadOnlyList<ArtifactId> values,
        string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("SatisfiedCapabilityIds must contain at least one item.", paramName);
        }

        var copied = values.Select(value => ArtifactId.Require(value, paramName)).ToArray();
        var duplicate = copied
            .GroupBy(static value => value)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"SatisfiedCapabilityIds contains duplicate capability '{duplicate.Key}'.",
                paramName);
        }

        return copied;
    }

    private static IReadOnlyDictionary<ArtifactId, string> CopyParameterValues(
        IReadOnlyDictionary<ArtifactId, string> values,
        string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToDictionary(
            entry => ArtifactId.Require(entry.Key, paramName),
            entry => RequireText(entry.Value, paramName));
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
