using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ResolvedExperimentDefinition
{
    private readonly Dictionary<ArtifactId, RoleBindingDefinition> _bindingsByRoleId;
    private readonly Dictionary<ArtifactId, ParameterDefinition> _parametersById;
    private readonly Dictionary<ArtifactId, ControlTargetDefinition> _controlTargetsById;

    public ResolvedExperimentDefinition(
        ExperimentDefinition experiment,
        string version,
        IReadOnlyList<DeviceDefinition> devices,
        IReadOnlyList<RoleBindingDefinition> roleBindings,
        IReadOnlyDictionary<ArtifactId, string> parameterValues)
    {
        Experiment = experiment ?? throw new ArgumentNullException(nameof(experiment));
        Version = RequireText(version, nameof(version));
        Devices = CopyRequired(devices, nameof(devices));
        RoleBindings = CopyRequired(roleBindings, nameof(roleBindings));
        ParameterValues = CopyArtifactKeyedStrings(parameterValues, nameof(parameterValues));

        var validation = Validate(Experiment, Devices, RoleBindings, ParameterValues);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Summary, nameof(roleBindings));
        }

        _bindingsByRoleId = RoleBindings.ToDictionary(static binding => binding.RoleId, static binding => binding);
        _parametersById = Experiment.Parameters.ToDictionary(static parameter => parameter.Id, static parameter => parameter);
        _controlTargetsById = Experiment.ControlTargets.ToDictionary(static controlTarget => controlTarget.Id, static controlTarget => controlTarget);
    }

    public ExperimentDefinition Experiment { get; }

    public string Version { get; }

    public IReadOnlyList<DeviceDefinition> Devices { get; }

    public IReadOnlyList<RoleBindingDefinition> RoleBindings { get; }

    public IReadOnlyDictionary<ArtifactId, string> ParameterValues { get; }

    public IReadOnlyList<ControlTargetDefinition> ControlTargets => Experiment.ControlTargets;

    public bool TryGetBinding(ArtifactId roleId, out RoleBindingDefinition? binding) =>
        _bindingsByRoleId.TryGetValue(ArtifactId.Require(roleId, nameof(roleId)), out binding);

    public bool TryGetControlTarget(ArtifactId controlTargetId, out ControlTargetDefinition? controlTarget) =>
        _controlTargetsById.TryGetValue(ArtifactId.Require(controlTargetId, nameof(controlTargetId)), out controlTarget);

    public bool TryGetParameterValue(ArtifactId parameterId, out string? value)
    {
        var requiredId = ArtifactId.Require(parameterId, nameof(parameterId));
        if (ParameterValues.TryGetValue(requiredId, out var explicitValue))
        {
            value = explicitValue;
            return true;
        }

        if (_parametersById.TryGetValue(requiredId, out var definition) && !string.IsNullOrWhiteSpace(definition.DefaultValue))
        {
            value = definition.DefaultValue;
            return true;
        }

        value = null;
        return false;
    }

    public static ExperimentBindingValidationResult Validate(
        ExperimentDefinition experiment,
        IReadOnlyList<DeviceDefinition> devices,
        IReadOnlyList<RoleBindingDefinition> roleBindings,
        IReadOnlyDictionary<ArtifactId, string> parameterValues)
    {
        ArgumentNullException.ThrowIfNull(experiment);
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(roleBindings);
        ArgumentNullException.ThrowIfNull(parameterValues);

        var issues = new List<ExperimentBindingValidationIssue>();

        var experimentRoles = new Dictionary<ArtifactId, DeviceRoleDefinition>();
        foreach (var role in experiment.Roles)
        {
            if (!experimentRoles.TryAdd(role.Id, role))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "duplicate_role_definition",
                    $"Experiment role '{role.Id}' appears more than once.",
                    roleId: role.Id));
            }
        }

        var deviceLookup = new Dictionary<ArtifactId, DeviceDefinition>();
        foreach (var device in devices)
        {
            if (!deviceLookup.TryAdd(device.Id, device))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "duplicate_device_definition",
                    $"Device '{device.Id}' appears more than once.",
                    deviceId: device.Id));
            }
        }

        var bindingsByRole = new Dictionary<ArtifactId, RoleBindingDefinition>();
        foreach (var binding in roleBindings)
        {
            if (!bindingsByRole.TryAdd(binding.RoleId, binding))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "duplicate_role_binding",
                    $"Role '{binding.RoleId}' is bound more than once.",
                    roleId: binding.RoleId,
                    deviceId: binding.DeviceId));
                continue;
            }

            if (!experimentRoles.TryGetValue(binding.RoleId, out var role))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "unknown_role_binding",
                    $"Binding references unknown role '{binding.RoleId}'.",
                    roleId: binding.RoleId,
                    deviceId: binding.DeviceId));
                continue;
            }

            if (!deviceLookup.TryGetValue(binding.DeviceId, out var device))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "unknown_device_binding",
                    $"Binding for role '{binding.RoleId}' references unknown device '{binding.DeviceId}'.",
                    roleId: binding.RoleId,
                    deviceId: binding.DeviceId));
                continue;
            }

            ValidateBindingAgainstRoleAndDevice(role, device, binding, issues);
        }

        foreach (var role in experiment.Roles)
        {
            if (!bindingsByRole.ContainsKey(role.Id))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "missing_role_binding",
                    $"Role '{role.Id}' does not have a concrete device binding.",
                    roleId: role.Id));
            }
        }

        foreach (var parameter in experiment.Parameters)
        {
            if (!parameterValues.ContainsKey(parameter.Id) && string.IsNullOrWhiteSpace(parameter.DefaultValue))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "missing_required_experiment_parameter",
                    $"Experiment parameter '{parameter.Id}' requires a value for the resolved run.",
                    parameterId: parameter.Id));
            }
        }

        var experimentParameterIds = experiment.Parameters
            .Select(static parameter => parameter.Id)
            .ToHashSet();
        foreach (var parameterEntry in parameterValues)
        {
            if (!experimentParameterIds.Contains(parameterEntry.Key))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "unknown_parameter_value",
                    $"Parameter '{parameterEntry.Key}' is not declared by experiment '{experiment.Id}'.",
                    parameterId: parameterEntry.Key));
            }
        }

        ValidateControlTargets(
            experiment,
            bindingsByRole,
            experimentParameterIds,
            parameterValues,
            experimentRoles,
            issues);

        return new ExperimentBindingValidationResult(issues);
    }

    private static void ValidateControlTargets(
        ExperimentDefinition experiment,
        IReadOnlyDictionary<ArtifactId, RoleBindingDefinition> bindingsByRole,
        IReadOnlySet<ArtifactId> experimentParameterIds,
        IReadOnlyDictionary<ArtifactId, string> parameterValues,
        IReadOnlyDictionary<ArtifactId, DeviceRoleDefinition> experimentRoles,
        ICollection<ExperimentBindingValidationIssue> issues)
    {
        var measuredSourceIds = experiment.Streams
            .Select(static stream => stream.Id)
            .ToHashSet();

        var parameterDefinitionsById = experiment.Parameters.ToDictionary(static parameter => parameter.Id, static parameter => parameter);

        foreach (var controlTarget in experiment.ControlTargets)
        {
            if (!experimentRoles.ContainsKey(controlTarget.CommandRoleId))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "unknown_control_target_command_role",
                    $"Control target '{controlTarget.Id}' references unknown command role '{controlTarget.CommandRoleId}'.",
                    roleId: controlTarget.CommandRoleId));
            }
            else if (!bindingsByRole.ContainsKey(controlTarget.CommandRoleId))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "unbound_control_target_command_role",
                    $"Control target '{controlTarget.Id}' references role '{controlTarget.CommandRoleId}', but that role has no concrete binding.",
                    roleId: controlTarget.CommandRoleId));
            }

            if (!measuredSourceIds.Contains(controlTarget.MeasuredSourceId))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "unknown_control_target_measured_source",
                    $"Control target '{controlTarget.Id}' references unknown measured source '{controlTarget.MeasuredSourceId}'."));
            }

            ValidateControlTargetParameter(
                controlTarget.TargetParameterId,
                "missing_control_target_parameter_value",
                "unknown_control_target_parameter",
                controlTarget,
                experimentParameterIds,
                parameterDefinitionsById,
                parameterValues,
                issues);

            ValidateControlTargetParameter(
                controlTarget.ScheduleParameterId,
                "missing_control_target_schedule_value",
                "unknown_control_target_schedule_parameter",
                controlTarget,
                experimentParameterIds,
                parameterDefinitionsById,
                parameterValues,
                issues);
        }
    }

    private static void ValidateControlTargetParameter(
        ArtifactId? parameterId,
        string missingValueIssueCode,
        string unknownParameterIssueCode,
        ControlTargetDefinition controlTarget,
        IReadOnlySet<ArtifactId> experimentParameterIds,
        IReadOnlyDictionary<ArtifactId, ParameterDefinition> parameterDefinitionsById,
        IReadOnlyDictionary<ArtifactId, string> parameterValues,
        ICollection<ExperimentBindingValidationIssue> issues)
    {
        if (!parameterId.HasValue)
        {
            return;
        }

        var requiredId = parameterId.Value;
        if (!experimentParameterIds.Contains(requiredId))
        {
            issues.Add(new ExperimentBindingValidationIssue(
                unknownParameterIssueCode,
                $"Control target '{controlTarget.Id}' references unknown parameter '{requiredId}'.",
                parameterId: requiredId));
            return;
        }

        if (parameterValues.ContainsKey(requiredId))
        {
            ValidateControlTargetParameterFormat(controlTarget, requiredId, parameterValues[requiredId], issues);
            return;
        }

        if (parameterDefinitionsById.TryGetValue(requiredId, out var definition) && !string.IsNullOrWhiteSpace(definition.DefaultValue))
        {
            ValidateControlTargetParameterFormat(controlTarget, requiredId, definition.DefaultValue!, issues);
            return;
        }

        issues.Add(new ExperimentBindingValidationIssue(
            missingValueIssueCode,
            $"Control target '{controlTarget.Id}' requires a value for parameter '{requiredId}'.",
            parameterId: requiredId));
    }

    private static void ValidateControlTargetParameterFormat(
        ControlTargetDefinition controlTarget,
        ArtifactId parameterId,
        string parameterValue,
        ICollection<ExperimentBindingValidationIssue> issues)
    {
        if (controlTarget.SetpointProfile != "scheduled" || controlTarget.ScheduleParameterId != parameterId)
        {
            return;
        }

        try
        {
            ControlTargetScheduleParser.Parse(parameterValue, parameterId);
        }
        catch (FormatException ex)
        {
            issues.Add(new ExperimentBindingValidationIssue(
                "invalid_control_target_schedule_format",
                $"Control target '{controlTarget.Id}' has an invalid schedule format: {ex.Message}",
                parameterId: parameterId));
        }
    }

    private static void ValidateBindingAgainstRoleAndDevice(
        DeviceRoleDefinition role,
        DeviceDefinition device,
        RoleBindingDefinition binding,
        ICollection<ExperimentBindingValidationIssue> issues)
    {
        var deviceCapabilityIds = device.Capabilities
            .Select(static capability => capability.Id)
            .ToHashSet();
        var bindingCapabilityIds = binding.SatisfiedCapabilityIds.ToHashSet();

        foreach (var requiredCapabilityId in role.RequiredCapabilityIds)
        {
            if (!bindingCapabilityIds.Contains(requiredCapabilityId))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "missing_required_capability_binding",
                    $"Binding for role '{role.Id}' does not satisfy required capability '{requiredCapabilityId}'.",
                    roleId: role.Id,
                    deviceId: device.Id));
            }

            if (!deviceCapabilityIds.Contains(requiredCapabilityId))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "device_missing_required_capability",
                    $"Device '{device.Id}' does not advertise required capability '{requiredCapabilityId}' for role '{role.Id}'.",
                    roleId: role.Id,
                    deviceId: device.Id));
            }
        }

        foreach (var capabilityId in binding.SatisfiedCapabilityIds)
        {
            if (!deviceCapabilityIds.Contains(capabilityId))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "binding_capability_not_advertised",
                    $"Binding for role '{role.Id}' claims capability '{capabilityId}', but device '{device.Id}' does not advertise it.",
                    roleId: role.Id,
                    deviceId: device.Id));
            }
        }

        if (role.ExpectedProtocolId.HasValue && role.ExpectedProtocolId.Value != binding.ProtocolId)
        {
            issues.Add(new ExperimentBindingValidationIssue(
                "protocol_mismatch",
                $"Binding for role '{role.Id}' uses protocol '{binding.ProtocolId}', but role expects '{role.ExpectedProtocolId.Value}'.",
                roleId: role.Id,
                deviceId: device.Id));
        }

        if (device.Protocol.Id != binding.ProtocolId)
        {
            issues.Add(new ExperimentBindingValidationIssue(
                "device_protocol_mismatch",
                $"Binding for role '{role.Id}' uses protocol '{binding.ProtocolId}', but device '{device.Id}' is defined with protocol '{device.Protocol.Id}'.",
                roleId: role.Id,
                deviceId: device.Id));
        }

        var declaredDeviceParameterIds = device.Parameters
            .Select(static parameter => parameter.Id)
            .ToHashSet();
        foreach (var parameter in device.Parameters)
        {
            if (!binding.ParameterValues.ContainsKey(parameter.Id) && string.IsNullOrWhiteSpace(parameter.DefaultValue))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "missing_required_binding_parameter",
                    $"Device binding parameter '{parameter.Id}' requires a value for role '{role.Id}'.",
                    roleId: role.Id,
                    deviceId: device.Id,
                    parameterId: parameter.Id));
            }
        }

        foreach (var parameterEntry in binding.ParameterValues)
        {
            if (!declaredDeviceParameterIds.Contains(parameterEntry.Key))
            {
                issues.Add(new ExperimentBindingValidationIssue(
                    "unknown_binding_parameter",
                    $"Binding for role '{role.Id}' specifies unknown device parameter '{parameterEntry.Key}'.",
                    roleId: role.Id,
                    deviceId: device.Id,
                    parameterId: parameterEntry.Key));
            }
        }
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

    private static IReadOnlyDictionary<ArtifactId, string> CopyArtifactKeyedStrings(
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
