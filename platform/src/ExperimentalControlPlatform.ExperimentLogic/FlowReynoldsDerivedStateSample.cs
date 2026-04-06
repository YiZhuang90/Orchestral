using System;

namespace ExperimentalControlPlatform.ExperimentLogic;

public sealed record FlowReynoldsDerivedStateSample(
    DateTimeOffset ObservedAtUtc,
    double RawFlowRateLitersPerMinute,
    double FilteredFlowRateLitersPerMinute,
    double MeanTemperatureC,
    double? TemperatureDeltaC,
    double BulkVelocityMetersPerSecond,
    double ReynoldsNumber,
    bool UsesFallbackTemperature,
    bool PulseTelemetryIsStale,
    bool TemperatureIsStale);
