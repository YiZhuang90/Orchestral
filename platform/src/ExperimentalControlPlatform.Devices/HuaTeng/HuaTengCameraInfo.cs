namespace ExperimentalControlPlatform.Devices.HuaTeng;

public sealed record HuaTengCameraInfo(
    int Index,
    string ProductName,
    string FriendlyName,
    string PortType,
    string SerialNumber,
    string SensorType,
    int Instance)
{
    public string DisplayName => string.IsNullOrWhiteSpace(FriendlyName)
        ? ProductName
        : FriendlyName;
}
