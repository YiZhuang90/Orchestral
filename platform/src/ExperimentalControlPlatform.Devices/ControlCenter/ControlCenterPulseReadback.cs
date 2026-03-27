using System;

namespace ExperimentalControlPlatform.Devices.ControlCenter;

public sealed record ControlCenterPulseReadback(
    string DeviceId,
    string DeviceName,
    double ControllerTimestampSeconds,
    int PulseCount,
    DateTimeOffset ReceivedAt);
