using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public sealed class DeviceSessionRegistry : IDeviceSessionRegistry
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<DeviceSessionId, IDeviceSession> _sessions = [];

    public IReadOnlyCollection<IDeviceSession> Sessions
    {
        get
        {
            lock (_syncRoot)
            {
                return _sessions.Values.ToArray();
            }
        }
    }

    public TSession GetOrAdd<TSession>(DeviceSessionId sessionId, Func<TSession> factory)
        where TSession : class, IDeviceSession
    {
        lock (_syncRoot)
        {
            if (_sessions.TryGetValue(sessionId, out var existing))
            {
                return (TSession)existing;
            }

            var created = factory();
            _sessions.Add(sessionId, created);
            return created;
        }
    }

    public bool TryGet<TSession>(DeviceSessionId sessionId, out TSession? session)
        where TSession : class, IDeviceSession
    {
        lock (_syncRoot)
        {
            if (_sessions.TryGetValue(sessionId, out var existing) && existing is TSession typed)
            {
                session = typed;
                return true;
            }
        }

        session = null;
        return false;
    }

    public bool Remove(DeviceSessionId sessionId)
    {
        lock (_syncRoot)
        {
            return _sessions.Remove(sessionId);
        }
    }

    public async Task StopAllAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        IDeviceSession[] sessions;
        lock (_syncRoot)
        {
            sessions = _sessions.Values.ToArray();
        }

        foreach (var session in sessions)
        {
            await session.StopAsync(reason, cancellationToken).ConfigureAwait(false);
        }
    }
}
