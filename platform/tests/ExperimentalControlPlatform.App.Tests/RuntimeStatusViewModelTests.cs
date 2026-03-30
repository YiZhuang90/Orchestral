using System.Collections.Generic;
using ExperimentalControlPlatform.App;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Runtime;
using Xunit;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class RuntimeStatusViewModelTests
{
    [Fact]
    public void StateSummary_Uses_Run_Context_Display_Name_And_Operator_Note()
    {
        var runContext = new RunContextDefinition(
            new ArtifactId("runctx.transition_demo_001"),
            CreateResolvedExperimentDefinition(),
            "transition-demo-001",
            "looping mode, RR=0.36",
            new Dictionary<ArtifactId, string>(),
            new Dictionary<ArtifactId, ArtifactId>(),
            [],
            []);
        var viewModel = new RuntimeStatusViewModel(
            new RuntimeRunContext(
                Guid.NewGuid(),
                RunState.Running,
                DateTimeOffset.Parse("2026-03-30T10:00:00+02:00"),
                runContext: runContext));

        Assert.Contains("transition-demo-001", viewModel.StateSummary);
        Assert.Contains("looping mode, RR=0.36", viewModel.StateSummary);
    }

    private static ResolvedExperimentDefinition CreateResolvedExperimentDefinition()
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe the flow with a camera and a controller.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.camera_upstream"),
                    "Upstream Camera",
                    "Captures the upstream flow region.",
                    [new ArtifactId("cap.frame_stream")],
                    new ArtifactId("protocol.vendor_sdk_camera_v1"))
            ],
            [],
            [],
            [],
            [],
            [],
            []);
        var device = new DeviceDefinition(
            new ArtifactId("device.camera_01"),
            "Camera 01",
            "camera_01",
            new ProtocolDefinition(
                new ArtifactId("protocol.vendor_sdk_camera_v1"),
                "Vendor SDK Camera",
                "sdk",
                "request-response",
                "session",
                "best-effort",
                "fail-fast",
                "manual"),
            [
                new CapabilityDefinition(new ArtifactId("cap.frame_stream"), "frame_stream", "Produces image frames.")
            ],
            [],
            "healthy");
        var binding = new RoleBindingDefinition(
            new ArtifactId("role.camera_upstream"),
            device.Id,
            device.Protocol.Id,
            [new ArtifactId("cap.frame_stream")]);

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [device],
            [binding],
            new Dictionary<ArtifactId, string>());
    }
}
