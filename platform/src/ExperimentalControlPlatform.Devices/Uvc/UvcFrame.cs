namespace ExperimentalControlPlatform.Devices.Uvc;

public sealed record UvcFrame(
    int Width,
    int Height,
    byte[] PixelData,
    bool IsColor,
    long TimestampTicks);
