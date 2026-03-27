using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed class StreamOutputPort<T> : IStreamOutputPort<T>
{
    private readonly object _syncRoot = new();
    private Action<T>? _produced;

    public event Action<T>? Produced
    {
        add
        {
            lock (_syncRoot)
            {
                _produced += value;
            }
        }
        remove
        {
            lock (_syncRoot)
            {
                _produced -= value;
            }
        }
    }

    public void Publish(T value)
    {
        Action<T>? handlers;
        lock (_syncRoot)
        {
            handlers = _produced;
        }

        handlers?.Invoke(value);
    }
}
