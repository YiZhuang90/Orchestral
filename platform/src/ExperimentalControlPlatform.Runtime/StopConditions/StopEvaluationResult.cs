using System;

namespace ExperimentalControlPlatform.Runtime.StopConditions;

public sealed record class StopEvaluationResult
{
    private StopEvaluationResult(bool shouldStop, StopReason? reason)
    {
        if (shouldStop && reason is null)
        {
            throw new ArgumentException("A stop reason is required when a stop is requested.", nameof(reason));
        }

        ShouldStop = shouldStop;
        Reason = reason;
    }

    public bool ShouldStop { get; }

    public StopReason? Reason { get; }

    public static StopEvaluationResult Continue() => new(false, null);

    public static StopEvaluationResult Stop(StopReason reason) => new(true, reason);
}
