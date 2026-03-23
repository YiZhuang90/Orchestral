namespace ExperimentalControlPlatform.Devices.Phantom;

public sealed record PhantomCaptureAttempt(
    bool Attempted,
    bool Succeeded,
    int? RecordStatus,
    int? TriggerStatus,
    uint? CineCountBefore,
    uint? CineCountAfter,
    string Message);
