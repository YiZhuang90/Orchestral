using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record ControlCenterAppliedState(
    bool PuffEnabled,
    bool LaserEnabled,
    int StepCount,
    DateTimeOffset AppliedAt,
    string TransportNote);
