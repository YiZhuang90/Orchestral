using System;

namespace ExperimentalControlPlatform.Runtime;

public readonly record struct DeviceSessionId
{
    public DeviceSessionId(string deviceFamily, string deviceId)
    {
        DeviceFamily = RequireText(deviceFamily, nameof(deviceFamily));
        DeviceId = RequireText(deviceId, nameof(deviceId));
    }

    public string DeviceFamily { get; }

    public string DeviceId { get; }

    public override string ToString() => $"{DeviceFamily}:{DeviceId}";

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        return value.Trim();
    }
}
