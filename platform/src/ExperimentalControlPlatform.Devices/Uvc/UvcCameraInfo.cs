namespace ExperimentalControlPlatform.Devices.Uvc;

public sealed record UvcCameraInfo(
    int Index,
    string DisplayName,
    string? InstanceId = null,
    string? Manufacturer = null)
{
    public string HeaderDisplayName => string.IsNullOrWhiteSpace(DisplayName)
        ? $"Camera {Index}"
        : DisplayName;

    public override string ToString() => HeaderDisplayName;
}
