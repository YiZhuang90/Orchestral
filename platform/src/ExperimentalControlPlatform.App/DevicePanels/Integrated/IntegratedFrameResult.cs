namespace ExperimentalControlPlatform.App.DevicePanels.Integrated;

public sealed record IntegratedFrameResult(
    int Width,
    int Height,
    byte[] PixelData,
    bool IsColor,
    long TimestampTicks);
