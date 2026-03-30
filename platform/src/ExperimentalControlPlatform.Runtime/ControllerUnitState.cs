using System;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ControllerUnitState
{
    public ArtifactId ControlTargetId { get; init; }

    public string ControlTargetName { get; init; } = string.Empty;

    public ArtifactId MeasuredSourceId { get; init; }

    public ArtifactId CommandRoleId { get; init; }

    public string SetpointProfile { get; init; } = string.Empty;

    public string RegulationMode { get; init; } = string.Empty;

    public DateTimeOffset RunStartedAtUtc { get; init; }

    public DateTimeOffset? LastObservedAtUtc { get; init; }

    public TimeSpan? LastElapsedRunTime { get; init; }

    public double? TargetValue { get; init; }

    public double? MeasuredValue { get; init; }

    public DateTimeOffset? MeasuredValueObservedAtUtc { get; init; }

    public bool MeasuredValueIsStale { get; init; }

    public double? ErrorValue { get; init; }

    public double? ControlOutputValue { get; init; }

    public string ControlOutputInterpretation { get; init; } = string.Empty;

    public long SampleSequence { get; init; }

    public string StatusMessage { get; init; } = "Controller unit ready.";
}
