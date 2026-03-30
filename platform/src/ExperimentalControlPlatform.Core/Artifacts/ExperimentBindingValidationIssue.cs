using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ExperimentBindingValidationIssue
{
    public ExperimentBindingValidationIssue(
        string code,
        string message,
        ArtifactId? roleId = null,
        ArtifactId? deviceId = null,
        ArtifactId? parameterId = null)
    {
        Code = RequireText(code, nameof(code));
        Message = RequireText(message, nameof(message));
        RoleId = NormalizeOptional(roleId, nameof(roleId));
        DeviceId = NormalizeOptional(deviceId, nameof(deviceId));
        ParameterId = NormalizeOptional(parameterId, nameof(parameterId));
    }

    public string Code { get; }

    public string Message { get; }

    public ArtifactId? RoleId { get; }

    public ArtifactId? DeviceId { get; }

    public ArtifactId? ParameterId { get; }

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
