using System;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.Runtime;

public sealed record class RuntimeRunContext
{
    public RuntimeRunContext(
        Guid runId,
        RunState state,
        DateTimeOffset? startedAtUtc = null,
        DateTimeOffset? stoppedAtUtc = null,
        StopReason? stopReason = null,
        ResolvedExperimentDefinition? experiment = null,
        RunContextDefinition? runContext = null)
    {
        if (runContext is not null)
        {
            if (experiment is not null && !Equals(runContext.Experiment, experiment))
            {
                throw new ArgumentException("Run context experiment must match the explicit experiment argument.", nameof(runContext));
            }

            experiment = runContext.Experiment;
        }

        RunId = runId;
        State = state;
        StartedAtUtc = startedAtUtc;
        StoppedAtUtc = stoppedAtUtc;
        StopReason = stopReason;
        Experiment = experiment;
        RunContext = runContext;
    }

    /// <summary>
    /// Unique identifier for this specific runtime execution instance.
    /// This is ephemeral runtime identity, not the stable artifact identity for run intent.
    /// </summary>
    public Guid RunId { get; }

    public RunState State { get; }

    public DateTimeOffset? StartedAtUtc { get; }

    public DateTimeOffset? StoppedAtUtc { get; }

    public StopReason? StopReason { get; }

    public ResolvedExperimentDefinition? Experiment { get; }

    /// <summary>
    /// Stable run-intent artifact for this execution, including metadata and reference ids.
    /// Its <see cref="RunContextDefinition.Id"/> is distinct from <see cref="RunId"/>.
    /// </summary>
    public RunContextDefinition? RunContext { get; }
}
