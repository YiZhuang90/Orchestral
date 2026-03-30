using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ExperimentMonitorItem(
    string Id,
    ExperimentMonitorSeverity Severity,
    string Source,
    string Message,
    DateTimeOffset ObservedAtUtc);
