using System;

namespace ExperimentalControlPlatform.Runtime;

public interface IExperimentMonitorSource : IDisposable
{
    event Action? Changed;

    ExperimentMonitorDeviceSnapshot CreateSnapshot();
}
