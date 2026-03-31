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

    public ExperimentDefinitionLintResult Lint()
    {
        var issues = new List<ExperimentDefinitionLintIssue>();

        ValidateDuplicateDefinitions(Roles, static role => role.Id, "duplicate_role_definition", "role", issues);
        ValidateDuplicateDefinitions(Parameters, static parameter => parameter.Id, "duplicate_parameter_definition", "parameter", issues);
        ValidateDuplicateDefinitions(Streams, static stream => stream.Id, "duplicate_stream_definition", "stream", issues);
        ValidateDuplicateDefinitions(Transforms, static transform => transform.Id, "duplicate_transform_definition", "transform", issues);
        ValidateDuplicateDefinitions(Monitors, static monitor => monitor.Id, "duplicate_monitor_definition", "monitor", issues);
        ValidateDuplicateDefinitions(StopConditions, static stopCondition => stopCondition.Id, "duplicate_stop_condition_definition", "stop condition", issues);
        ValidateDuplicateDefinitions(Outputs, static output => output.Id, "duplicate_output_definition", "output", issues);
        ValidateDuplicateDefinitions(ControlTargets, static controlTarget => controlTarget.Id, "duplicate_control_target_definition", "control target", issues);

        var streamIds = Streams.Select(static stream => stream.Id).ToHashSet();
        var roleIds = Roles.Select(static role => role.Id).ToHashSet();
        var parameterIds = Parameters.Select(static parameter => parameter.Id).ToHashSet();

        foreach (var transform in Transforms)
        {
            foreach (var inputStreamId in transform.InputStreamIds)
            {
                if (!streamIds.Contains(inputStreamId))
                {
                    issues.Add(new ExperimentDefinitionLintIssue(
                        "unknown_transform_input_stream",
                        ExperimentDefinitionLintSeverity.Error,
                        $"Transform '{transform.Id}' references unknown input stream '{inputStreamId}'.",
                        transform.Id,
                        inputStreamId));
                }
            }

            foreach (var outputStreamId in transform.OutputStreamIds)
            {
                if (!streamIds.Contains(outputStreamId))
                {
                    issues.Add(new ExperimentDefinitionLintIssue(
                        "unknown_transform_output_stream",
                        ExperimentDefinitionLintSeverity.Error,
                        $"Transform '{transform.Id}' references unknown output stream '{outputStreamId}'.",
                        transform.Id,
                        outputStreamId));
                }
            }
        }

        foreach (var monitor in Monitors)
        {
            foreach (var observedStreamId in monitor.ObservedStreamIds)
            {
                if (!streamIds.Contains(observedStreamId))
                {
                    issues.Add(new ExperimentDefinitionLintIssue(
                        "unknown_monitor_stream",
                        ExperimentDefinitionLintSeverity.Error,
                        $"Monitor '{monitor.Id}' references unknown observed stream '{observedStreamId}'.",
                        monitor.Id,
                        observedStreamId));
                }
            }
        }

        foreach (var controlTarget in ControlTargets)
        {
            if (!streamIds.Contains(controlTarget.MeasuredSourceId))
            {
                issues.Add(new ExperimentDefinitionLintIssue(
                    "unknown_control_target_measured_source",
                    ExperimentDefinitionLintSeverity.Error,
                    $"Control target '{controlTarget.Id}' references unknown measured source '{controlTarget.MeasuredSourceId}'.",
                    controlTarget.Id,
                    controlTarget.MeasuredSourceId));
            }

            if (!roleIds.Contains(controlTarget.CommandRoleId))
            {
                issues.Add(new ExperimentDefinitionLintIssue(
                    "unknown_control_target_command_role",
                    ExperimentDefinitionLintSeverity.Error,
                    $"Control target '{controlTarget.Id}' references unknown command role '{controlTarget.CommandRoleId}'.",
                    controlTarget.Id,
                    controlTarget.CommandRoleId));
            }

            ValidateControlTargetParameterReference(
                controlTarget.Id,
                controlTarget.TargetParameterId,
                parameterIds,
                "unknown_control_target_parameter",
                "parameter",
                issues);

            ValidateControlTargetParameterReference(
                controlTarget.Id,
                controlTarget.ScheduleParameterId,
                parameterIds,
                "unknown_control_target_schedule_parameter",
                "schedule parameter",
                issues);
        }

        return new ExperimentDefinitionLintResult(issues);
    }

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

    private static void ValidateDuplicateDefinitions<T>(
        IReadOnlyList<T> values,
        Func<T, ArtifactId> idSelector,
        string issueCode,
        string collectionDisplayName,
        ICollection<ExperimentDefinitionLintIssue> issues)
    {
        foreach (var group in values.GroupBy(idSelector))
        {
            if (group.Count() < 2)
            {
                continue;
            }

            issues.Add(new ExperimentDefinitionLintIssue(
                issueCode,
                ExperimentDefinitionLintSeverity.Error,
                $"Experiment declares {collectionDisplayName} '{group.Key}' more than once.",
                group.Key));
        }
    }

    private static void ValidateControlTargetParameterReference(
        ArtifactId controlTargetId,
        ArtifactId? parameterId,
        IReadOnlySet<ArtifactId> parameterIds,
        string issueCode,
        string parameterDisplayName,
        ICollection<ExperimentDefinitionLintIssue> issues)
    {
        if (!parameterId.HasValue)
        {
            return;
        }

        if (parameterIds.Contains(parameterId.Value))
        {
            return;
        }

        issues.Add(new ExperimentDefinitionLintIssue(
            issueCode,
            ExperimentDefinitionLintSeverity.Error,
            $"Control target '{controlTargetId}' references unknown {parameterDisplayName} '{parameterId.Value}'.",
            controlTargetId,
            parameterId.Value));
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
