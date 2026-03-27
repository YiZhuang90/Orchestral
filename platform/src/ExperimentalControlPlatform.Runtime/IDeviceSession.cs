using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public interface IDeviceSession : IAsyncDisposable
{
    DeviceSessionId SessionId { get; }

    ValueTask StopAsync(StopReason reason, CancellationToken cancellationToken = default);
}
