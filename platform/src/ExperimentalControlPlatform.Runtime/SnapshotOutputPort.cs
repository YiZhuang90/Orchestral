using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed class SnapshotOutputPort<T> : ISnapshotOutputPort<T>
{
    private readonly object _syncRoot = new();
    private T? _current;
    private Action<T>? _changed;

    public SnapshotOutputPort()
    {
    }

    public SnapshotOutputPort(T initialValue)
    {
        _current = initialValue;
    }

    public T? Current
    {
        get
        {
            lock (_syncRoot)
            {
                return _current;
            }
        }
    }

    public event Action<T>? Changed
    {
        add
        {
            lock (_syncRoot)
            {
                _changed += value;
            }
        }
        remove
        {
            lock (_syncRoot)
            {
                _changed -= value;
            }
        }
    }

    public void Publish(T value)
    {
        Action<T>? handlers;
        lock (_syncRoot)
        {
            _current = value;
            handlers = _changed;
        }

        handlers?.Invoke(value);
    }
}
