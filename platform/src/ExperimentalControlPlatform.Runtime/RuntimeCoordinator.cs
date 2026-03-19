using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed class RuntimeCoordinator : IRuntimeCoordinator
{
    private readonly object _syncRoot = new();
    private RuntimeRunContext _latestSnapshot = new(Guid.Empty, RunState.Idle);

    public RuntimeRunContext LatestSnapshot
    {
        get
        {
            lock (_syncRoot)
            {
                return _latestSnapshot;
            }
        }
    }

    public RuntimeRunContext Start()
    {
        lock (_syncRoot)
        {
            if (IsActive(_latestSnapshot.State))
            {
                throw new InvalidOperationException("Runtime is already active and cannot be started again.");
            }

            var startedAtUtc = DateTimeOffset.UtcNow;
            var runId = Guid.NewGuid();

            _latestSnapshot = new RuntimeRunContext(
                runId,
                RunState.Running,
                startedAtUtc: startedAtUtc);

            return _latestSnapshot;
        }
    }

    public RuntimeRunContext RequestStop(StopReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        lock (_syncRoot)
        {
            if (!IsActive(_latestSnapshot.State))
            {
                throw new InvalidOperationException("Runtime is not active and cannot be stopped.");
            }

            var stoppedAtUtc = DateTimeOffset.UtcNow;

            _latestSnapshot = new RuntimeRunContext(
                _latestSnapshot.RunId,
                RunState.Idle,
                _latestSnapshot.StartedAtUtc,
                stoppedAtUtc,
                reason);

            return _latestSnapshot;
        }
    }

    private static bool IsActive(RunState state) =>
        state is RunState.Running;
}
