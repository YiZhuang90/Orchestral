namespace ExperimentalControlPlatform.Devices.ControlCenter;

public sealed record ControlCenterCommand(
    bool PuffEnabled,
    bool LaserEnabled,
    int StepCount);
