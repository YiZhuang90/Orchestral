using System;

namespace ExperimentalControlPlatform.Runtime;

public interface ISnapshotOutputPort<T>
{
    T? Current { get; }

    event Action<T>? Changed;
}
