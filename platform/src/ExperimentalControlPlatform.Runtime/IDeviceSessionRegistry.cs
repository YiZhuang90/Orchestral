using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public interface IDeviceSessionRegistry
{
    IReadOnlyCollection<IDeviceSession> Sessions { get; }

    TSession GetOrAdd<TSession>(DeviceSessionId sessionId, Func<TSession> factory)
        where TSession : class, IDeviceSession;

    bool TryGet<TSession>(DeviceSessionId sessionId, out TSession? session)
        where TSession : class, IDeviceSession;

    bool Remove(DeviceSessionId sessionId);

    Task StopAllAsync(StopReason reason, CancellationToken cancellationToken = default);
}
