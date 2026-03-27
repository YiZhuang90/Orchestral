using System;

namespace ExperimentalControlPlatform.Runtime;

public interface IStreamOutputPort<T>
{
    event Action<T>? Produced;
}
