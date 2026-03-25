using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Devices.Audio;

public interface IMicrophoneService
{
    IReadOnlyList<MicrophoneDeviceInfo> ListCaptureDevices();

    MicrophoneFrame CaptureSnapshot(MicrophoneCaptureSettings settings);

    Task StreamFramesAsync(
        MicrophoneCaptureSettings settings,
        Func<MicrophoneFrame, Task> onFrame,
        CancellationToken cancellationToken = default);
}
