using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ExperimentMonitorDeviceSnapshot
{
    public string SourceId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string SessionFamily { get; init; } = string.Empty;

    public bool Connected { get; init; }

    public bool Busy { get; init; }

    public bool LiveActive { get; init; }

    public bool IsCriticalControl { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    public string? LastError { get; init; }

    public DateTimeOffset? LastObservedAtUtc { get; init; }
}
