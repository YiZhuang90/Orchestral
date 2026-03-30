using System;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public interface IStreamOutputPort<T>
{
    event Action<T>? Produced;

    IStreamDeliverySubscription Subscribe(StreamDeliveryPolicy policy, Func<T, ValueTask> onItem);
}
