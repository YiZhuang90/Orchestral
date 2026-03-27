using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record Pt104ChannelState(
    int Channel,
    bool Available,
    Pt104MeasurementMode MeasurementMode,
    int WireCount,
    int MainsFrequencyHz,
    bool FilteredRead,
    bool LiveReading,
    DateTimeOffset? LastSampleTimestamp,
    double? LastSampleValue,
    string? LastSourceMode);
