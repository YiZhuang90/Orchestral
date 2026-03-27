using System;
namespace ExperimentalControlPlatform.Runtime;

public sealed record HuaTengSessionState
{
    public required string DeviceId { get; init; }

    public required string DeviceName { get; init; }

    public bool Connected { get; init; }

    public bool Busy { get; init; }

    public bool LivePreviewing { get; init; }

    public string StatusMessage { get; init; } = "HuaTeng camera session ready.";

    public string PixelFormat { get; init; } = "Auto";

    public string TriggerMode { get; init; } = "Triggered";

    public string ColorTone { get; init; } = "Neutral";

    public double? ExposureUs { get; init; }

    public double TargetFrameRate { get; init; } = 3.0;

    public CaptureRegion? AppliedRoi { get; init; }

    public long FrameSequence { get; init; }

    public DateTimeOffset? LastFrameCapturedAt { get; init; }

    public string? LastSourceMode { get; init; }
}
