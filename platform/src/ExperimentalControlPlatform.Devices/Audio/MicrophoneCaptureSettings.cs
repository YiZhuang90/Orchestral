namespace ExperimentalControlPlatform.Devices.Audio;

public sealed record MicrophoneCaptureSettings(
    string DeviceId,
    double TargetUpdateRateHz,
    int WindowMilliseconds,
    MicrophoneChannelMode ChannelMode);
