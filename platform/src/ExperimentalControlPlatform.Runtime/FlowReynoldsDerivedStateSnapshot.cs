using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record FlowReynoldsDerivedStateSnapshot
{
    public DateTimeOffset? ObservedAtUtc { get; init; }

    public DateTimeOffset? PulseObservedAtUtc { get; init; }

    public double? PulseTimestampSeconds { get; init; }

    public int? LatestPulseCount { get; init; }

    public DateTimeOffset? TemperatureObservedAtUtc { get; init; }

    public double? RawFlowRateLitersPerMinute { get; init; }

    public double? FilteredFlowRateLitersPerMinute { get; init; }

    public double? MeanTemperatureC { get; init; }

    public double? TemperatureDeltaC { get; init; }

    public double? BulkVelocityMetersPerSecond { get; init; }

    public double? ReynoldsNumber { get; init; }

    public bool UsesFallbackTemperature { get; init; }

    public bool PulseTelemetryIsStale { get; init; }

    public bool TemperatureIsStale { get; init; }

    public long SampleSequence { get; init; }

    public string StatusMessage { get; init; } = "Flow/Reynolds derived state ready.";
}
