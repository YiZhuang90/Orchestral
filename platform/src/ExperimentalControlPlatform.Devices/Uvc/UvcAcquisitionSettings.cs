namespace ExperimentalControlPlatform.Devices.Uvc;

public sealed record UvcAcquisitionSettings(
    int CameraIndex,
    double TargetFrameRate,
    bool Color);
