using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record IntegratedCameraSessionState
{
    public required string DeviceId { get; init; }

    public required string DeviceName { get; init; }

    public bool Connected { get; init; }

    public bool Busy { get; init; }

    public bool LivePreviewing { get; init; }

    public string StatusMessage { get; init; } = "Integrated camera session ready.";

    public double TargetFrameRate { get; init; } = 20.0;

    public bool ColorEnabled { get; init; } = true;

    public long FrameSequence { get; init; }

    public DateTimeOffset? LastFrameCapturedAt { get; init; }

    public string? LastSourceMode { get; init; }
}
