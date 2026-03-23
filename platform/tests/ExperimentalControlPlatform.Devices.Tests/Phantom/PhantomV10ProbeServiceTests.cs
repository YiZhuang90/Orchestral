using System;
using System.Collections.Generic;
using System.IO;
using ExperimentalControlPlatform.Devices.Phantom;
using Xunit;

namespace ExperimentalControlPlatform.Devices.Tests.Phantom;

public sealed class PhantomV10ProbeServiceTests
{
    [Fact]
    public void TryLocate_ReturnsInstall_WhenManagedAssemblyExists()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var phSharpPath = Path.Combine(root.FullName, "PhSharp.Dll");
            File.WriteAllText(phSharpPath, "placeholder");

            var locator = new PhantomSdkLocator(new[] { root.FullName });

            var found = locator.TryLocate(out var install);

            Assert.True(found);
            Assert.NotNull(install);
            Assert.Equal(root.FullName, install!.RootDirectory);
            Assert.Equal(phSharpPath, install.ManagedAssemblyPath);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Probe_ReturnsMissingSdkResult_WhenNoInstallExists()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var service = new PhantomV10ProbeService(
                new PhantomSdkLocator(new[] { root.FullName }),
                _ => throw new InvalidOperationException("SDK factory should not be used when install is missing."));

            var result = service.Probe(expectedIpAddress: "100.100.77.211", attemptCapture: false);

            Assert.False(result.SdkFound);
            Assert.Equal("inferred but unverified", result.TrustStatus);
            Assert.Empty(result.Cameras);
            Assert.Contains("Phantom SDK runtime was not found", result.Summary, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Probe_ReturnsStructuredFailure_WhenSdkSeesNoCameras()
    {
        var install = new PhantomSdkInstall(@"C:\Program Files\Phantom", @"C:\Program Files\Phantom\PhSharp.Dll");
        var service = new PhantomV10ProbeService(
            new StubLocator(install),
            _ => new StubSdk("3.11.11.806", []));

        var result = service.Probe(expectedIpAddress: "100.100.77.211", attemptCapture: false);

        Assert.True(result.SdkFound);
        Assert.Empty(result.Cameras);
        Assert.False(result.Capture.Attempted);
        Assert.Equal("supported by local network evidence", result.TrustStatus);
        Assert.Contains("reachable camera peer may exist", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Probe_SelectsExpectedIpCamera_AndCapturesWhenRequested()
    {
        var install = new PhantomSdkInstall(@"C:\Program Files\Phantom", @"C:\Program Files\Phantom\PhSharp.Dll");
        var matchingCamera = new PhantomCameraInfo(7, 1007, 4242, "Phantom v10", "100.100.77.211", "Bench camera");
        var otherCamera = new PhantomCameraInfo(2, 1002, 2424, "Phantom v611", "100.100.88.10", "Other");
        var capture = new PhantomCaptureAttempt(true, true, 0, 0, 0, 1, "Single cine trigger completed.");
        var sdk = new StubSdk("3.11.11.806", [otherCamera, matchingCamera], capture);
        var service = new PhantomV10ProbeService(new StubLocator(install), _ => sdk);

        var result = service.Probe(expectedIpAddress: "100.100.77.211", attemptCapture: true);

        Assert.True(result.Capture.Succeeded);
        Assert.Equal((uint)7, sdk.CapturedCameraNumber);
        Assert.Equal("verified by vendor SDK", result.TrustStatus);
    }

    private sealed class StubLocator : IPhantomSdkLocator
    {
        private readonly PhantomSdkInstall _install;

        public StubLocator(PhantomSdkInstall install)
        {
            _install = install;
        }

        public bool TryLocate(out PhantomSdkInstall? install)
        {
            install = _install;
            return true;
        }
    }

    private sealed class StubSdk : IPhantomVendorSdk
    {
        private readonly IReadOnlyList<PhantomCameraInfo> _cameras;
        private readonly PhantomCaptureAttempt _capture;

        public StubSdk(string version, IReadOnlyList<PhantomCameraInfo> cameras, PhantomCaptureAttempt? capture = null)
        {
            Version = version;
            _cameras = cameras;
            _capture = capture ?? new PhantomCaptureAttempt(false, false, null, null, null, null, "Capture was not requested.");
        }

        public string Version { get; }

        public uint? CapturedCameraNumber { get; private set; }

        public string GetSdkVersion() => Version;

        public IReadOnlyList<PhantomCameraInfo> DiscoverVisibleCameras(string? expectedIpAddress, IList<string> diagnostics)
        {
            diagnostics.Add($"Discovery called with expectedIpAddress={expectedIpAddress ?? "<none>"}");
            return _cameras;
        }

        public PhantomCaptureAttempt CaptureSingleCine(uint cameraNumber, IList<string> diagnostics)
        {
            CapturedCameraNumber = cameraNumber;
            diagnostics.Add($"Capture requested for cameraNumber={cameraNumber}");
            return _capture;
        }
    }
}
