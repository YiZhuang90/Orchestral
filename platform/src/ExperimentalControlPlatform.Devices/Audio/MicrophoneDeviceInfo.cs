namespace ExperimentalControlPlatform.Devices.Audio;

public sealed record MicrophoneDeviceInfo(
    int Index,
    string DisplayName,
    string DeviceId,
    int SampleRate,
    int Channels)
{
    public string HeaderDisplayName => DisplayName;

    public override string ToString() => DisplayName;
}
