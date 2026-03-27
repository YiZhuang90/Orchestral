using System;
using ExperimentalControlPlatform.Devices.Audio;

namespace ExperimentalControlPlatform.Runtime;

public sealed record IntegratedMicrophoneSessionState
{
    public string DeviceId { get; init; } = string.Empty;

    public string DeviceName { get; init; } = string.Empty;

    public bool Connected { get; init; }

    public bool LiveReading { get; init; }

    public bool Busy { get; init; }

    public string StatusMessage { get; init; } = "Ready to connect.";

    public double? TargetUpdateRateHz { get; init; }

    public int? WindowMilliseconds { get; init; }

    public MicrophoneChannelMode? ChannelMode { get; init; }

    public DateTimeOffset? LastFrameCapturedAt { get; init; }

    public long FrameSequence { get; init; }

    public string? LastSourceMode { get; init; }
}
