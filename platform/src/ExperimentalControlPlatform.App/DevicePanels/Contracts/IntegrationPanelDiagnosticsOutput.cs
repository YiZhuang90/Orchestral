namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public sealed record IntegrationPanelDiagnosticsOutput
{
    public string? LastCommand { get; init; }

    public string? LastHardwareResponse { get; init; }

    public string? LastError { get; init; }

    public string? LastStateTransition { get; init; }

    public string? LastValidationResult { get; init; }
}
