using System.Collections.Generic;

namespace ExperimentalControlPlatform.App.DevicePanels.HuaTeng;

public sealed class HuaTengProbeResult
{
    public bool Ok { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<HuaTengCameraInfo> Cameras { get; init; } = [];

    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    public string? Exception { get; init; }
}
