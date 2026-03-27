namespace ExperimentalControlPlatform.Runtime;

public sealed record IntegratedCameraFrame(
    string DeviceId,
    string DisplayName,
    int Width,
    int Height,
    byte[] PixelData,
    bool IsColor,
    long TimestampTicks);
