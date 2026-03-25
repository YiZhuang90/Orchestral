using System.Collections.Generic;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public sealed record IntegrationPanelStatusOutput
{
    public bool Connected { get; init; }

    public string? ReadyState { get; init; }

    public string? FaultState { get; init; }

    public string? LiveState { get; init; }

    public string? SelectedEndpoint { get; init; }

    public IReadOnlyList<string>? BackgroundActiveEndpoints { get; init; }
}
