using System;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ControlTargetDefinition
{
    private static readonly string[] AllowedSetpointProfiles =
    [
        "constant",
        "scheduled"
    ];

    private static readonly string[] AllowedRegulationModes =
    [
        "open_loop",
        "closed_loop"
    ];

    public ControlTargetDefinition(
        ArtifactId id,
        string name,
        string description,
        ArtifactId measuredSourceId,
        ArtifactId commandRoleId,
        string setpointProfile,
        string regulationMode,
        ArtifactId? targetParameterId = null,
        ArtifactId? scheduleParameterId = null)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Description = RequireText(description, nameof(description));
        MeasuredSourceId = ArtifactId.Require(measuredSourceId, nameof(measuredSourceId));
        CommandRoleId = ArtifactId.Require(commandRoleId, nameof(commandRoleId));
        SetpointProfile = RequireAllowed(setpointProfile, AllowedSetpointProfiles, nameof(setpointProfile));
        RegulationMode = RequireAllowed(regulationMode, AllowedRegulationModes, nameof(regulationMode));
        TargetParameterId = NormalizeOptionalArtifactId(targetParameterId, nameof(targetParameterId));
        ScheduleParameterId = NormalizeOptionalArtifactId(scheduleParameterId, nameof(scheduleParameterId));

        if (SetpointProfile == "constant" && TargetParameterId is null)
        {
            throw new ArgumentException("targetParameterId is required for constant control targets.", nameof(targetParameterId));
        }

        if (SetpointProfile == "scheduled" && ScheduleParameterId is null)
        {
            throw new ArgumentException("scheduleParameterId is required for scheduled control targets.", nameof(scheduleParameterId));
        }
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Description { get; }

    public ArtifactId MeasuredSourceId { get; }

    public ArtifactId CommandRoleId { get; }

    public string SetpointProfile { get; }

    public string RegulationMode { get; }

    public ArtifactId? TargetParameterId { get; }

    public ArtifactId? ScheduleParameterId { get; }

    private static ArtifactId? NormalizeOptionalArtifactId(ArtifactId? value, string paramName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return ArtifactId.Require(value.Value, paramName);
    }

    private static string RequireAllowed(string value, string[] allowedValues, string paramName)
    {
        var normalized = RequireText(value, paramName);
        if (!allowedValues.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ArgumentException($"{paramName} must be one of the V1 control-target values.", paramName);
        }

        return normalized;
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
