using System.Collections.Generic;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public interface IRunRecorder
{
    void BeginRun(RuntimeRunContext snapshot);

    RunRecordingResult? CompleteRun(RuntimeRunContext snapshot, IReadOnlyList<IDeviceTestPanelViewModel> panels);
}
