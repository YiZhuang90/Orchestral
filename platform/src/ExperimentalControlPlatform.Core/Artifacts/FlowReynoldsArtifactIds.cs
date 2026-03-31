namespace ExperimentalControlPlatform.Core.Artifacts;

public static class FlowReynoldsArtifactIds
{
    public static readonly ArtifactId FlowRateStreamId = new("stream.flowrate_lpm");

    public static readonly ArtifactId ReynoldsNumberStreamId = new("stream.reynolds_number");

    public static readonly ArtifactId MeanTemperatureStreamId = new("stream.temperature_mean_c");

    public static readonly ArtifactId TemperatureDeltaStreamId = new("stream.temperature_delta_c");

    public static readonly ArtifactId PrimaryControlTargetId = new("control.re_primary");

    public static readonly ArtifactId PrimaryControlTargetParameterId = new("param.re_target");

    public static readonly ArtifactId PulsesPerLiterParameterId = new("param.control_center_pulses_per_liter");

    public static readonly ArtifactId PipeInnerDiameterParameterId = new("param.pipe_inner_diameter_m");

    public static readonly ArtifactId PipeLengthParameterId = new("param.pipe_length_m");

    public static readonly ArtifactId PipeRoughnessParameterId = new("param.pipe_roughness_m");

    public static readonly ArtifactId ReferenceTemperatureParameterId = new("param.reference_temperature_c");

    public static readonly ArtifactId FlowrateAverageCountParameterId = new("param.flowrate_average_count");

    public static readonly ArtifactId PulsePollIntervalMillisecondsParameterId = new("param.control_center_poll_interval_ms");
}
