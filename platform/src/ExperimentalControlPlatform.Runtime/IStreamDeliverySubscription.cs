using System;

namespace ExperimentalControlPlatform.Runtime;

public interface IStreamDeliverySubscription : IDisposable
{
    StreamDeliveryPolicy Policy { get; }

    long DeliveredItemCount { get; }

    long DroppedItemCount { get; }
}
