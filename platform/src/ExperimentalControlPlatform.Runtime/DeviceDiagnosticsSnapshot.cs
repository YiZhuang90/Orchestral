namespace ExperimentalControlPlatform.Runtime;

public sealed record DeviceDiagnosticsSnapshot
{
    public string? LastCommand { get; init; }

    public string? LastHardwareResponse { get; init; }

    public string? LastError { get; init; }

    public string? LastStateTransition { get; init; }

    public string? LastValidationResult { get; init; }
}
