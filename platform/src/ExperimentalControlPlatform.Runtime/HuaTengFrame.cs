namespace ExperimentalControlPlatform.Runtime;

public sealed record HuaTengFrame(
    string DeviceId,
    string DisplayName,
    bool Ok,
    string Summary,
    int Width,
    int Height,
    byte[] PixelData,
    string PixelFormat,
    string TriggerMode,
    string ColorTone,
    double ExposureUs,
    bool IsMono,
    int TimestampTenthsOfMilliseconds,
    CaptureRegion? Roi,
    IReadOnlyList<string> Diagnostics);
