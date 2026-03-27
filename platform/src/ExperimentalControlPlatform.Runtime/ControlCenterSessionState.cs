using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ControlCenterSessionState
{
    public string DeviceId { get; init; } = string.Empty;

    public string DeviceName { get; init; } = string.Empty;

    public bool Connected { get; init; }

    public bool Busy { get; init; }

    public bool? LastCommandedPuffEnabled { get; init; }

    public bool? LastCommandedLaserEnabled { get; init; }

    public int? LastStepCount { get; init; }

    public int? LastPulseCount { get; init; }

    public double? LastControllerTimestampSeconds { get; init; }

    public DateTimeOffset? LastPulseCapturedAt { get; init; }

    public long CommandSequence { get; init; }

    public long PulseSequence { get; init; }

    public string StatusMessage { get; init; } = "Control center session ready.";
}
