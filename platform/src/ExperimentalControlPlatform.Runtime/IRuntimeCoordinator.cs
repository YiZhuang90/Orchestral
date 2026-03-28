using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public interface IRuntimeCoordinator
{
    RuntimeRunContext LatestSnapshot { get; }

    RuntimeRunContext Start();

    Task<RuntimeRunContext> RequestStopAsync(StopReason reason, CancellationToken cancellationToken = default);

    Task<RuntimeRunContext> EnsureStoppedAsync(StopReason reason, CancellationToken cancellationToken = default);
}
