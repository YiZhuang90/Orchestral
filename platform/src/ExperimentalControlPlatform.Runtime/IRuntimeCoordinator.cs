using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.Runtime;

public interface IRuntimeCoordinator
{
    RuntimeRunContext LatestSnapshot { get; }

    RuntimeRunContext Start();

    RuntimeRunContext Start(ResolvedExperimentDefinition experiment);

    RuntimeRunContext Start(RunContextDefinition runContext);

    Task<RuntimeRunContext> RequestStopAsync(StopReason reason, CancellationToken cancellationToken = default);

    Task<RuntimeRunContext> EnsureStoppedAsync(StopReason reason, CancellationToken cancellationToken = default);
}
