using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ExperimentMonitorSnapshot
{
    public Guid RunId { get; init; }

    public RunState RunState { get; init; }

    public DateTimeOffset ObservedAtUtc { get; init; }

    public string RunDisplayName { get; init; } = "Experiment Monitor";

    public string StateSummary { get; init; } = "Ready to initialize.";

    public ExperimentMonitorSeverity HighestSeverity { get; init; }

    public int WarningCount { get; init; }

    public int AlarmCount { get; init; }

    public IReadOnlyList<ExperimentMonitorItem> Items { get; init; } = Array.Empty<ExperimentMonitorItem>();

    public IReadOnlyList<ExperimentMonitorDeviceSnapshot> Devices { get; init; } = Array.Empty<ExperimentMonitorDeviceSnapshot>();

    public string? PrimaryControlLabel { get; init; }

    public double? PrimaryControlTargetValue { get; init; }

    public double? PrimaryControlMeasuredValue { get; init; }

    public double? PrimaryControlErrorValue { get; init; }

    public bool PrimaryControlMeasuredValueIsStale { get; init; }

    public string PrimaryControlSummary { get; init; } = "No active control target.";

    public double? DerivedFlowRateLitersPerMinute { get; init; }

    public double? DerivedReynoldsNumber { get; init; }

    public double? DerivedMeanTemperatureC { get; init; }

    public bool DerivedStateIsStale { get; init; }

    public string DerivedStateSummary { get; init; } = "No derived flow state.";

    public IReadOnlyList<string> ActiveDeviceNames =>
        Devices.Select(static device => device.DisplayName).ToArray();
}
