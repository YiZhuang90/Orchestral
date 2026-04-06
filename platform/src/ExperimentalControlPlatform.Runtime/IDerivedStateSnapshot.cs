using System;

namespace ExperimentalControlPlatform.Runtime;

/// <summary>
/// Snapshot of derived-state values consumed by runtime monitoring infrastructure.
/// Experiment-logic layers implement this with their specific derived quantities.
/// </summary>
public interface IDerivedStateSnapshot
{
    DateTimeOffset? ObservedAtUtc { get; }

    double? FilteredFlowRateLitersPerMinute { get; }

    double? ReynoldsNumber { get; }

    double? MeanTemperatureC { get; }

    bool UsesFallbackTemperature { get; }

    bool PulseTelemetryIsStale { get; }

    bool TemperatureIsStale { get; }
}
