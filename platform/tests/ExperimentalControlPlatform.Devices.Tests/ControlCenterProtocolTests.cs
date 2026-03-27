using System;
using ExperimentalControlPlatform.Devices.ControlCenter;
using Xunit;

namespace ExperimentalControlPlatform.Devices.Tests;

public sealed class ControlCenterProtocolTests
{
    private static readonly ControlCenterDeviceInfo TestDevice = ControlCenterDeviceInfo.FromPortName("COM5");

    [Fact]
    public void FormatCommand_UsesLegacyAsciiShape()
    {
        var formatted = ControlCenterProtocol.FormatCommand(new ControlCenterCommand(
            PuffEnabled: true,
            LaserEnabled: false,
            StepCount: 42));

        Assert.Equal("1,0,42\n", formatted);
    }

    [Fact]
    public void ParsePulseReadback_ParsesTimestampAndPulseCount()
    {
        var receivedAt = DateTimeOffset.Parse("2026-03-27T10:15:00Z");

        var readback = ControlCenterProtocol.ParsePulseReadback("1500,80", TestDevice, receivedAt);

        Assert.Equal(TestDevice.DeviceId, readback.DeviceId);
        Assert.Equal(1.5, readback.ControllerTimestampSeconds, 3);
        Assert.Equal(80, readback.PulseCount);
        Assert.Equal(receivedAt, readback.ReceivedAt);
    }
}
