using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class CrossSessionValidationIssue
{
    public CrossSessionValidationIssue(
        string code,
        string message,
        ArtifactId? deviceId = null,
        ArtifactId? capabilityId = null,
        ArtifactId? commandRoleId = null,
        IReadOnlyList<ArtifactId>? roleIds = null,
        IReadOnlyList<ArtifactId>? controlTargetIds = null)
    {
        Code = RequireText(code, nameof(code));
        Message = RequireText(message, nameof(message));
        DeviceId = NormalizeOptional(deviceId, nameof(deviceId));
        CapabilityId = NormalizeOptional(capabilityId, nameof(capabilityId));
        CommandRoleId = NormalizeOptional(commandRoleId, nameof(commandRoleId));
        RoleIds = CopyArtifactList(roleIds);
        ControlTargetIds = CopyArtifactList(controlTargetIds);
    }

    public string Code { get; }

    public string Message { get; }

    public ArtifactId? DeviceId { get; }

    public ArtifactId? CapabilityId { get; }

    public ArtifactId? CommandRoleId { get; }

    public IReadOnlyList<ArtifactId> RoleIds { get; }

    public IReadOnlyList<ArtifactId> ControlTargetIds { get; }

    private static IReadOnlyList<ArtifactId> CopyArtifactList(IReadOnlyList<ArtifactId>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<ArtifactId>();
        }

        return values
            .Select(value => ArtifactId.Require(value, nameof(values)))
            .Distinct()
            .ToArray();
    }

    private static ArtifactId? NormalizeOptional(ArtifactId? value, string paramName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return ArtifactId.Require(value.Value, paramName);
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
