namespace ExperimentalControlPlatform.Devices.ControlCenter;

public sealed record ControlCenterDeviceInfo(
    string PortName,
    string DisplayName,
    string DeviceId)
{
    public static ControlCenterDeviceInfo FromPortName(string portName)
    {
        return new ControlCenterDeviceInfo(
            portName,
            $"Control center ({portName})",
            portName);
    }

    public override string ToString() => DisplayName;
}
