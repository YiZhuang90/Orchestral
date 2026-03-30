using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ExperimentDefinition
{
    public ExperimentDefinition(
        ArtifactId id,
        string name,
        string purpose,
        IReadOnlyList<DeviceRoleDefinition> roles,
        IReadOnlyList<ParameterDefinition> parameters,
        IReadOnlyList<StreamDefinition> streams,
        IReadOnlyList<TransformDefinition> transforms,
        IReadOnlyList<MonitorDefinition> monitors,
        IReadOnlyList<StopConditionDefinition> stopConditions,
        IReadOnlyList<OutputDefinition> outputs)
        : this(
            id,
            name,
            purpose,
            roles,
            parameters,
            streams,
            transforms,
            monitors,
            stopConditions,
            outputs,
            [])
    {
    }

    public ExperimentDefinition(
        ArtifactId id,
        string name,
        string purpose,
        IReadOnlyList<DeviceRoleDefinition> roles,
        IReadOnlyList<ParameterDefinition> parameters,
        IReadOnlyList<StreamDefinition> streams,
        IReadOnlyList<TransformDefinition> transforms,
        IReadOnlyList<MonitorDefinition> monitors,
        IReadOnlyList<StopConditionDefinition> stopConditions,
        IReadOnlyList<OutputDefinition> outputs,
        IReadOnlyList<ControlTargetDefinition> controlTargets)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Purpose = RequireText(purpose, nameof(purpose));
        Roles = CopyRequired(roles, nameof(roles));
        Parameters = CopyOptional(parameters, nameof(parameters));
        Streams = CopyOptional(streams, nameof(streams));
        Transforms = CopyOptional(transforms, nameof(transforms));
        Monitors = CopyOptional(monitors, nameof(monitors));
        StopConditions = CopyOptional(stopConditions, nameof(stopConditions));
        Outputs = CopyOptional(outputs, nameof(outputs));
        ControlTargets = CopyOptional(controlTargets, nameof(controlTargets));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string Purpose { get; }

    public IReadOnlyList<DeviceRoleDefinition> Roles { get; }

    public IReadOnlyList<ParameterDefinition> Parameters { get; }

    public IReadOnlyList<StreamDefinition> Streams { get; }

    public IReadOnlyList<TransformDefinition> Transforms { get; }

    public IReadOnlyList<MonitorDefinition> Monitors { get; }

    public IReadOnlyList<StopConditionDefinition> StopConditions { get; }

    public IReadOnlyList<OutputDefinition> Outputs { get; }

    public IReadOnlyList<ControlTargetDefinition> ControlTargets { get; }

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
