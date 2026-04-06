using System.Collections.Generic;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.ExperimentLogic;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public interface IRunRecorder
{
    void BeginRun(RuntimeRunContext snapshot);

    RunRecordingResult? CompleteRun(
        RuntimeRunContext snapshot,
        IReadOnlyList<IDeviceTestPanelViewModel> panels,
        FlowReynoldsDerivedStateSnapshot? derivedStateSnapshot = null,
        IReadOnlyList<FlowReynoldsDerivedStateSample>? derivedStateSamples = null,
        ExperimentMonitorSnapshot? monitorSnapshot = null);
}
