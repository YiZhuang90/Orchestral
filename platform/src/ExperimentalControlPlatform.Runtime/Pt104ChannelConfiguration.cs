namespace ExperimentalControlPlatform.Runtime;

public sealed record Pt104ChannelConfiguration(
    int Channel,
    Pt104MeasurementMode MeasurementMode,
    int WireCount,
    int MainsFrequencyHz,
    bool FilteredRead);
