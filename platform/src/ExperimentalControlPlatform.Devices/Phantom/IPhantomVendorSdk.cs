using System.Collections.Generic;

namespace ExperimentalControlPlatform.Devices.Phantom;

internal interface IPhantomVendorSdk
{
    string GetSdkVersion();

    IReadOnlyList<PhantomCameraInfo> DiscoverVisibleCameras(string? expectedIpAddress, IList<string> diagnostics);

    PhantomCaptureAttempt CaptureSingleCine(uint cameraNumber, IList<string> diagnostics);
}
