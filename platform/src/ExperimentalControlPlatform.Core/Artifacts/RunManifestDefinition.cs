using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class RunManifestDefinition
{
    public RunManifestDefinition(
        ArtifactId id,
        ArtifactId experimentId,
        string experimentVersion,
        IReadOnlyDictionary<ArtifactId, ArtifactId> roleBindings,
        IReadOnlyDictionary<ArtifactId, ArtifactId> protocolBindings,
        IReadOnlyDictionary<ArtifactId, string> parameterValues,
        DateTimeOffset startedAt,
        DateTimeOffset? stoppedAt,
        ArtifactId? activatedStopConditionId,
        string? stopReason,
        IReadOnlyList<ArtifactId> outputIds,
        IReadOnlyList<string> runtimeEvents,
        IReadOnlyList<string> warningsOrFaults)
        : this(
            id,
            experimentId,
            experimentVersion,
            roleBindings,
            protocolBindings,
            new Dictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>>(),
            parameterValues,
            startedAt,
            stoppedAt,
            activatedStopConditionId,
            stopReason,
            outputIds,
            runtimeEvents,
            warningsOrFaults,
            new Dictionary<ArtifactId, string>())
    {
    }

    public RunManifestDefinition(
        ArtifactId id,
        ArtifactId experimentId,
        string experimentVersion,
        IReadOnlyDictionary<ArtifactId, ArtifactId> roleBindings,
        IReadOnlyDictionary<ArtifactId, ArtifactId> protocolBindings,
        IReadOnlyDictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>> roleBindingParameterValues,
        IReadOnlyDictionary<ArtifactId, string> parameterValues,
        DateTimeOffset startedAt,
        DateTimeOffset? stoppedAt,
        ArtifactId? activatedStopConditionId,
        string? stopReason,
        IReadOnlyList<ArtifactId> outputIds,
        IReadOnlyList<string> runtimeEvents,
        IReadOnlyList<string> warningsOrFaults)
        : this(
            id,
            experimentId,
            experimentVersion,
            roleBindings,
            protocolBindings,
            roleBindingParameterValues,
            parameterValues,
            startedAt,
            stoppedAt,
            activatedStopConditionId,
            stopReason,
            outputIds,
            runtimeEvents,
            warningsOrFaults,
            new Dictionary<ArtifactId, string>())
    {
    }

    public RunManifestDefinition(
        ArtifactId id,
        ArtifactId experimentId,
        string experimentVersion,
        IReadOnlyDictionary<ArtifactId, ArtifactId> roleBindings,
        IReadOnlyDictionary<ArtifactId, ArtifactId> protocolBindings,
        IReadOnlyDictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>> roleBindingParameterValues,
        IReadOnlyDictionary<ArtifactId, string> parameterValues,
        DateTimeOffset startedAt,
        DateTimeOffset? stoppedAt,
        ArtifactId? activatedStopConditionId,
        string? stopReason,
        IReadOnlyList<ArtifactId> outputIds,
        IReadOnlyList<string> runtimeEvents,
        IReadOnlyList<string> warningsOrFaults,
        IReadOnlyDictionary<ArtifactId, string> artifacts)
    {
        Id = ArtifactId.Require(id, nameof(id));
        ExperimentId = ArtifactId.Require(experimentId, nameof(experimentId));
        ExperimentVersion = RequireText(experimentVersion, nameof(experimentVersion));
        RoleBindings = CopyArtifactDictionary(roleBindings, nameof(roleBindings));
        ProtocolBindings = CopyArtifactDictionary(protocolBindings, nameof(protocolBindings));
        RoleBindingParameterValues = CopyNestedArtifactDictionary(roleBindingParameterValues, nameof(roleBindingParameterValues));
        ParameterValues = CopyArtifactKeyedStrings(parameterValues, nameof(parameterValues));
        StartedAt = startedAt;
        StoppedAt = stoppedAt;
        ActivatedStopConditionId = NormalizeOptionalArtifactId(activatedStopConditionId, nameof(activatedStopConditionId));
        StopReason = NormalizeOptional(stopReason);
        OutputIds = CopyArtifactList(outputIds, nameof(outputIds));
        RuntimeEvents = CopyStrings(runtimeEvents, nameof(runtimeEvents));
        WarningsOrFaults = CopyStrings(warningsOrFaults, nameof(warningsOrFaults));
        Artifacts = CopyArtifactKeyedStrings(artifacts, nameof(artifacts));
    }

    public ArtifactId Id { get; }

    public ArtifactId ExperimentId { get; }

    public string ExperimentVersion { get; }

    public IReadOnlyDictionary<ArtifactId, ArtifactId> RoleBindings { get; }

    public IReadOnlyDictionary<ArtifactId, ArtifactId> ProtocolBindings { get; }

    public IReadOnlyDictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>> RoleBindingParameterValues { get; }

    public IReadOnlyDictionary<ArtifactId, string> ParameterValues { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? StoppedAt { get; }

    public ArtifactId? ActivatedStopConditionId { get; }

    public string? StopReason { get; }

    public IReadOnlyList<ArtifactId> OutputIds { get; }

    public IReadOnlyList<string> RuntimeEvents { get; }

    public IReadOnlyList<string> WarningsOrFaults { get; }

    public IReadOnlyDictionary<ArtifactId, string> Artifacts { get; }

    private static IReadOnlyDictionary<TKey, TValue> CopyDictionary<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> values,
        string paramName)
        where TKey : notnull
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    private static IReadOnlyDictionary<ArtifactId, ArtifactId> CopyArtifactDictionary(
        IReadOnlyDictionary<ArtifactId, ArtifactId> values,
        string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToDictionary(
            entry => ArtifactId.Require(entry.Key, paramName),
            entry => ArtifactId.Require(entry.Value, paramName));
    }

    private static IReadOnlyDictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>> CopyNestedArtifactDictionary(
        IReadOnlyDictionary<ArtifactId, IReadOnlyDictionary<ArtifactId, string>> values,
        string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToDictionary(
            entry => ArtifactId.Require(entry.Key, paramName),
            entry => (IReadOnlyDictionary<ArtifactId, string>)entry.Value.ToDictionary(
                nested => ArtifactId.Require(nested.Key, paramName),
                nested => RequireText(nested.Value, paramName)));
    }

    private static IReadOnlyList<T> CopyList<T>(IReadOnlyList<T> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.ToArray();
    }

    private static IReadOnlyList<ArtifactId> CopyArtifactList(IReadOnlyList<ArtifactId> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.Select(value => ArtifactId.Require(value, paramName)).ToArray();
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

    private static IReadOnlyList<string> CopyStrings(IReadOnlyList<string> values, string paramName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return values.Select(value => RequireText(value, paramName)).ToArray();
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{ToDisplayName(paramName)} is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ArtifactId? NormalizeOptionalArtifactId(ArtifactId? value, string paramName)
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
