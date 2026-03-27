using System;

namespace ExperimentalControlPlatform.Devices.ControlCenter;

public interface IControlCenterConnection : IDisposable
{
    ControlCenterDeviceInfo Device { get; }
}
