using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public interface IHuaTengCameraRuntimeService
{
    Task<HuaTengFrame> CaptureSnapshotAsync(HuaTengCaptureSettings settings, CancellationToken cancellationToken = default);

    Task StreamFramesAsync(
        HuaTengCaptureSettings settings,
        Func<HuaTengFrame, Task> onFrame,
        CancellationToken cancellationToken = default);
}
