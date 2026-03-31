using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ControlCenterLaserCapabilityState
{
    public bool? LastCommandedEnabled { get; init; }

    public DateTimeOffset? LastCommandedAtUtc { get; init; }

    public long CommandSequence { get; init; }

    public string StatusMessage { get; init; } = "Laser control ready.";
}
