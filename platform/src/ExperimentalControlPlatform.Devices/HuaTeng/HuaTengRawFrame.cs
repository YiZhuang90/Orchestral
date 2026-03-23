namespace ExperimentalControlPlatform.Devices.HuaTeng;

public sealed record HuaTengRawFrame(
    int Width,
    int Height,
    byte[] PixelData,
    uint TimestampTenthsOfMilliseconds,
    double ExposureUs);
