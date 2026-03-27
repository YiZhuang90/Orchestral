using System;
using System.Globalization;

namespace ExperimentalControlPlatform.Devices.ControlCenter;

public static class ControlCenterProtocol
{
    public const int DefaultBaudRate = 9600;

    public static string FormatCommand(ControlCenterCommand command)
    {
        return $"{BoolToDigit(command.PuffEnabled)},{BoolToDigit(command.LaserEnabled)},{command.StepCount}\n";
    }

    public static ControlCenterPulseReadback ParsePulseReadback(
        string line,
        ControlCenterDeviceInfo device,
        DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentException.ThrowIfNullOrWhiteSpace(line);

        var parts = line.Trim().Split(',');
        if (parts.Length != 2)
        {
            throw new FormatException($"Expected '<timestamp_ms>,<pulse_count>' but received '{line}'.");
        }

        if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampMs))
        {
            throw new FormatException($"Unable to parse controller timestamp from '{line}'.");
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pulseCount))
        {
            throw new FormatException($"Unable to parse pulse count from '{line}'.");
        }

        return new ControlCenterPulseReadback(
            device.DeviceId,
            device.DisplayName,
            timestampMs / 1000d,
            pulseCount,
            receivedAt);
    }

    private static int BoolToDigit(bool value) => value ? 1 : 0;
}
