using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record Pt104Reading(
    string DeviceId,
    int Channel,
    double ValueCelsius,
    DateTimeOffset CapturedAt,
    string SourceMode);
