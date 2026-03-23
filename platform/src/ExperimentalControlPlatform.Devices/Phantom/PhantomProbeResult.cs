using System.Collections.Generic;

namespace ExperimentalControlPlatform.Devices.Phantom;

public sealed record PhantomProbeResult(
    bool SdkFound,
    PhantomSdkInstall? Install,
    string SdkVersion,
    string TrustStatus,
    string Summary,
    string? ExpectedIpAddress,
    IReadOnlyList<PhantomCameraInfo> Cameras,
    PhantomCaptureAttempt Capture,
    IReadOnlyList<string> Diagnostics);
