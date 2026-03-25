using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.Audio;

namespace ExperimentalControlPlatform.App.DevicePanels.Microphone;

public sealed class IntegratedMicrophoneClient
{
    private readonly IMicrophoneService _service;

    public IntegratedMicrophoneClient(IMicrophoneService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public IReadOnlyList<MicrophoneDeviceInfo> ListDevices() => _service.ListCaptureDevices();

    public MicrophoneFrame CaptureSnapshot(
        string deviceId,
        double targetUpdateRateHz,
        int windowMilliseconds,
        MicrophoneChannelMode channelMode)
    {
        return _service.CaptureSnapshot(new MicrophoneCaptureSettings(
            deviceId,
            targetUpdateRateHz,
            windowMilliseconds,
            channelMode));
    }

    public Task StreamFramesAsync(
        string deviceId,
        double targetUpdateRateHz,
        int windowMilliseconds,
        MicrophoneChannelMode channelMode,
        Func<MicrophoneFrame, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        return _service.StreamFramesAsync(
            new MicrophoneCaptureSettings(
                deviceId,
                targetUpdateRateHz,
                windowMilliseconds,
                channelMode),
            onFrame,
            cancellationToken);
    }
}
