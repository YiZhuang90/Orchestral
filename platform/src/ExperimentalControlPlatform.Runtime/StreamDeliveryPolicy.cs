using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record StreamDeliveryPolicy(
    StreamDeliveryMode Mode,
    int? MaxBufferedItems = null,
    double? MaxDeliveryRateHz = null,
    StreamOverflowPolicy OverflowPolicy = StreamOverflowPolicy.DropOldest)
{
    public static StreamDeliveryPolicy Ordered(
        int? maxBufferedItems = null,
        double? maxDeliveryRateHz = null,
        StreamOverflowPolicy overflowPolicy = StreamOverflowPolicy.DropOldest)
    {
        var policy = new StreamDeliveryPolicy(StreamDeliveryMode.Ordered, maxBufferedItems, maxDeliveryRateHz, overflowPolicy);
        policy.Validate();
        return policy;
    }

    public static StreamDeliveryPolicy LatestOnly(double? maxDeliveryRateHz = null)
    {
        var policy = new StreamDeliveryPolicy(StreamDeliveryMode.LatestOnly, 1, maxDeliveryRateHz, StreamOverflowPolicy.DropOldest);
        policy.Validate();
        return policy;
    }

    public void Validate()
    {
        if (MaxBufferedItems.HasValue && MaxBufferedItems.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxBufferedItems), "MaxBufferedItems must be positive when provided.");
        }

        if (MaxDeliveryRateHz.HasValue && MaxDeliveryRateHz.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDeliveryRateHz), "MaxDeliveryRateHz must be positive when provided.");
        }

        if (Mode == StreamDeliveryMode.LatestOnly && MaxBufferedItems is > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxBufferedItems), "LatestOnly delivery may buffer at most one item.");
        }
    }
}
