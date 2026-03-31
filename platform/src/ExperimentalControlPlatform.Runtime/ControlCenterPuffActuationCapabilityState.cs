using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ControlCenterPuffActuationCapabilityState
{
    public bool? LastCommandedEnabled { get; init; }

    public int? LastStepCount { get; init; }

    public DateTimeOffset? LastCommandedAtUtc { get; init; }

    public long CommandSequence { get; init; }

    public string StatusMessage { get; init; } = "Puff actuation ready.";
}
