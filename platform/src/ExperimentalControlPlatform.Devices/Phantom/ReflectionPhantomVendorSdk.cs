using System;
using System.Collections.Generic;

namespace ExperimentalControlPlatform.Devices.Phantom;

internal sealed class ReflectionPhantomVendorSdk : IPhantomVendorSdk
{
    public ReflectionPhantomVendorSdk(PhantomSdkInstall install)
    {
        ArgumentNullException.ThrowIfNull(install);
    }

    public string GetSdkVersion() => "Unavailable";

    public IReadOnlyList<PhantomCameraInfo> DiscoverVisibleCameras(string? expectedIpAddress, IList<string> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        diagnostics.Add("Reflection-backed Phantom SDK probing is not fully wired into the repo build yet.");
        if (!string.IsNullOrWhiteSpace(expectedIpAddress))
        {
            diagnostics.Add($"Expected camera IP hint: {expectedIpAddress}");
        }

        return Array.Empty<PhantomCameraInfo>();
    }

    public PhantomCaptureAttempt CaptureSingleCine(uint cameraNumber, IList<string> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        diagnostics.Add($"Capture was requested for camera #{cameraNumber}, but the reflection-backed Phantom SDK adapter is not implemented yet.");
        return new PhantomCaptureAttempt(
            Attempted: true,
            Succeeded: false,
            RecordStatus: null,
            TriggerStatus: null,
            CineCountBefore: null,
            CineCountAfter: null,
            Message: "Capture is not implemented in the current repo slice.");
    }
}
