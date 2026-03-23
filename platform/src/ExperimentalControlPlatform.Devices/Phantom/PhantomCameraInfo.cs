namespace ExperimentalControlPlatform.Devices.Phantom;

public sealed record PhantomCameraInfo(
    uint CameraNumber,
    uint CameraId,
    uint Serial,
    string Model,
    string IpAddress,
    string Description);
