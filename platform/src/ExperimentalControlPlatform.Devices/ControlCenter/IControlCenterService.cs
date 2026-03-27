using System.Collections.Generic;

namespace ExperimentalControlPlatform.Devices.ControlCenter;

public interface IControlCenterService
{
    IReadOnlyList<ControlCenterDeviceInfo> ListDevices();

    IControlCenterConnection Open(ControlCenterDeviceInfo device);

    void SendCommand(IControlCenterConnection connection, ControlCenterCommand command);

    ControlCenterPulseReadback ReadPulseCount(IControlCenterConnection connection);
}
