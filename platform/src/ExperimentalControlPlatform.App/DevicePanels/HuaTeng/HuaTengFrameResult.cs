using System.Collections.Generic;
using System.Windows;

namespace ExperimentalControlPlatform.App.DevicePanels.HuaTeng;

public sealed class HuaTengFrameResult
{
    public bool Ok { get; init; }

    public string Summary { get; init; } = string.Empty;

    public HuaTengCameraInfo? Camera { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public byte[] PixelData { get; init; } = [];

    public string PixelFormat { get; init; } = string.Empty;

    public string TriggerMode { get; init; } = string.Empty;

    public double ExposureUs { get; init; }

    public bool IsMono { get; init; }

    public int TimestampTenthsOfMilliseconds { get; init; }

    public Rect? Roi { get; init; }

    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    public string? Exception { get; init; }
}
