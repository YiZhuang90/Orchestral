using System;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ExperimentDefinitionTests
{
    [Fact]
    public void Constructor_Assigns_Required_V1_Artifacts()
    {
        var frameStream = new CapabilityDefinition(
            new ArtifactId("cap.frame_stream"),
            "frame_stream",
            "Produces image frames.");
        var cameraRole = new DeviceRoleDefinition(
            new ArtifactId("role.upstream_camera"),
            "Upstream Camera",
            "Captures the upstream flow view.",
            [frameStream.Id],
            new ArtifactId("protocol.sdk_camera_v1"));
        var samplePeriod = new ParameterDefinition(
            new ArtifactId("param.sample_period_ms"),
            "Sample Period",
            "integer",
            "experiment",
            unit: "ms",
            defaultValue: "50");
        var rawTemperatureStream = new StreamDefinition(
            new ArtifactId("stream.temperature_inlet_samples"),
            "temperature_inlet.samples",
            "device",
            "scalar_sample_stream",
            "sample");
        var turbulenceIndicatorStream = new StreamDefinition(
            new ArtifactId("stream.turbulence_indicator"),
            "turbulence_indicator",
            "transform",
            "derived_metric",
            "sample");
        var turbulenceTransform = new TransformDefinition(
            new ArtifactId("transform.compute_turbulence_indicator"),
            "Compute Turbulence Indicator",
            "Derives a turbulence indicator from raw temperature samples.",
            [rawTemperatureStream.Id],
            [turbulenceIndicatorStream.Id]);
        var temperatureMonitor = new MonitorDefinition(
            new ArtifactId("monitor.temperature_monitor"),
            "Temperature Monitor",
            "Shows live inlet temperature.",
            "derived-state",
            [rawTemperatureStream.Id]);
        var overTemperature = new StopConditionDefinition(
            new ArtifactId("stop.over_temperature"),
            "Over Temperature",
            "safety-based",
            "temperature_inlet.samples > temperature_limit_c for 3s",
            "stop",
            "temperature limit exceeded");
        var runManifest = new OutputDefinition(
            new ArtifactId("output.run_manifest"),
            "Run Manifest",
            "run_manifest",
            "Structured summary of the execution.");

        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe and control turbulence transition with live safety monitoring.",
            [cameraRole],
            [samplePeriod],
            [rawTemperatureStream, turbulenceIndicatorStream],
            [turbulenceTransform],
            [temperatureMonitor],
            [overTemperature],
            [runManifest]);

        Assert.Equal("Turbulence Transition", experiment.Name);
        Assert.Equal("Observe and control turbulence transition with live safety monitoring.", experiment.Purpose);
        Assert.Single(experiment.Roles);
        Assert.Single(experiment.Parameters);
        Assert.Equal(2, experiment.Streams.Count);
        Assert.Single(experiment.Transforms);
        Assert.Single(experiment.Monitors);
        Assert.Single(experiment.StopConditions);
        Assert.Single(experiment.Outputs);
        Assert.Same(cameraRole, experiment.Roles[0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Requires_Non_Empty_Name_And_Purpose(string invalidText)
    {
        var id = new ArtifactId("exp.turbulence_transition_v1");

        var nameException = Assert.Throws<ArgumentException>(() =>
            new ExperimentDefinition(
                id,
                invalidText,
                "Purpose",
                [],
                [],
                [],
                [],
                [],
                [],
                []));
        Assert.Contains("Name", nameException.Message, StringComparison.Ordinal);

        var purposeException = Assert.Throws<ArgumentException>(() =>
            new ExperimentDefinition(
                id,
                "Turbulence Transition",
                invalidText,
                [],
                [],
                [],
                [],
                [],
                [],
                []));
        Assert.Contains("Purpose", purposeException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_Requires_At_Least_One_Device_Role()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ExperimentDefinition(
                new ArtifactId("exp.turbulence_transition_v1"),
                "Turbulence Transition",
                "Observe and control turbulence transition with live safety monitoring.",
                [],
                [],
                [],
                [],
                [],
                [],
                []));

        Assert.Contains("Roles", exception.Message, StringComparison.Ordinal);
    }
}
