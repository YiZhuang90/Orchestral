namespace ExperimentalControlPlatform.App.DevicePanels.HuaTeng;

public sealed class HuaTengCameraInfo
{
    public int Index { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string FriendlyName { get; init; } = string.Empty;

    public string PortType { get; init; } = string.Empty;

    public string SerialNumber { get; init; } = string.Empty;

    public string SensorType { get; init; } = string.Empty;

    public int Instance { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(FriendlyName)
        ? ProductName
        : FriendlyName;
}
