using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record DeviceSessionEndSnapshot
{
    public DateTimeOffset EndedAt { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string ReasonMessage { get; init; } = string.Empty;

    public bool ConnectionClosed { get; init; }

    public bool LiveStopped { get; init; }
}
