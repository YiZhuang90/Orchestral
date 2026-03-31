using System.Collections.Generic;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.App;

public interface IRunDefinitionFactory
{
    ResolvedExperimentDefinition Create(IEnumerable<IDeviceTestPanelViewModel> panels);

    ResolvedExperimentDefinition Create(IEnumerable<IDeviceTestPanelViewModel> panels, double? primaryControlTargetValue);
}
