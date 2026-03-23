using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Devices.Uvc;

public interface IUvcCameraService
{
    IReadOnlyList<UvcCameraInfo> ListCameras();

    UvcFrame CaptureSnapshot(UvcAcquisitionSettings settings);

    Task StreamFramesAsync(
        UvcAcquisitionSettings settings,
        Func<UvcFrame, Task> onFrame,
        CancellationToken cancellationToken = default);
}
