namespace ExperimentalControlPlatform.Runtime;

public sealed record HuaTengCaptureSettings(
    string DeviceId,
    int CameraIndex,
    string DisplayName,
    string PixelFormat,
    string TriggerMode,
    string ColorTone,
    double? ExposureUs,
    double TargetFrameRate,
    CaptureRegion? Roi);
