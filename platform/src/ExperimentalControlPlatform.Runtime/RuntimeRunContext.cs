using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record class RuntimeRunContext
{
    public RuntimeRunContext(
        Guid runId,
        RunState state,
        DateTimeOffset? startedAtUtc = null,
        DateTimeOffset? stoppedAtUtc = null,
        StopReason? stopReason = null)
    {
        RunId = runId;
        State = state;
        StartedAtUtc = startedAtUtc;
        StoppedAtUtc = stoppedAtUtc;
        StopReason = stopReason;
    }

    public Guid RunId { get; }

    public RunState State { get; }

    public DateTimeOffset? StartedAtUtc { get; }

    public DateTimeOffset? StoppedAtUtc { get; }

    public StopReason? StopReason { get; }
}
