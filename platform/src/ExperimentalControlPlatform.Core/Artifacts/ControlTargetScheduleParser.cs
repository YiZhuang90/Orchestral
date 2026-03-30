using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public static class ControlTargetScheduleParser
{
    public static IReadOnlyList<ControlTargetSchedulePoint> Parse(string value, ArtifactId parameterId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Schedule parameter '{parameterId}' must not be empty.", nameof(value));
        }

        var points = new List<ControlTargetSchedulePoint>();
        var segments = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var parts = segment.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new FormatException(
                    $"Schedule parameter '{parameterId}' must use 'seconds:value' segments separated by ';'.");
            }

            var seconds = ParseDouble(parts[0], parameterId);
            var scheduleValue = ParseDouble(parts[1], parameterId);
            points.Add(new ControlTargetSchedulePoint(TimeSpan.FromSeconds(seconds), scheduleValue));
        }

        return points.OrderBy(static point => point.Offset).ToArray();
    }

    private static double ParseDouble(string value, ArtifactId parameterId)
    {
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new FormatException($"Parameter '{parameterId}' must contain a finite numeric value.");
        }

        if (double.IsNaN(parsed) || double.IsInfinity(parsed))
        {
            throw new FormatException($"Parameter '{parameterId}' must contain a finite numeric value.");
        }

        return parsed;
    }
}
