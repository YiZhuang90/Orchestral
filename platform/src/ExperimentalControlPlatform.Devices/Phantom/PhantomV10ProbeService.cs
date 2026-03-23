using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Devices.Phantom;

public sealed class PhantomV10ProbeService
{
    private readonly IPhantomSdkLocator _sdkLocator;
    private readonly Func<PhantomSdkInstall, IPhantomVendorSdk> _sdkFactory;

    public PhantomV10ProbeService()
        : this(new PhantomSdkLocator(), install => new ReflectionPhantomVendorSdk(install))
    {
    }

    internal PhantomV10ProbeService(IPhantomSdkLocator sdkLocator, Func<PhantomSdkInstall, IPhantomVendorSdk> sdkFactory)
    {
        _sdkLocator = sdkLocator ?? throw new ArgumentNullException(nameof(sdkLocator));
        _sdkFactory = sdkFactory ?? throw new ArgumentNullException(nameof(sdkFactory));
    }

    public PhantomProbeResult Probe(string? expectedIpAddress = null, bool attemptCapture = false)
    {
        if (!_sdkLocator.TryLocate(out var install) || install is null)
        {
            return new PhantomProbeResult(
                SdkFound: false,
                Install: null,
                SdkVersion: "Unavailable",
                TrustStatus: "inferred but unverified",
                Summary: "Phantom SDK runtime was not found on this PC.",
                ExpectedIpAddress: expectedIpAddress,
                Cameras: Array.Empty<PhantomCameraInfo>(),
                Capture: new PhantomCaptureAttempt(false, false, null, null, null, null, "Capture was not attempted because the Phantom SDK was not installed."),
                Diagnostics:
                [
                    "Expected managed Phantom runtime: PhSharp.Dll",
                    @"Default probe root: C:\Program Files\Phantom",
                ]);
        }

        var diagnostics = new List<string>
        {
            $"Using Phantom runtime root: {install.RootDirectory}",
            $"Using Phantom managed assembly: {install.ManagedAssemblyPath}",
        };

        var sdk = _sdkFactory(install);
        var sdkVersion = sdk.GetSdkVersion();
        diagnostics.Add($"Phantom SDK version: {sdkVersion}");

        var cameras = sdk.DiscoverVisibleCameras(expectedIpAddress, diagnostics);
        if (cameras.Count == 0)
        {
            return new PhantomProbeResult(
                SdkFound: true,
                Install: install,
                SdkVersion: sdkVersion,
                TrustStatus: string.IsNullOrWhiteSpace(expectedIpAddress)
                    ? "inferred but unverified"
                    : "supported by local network evidence",
                Summary: string.IsNullOrWhiteSpace(expectedIpAddress)
                    ? "Phantom SDK is installed, but it did not report any visible cameras."
                    : $"Phantom SDK is installed, but it did not report any visible cameras even though a reachable camera peer may exist at {expectedIpAddress}.",
                ExpectedIpAddress: expectedIpAddress,
                Cameras: Array.Empty<PhantomCameraInfo>(),
                Capture: new PhantomCaptureAttempt(false, false, null, null, null, null, "Capture was not attempted because no Phantom cameras were visible to the vendor SDK."),
                Diagnostics: diagnostics);
        }

        var selectedCamera = SelectCamera(cameras, expectedIpAddress);
        var capture = attemptCapture
            ? sdk.CaptureSingleCine(selectedCamera.CameraNumber, diagnostics)
            : new PhantomCaptureAttempt(false, false, null, null, null, null, "Capture was not requested.");

        var summary = capture.Attempted
            ? capture.Message
            : $"Phantom SDK discovered {cameras.Count} camera(s); selected camera #{selectedCamera.CameraNumber} for the first bounded probe.";

        return new PhantomProbeResult(
            SdkFound: true,
            Install: install,
            SdkVersion: sdkVersion,
            TrustStatus: "verified by vendor SDK",
            Summary: summary,
            ExpectedIpAddress: expectedIpAddress,
            Cameras: cameras,
            Capture: capture,
            Diagnostics: diagnostics);
    }

    private static PhantomCameraInfo SelectCamera(IReadOnlyList<PhantomCameraInfo> cameras, string? expectedIpAddress)
    {
        if (!string.IsNullOrWhiteSpace(expectedIpAddress))
        {
            var exactMatch = cameras.FirstOrDefault(camera =>
                string.Equals(camera.IpAddress, expectedIpAddress, StringComparison.OrdinalIgnoreCase));
            if (exactMatch is not null)
            {
                return exactMatch;
            }
        }

        var v10Match = cameras.FirstOrDefault(camera =>
            camera.Model.Contains("v10", StringComparison.OrdinalIgnoreCase));
        return v10Match ?? cameras[0];
    }
}
