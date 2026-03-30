using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ControlTargetSchedulePoint
{
    public ControlTargetSchedulePoint(TimeSpan offset, double value)
    {
        if (offset < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be zero or greater.");
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Schedule value must be finite.");
        }

        Offset = offset;
        Value = value;
    }

    public TimeSpan Offset { get; }

    public double Value { get; }
}
