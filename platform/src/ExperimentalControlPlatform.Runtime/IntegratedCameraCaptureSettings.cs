namespace ExperimentalControlPlatform.Runtime;

public sealed record IntegratedCameraCaptureSettings(
    string DeviceId,
    int CameraIndex,
    string DisplayName,
    double TargetFrameRate,
    bool ColorEnabled);
