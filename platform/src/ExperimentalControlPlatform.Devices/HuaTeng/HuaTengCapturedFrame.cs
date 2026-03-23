namespace ExperimentalControlPlatform.Devices.HuaTeng;

public sealed record HuaTengCapturedFrame(
    HuaTengCameraInfo Camera,
    HuaTengPixelFormat PixelFormat,
    HuaTengTriggerMode TriggerMode,
    int Width,
    int Height,
    byte[] PixelData,
    bool IsMono,
    uint TimestampTenthsOfMilliseconds,
    double ExposureUs,
    HuaTengRoi? Roi);
