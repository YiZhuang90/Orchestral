using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public interface IIntegratedCameraRuntimeService
{
    IntegratedCameraFrame CaptureSnapshot(IntegratedCameraCaptureSettings settings);

    Task StreamFramesAsync(
        IntegratedCameraCaptureSettings settings,
        Func<IntegratedCameraFrame, Task> onFrame,
        CancellationToken cancellationToken = default);
}
