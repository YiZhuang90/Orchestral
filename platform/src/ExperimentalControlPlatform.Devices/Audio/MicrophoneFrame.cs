using System;

namespace ExperimentalControlPlatform.Devices.Audio;

public sealed record MicrophoneFrame(
    string DeviceId,
    string DeviceName,
    int SampleRate,
    int Channels,
    MicrophoneChannelMode ChannelMode,
    float[] Samples,
    double RmsDbfs,
    double PeakDbfs,
    bool IsClipping,
    long TimestampTicks,
    TimeSpan WindowDuration);
