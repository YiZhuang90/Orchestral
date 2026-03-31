using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ControlCenterFlowTelemetryState
{
    public int? LastPulseCount { get; init; }

    public double? LastControllerTimestampSeconds { get; init; }

    public DateTimeOffset? LastObservedAtUtc { get; init; }

    public long PulseSequence { get; init; }

    public string StatusMessage { get; init; } = "Awaiting flow telemetry.";
}
