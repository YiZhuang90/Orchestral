using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public sealed class RuntimeCoordinator : IRuntimeCoordinator
{
    private readonly IDeviceSessionRegistry _sessionRegistry;
    private readonly object _syncRoot = new();
    private RuntimeRunContext _latestSnapshot = new(Guid.Empty, RunState.Idle);
    private Task<RuntimeRunContext>? _activeStopTask;

    public RuntimeCoordinator(IDeviceSessionRegistry sessionRegistry)
    {
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
    }

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

    public async Task<RuntimeRunContext> RequestStopAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);

        lock (_syncRoot)
        {
            if (_latestSnapshot.State is RunState.Idle)
            {
                throw new InvalidOperationException("Runtime is not active and cannot be stopped.");
            }

            if (_latestSnapshot.State is RunState.Stopping)
            {
                throw new InvalidOperationException("Runtime stop is already in progress.");
            }
        }

        var stopTask = BeginStop(reason, cancellationToken);
        return await stopTask.ConfigureAwait(false);
    }

    public async Task<RuntimeRunContext> EnsureStoppedAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);

        Task<RuntimeRunContext>? existingStopTask = null;
        RuntimeRunContext? idleSnapshot = null;
        lock (_syncRoot)
        {
            if (_latestSnapshot.State is RunState.Idle)
            {
                idleSnapshot = _latestSnapshot;
            }
            else if (_latestSnapshot.State is RunState.Stopping)
            {
                existingStopTask = _activeStopTask;
                if (existingStopTask is null)
                {
                    throw new InvalidOperationException("Runtime stop is already in progress, but no active stop task is available.");
                }
            }
        }

        if (idleSnapshot is not null)
        {
            return idleSnapshot;
        }

        if (existingStopTask is not null)
        {
            return await existingStopTask.ConfigureAwait(false);
        }

        var stopTask = BeginStop(reason, cancellationToken);
        return await stopTask.ConfigureAwait(false);
    }

    private static bool IsActive(RunState state) =>
        state is RunState.Running or RunState.Stopping;

    private Task<RuntimeRunContext> BeginStop(StopReason reason, CancellationToken cancellationToken)
    {
        RuntimeRunContext stoppingSnapshot;

        lock (_syncRoot)
        {
            _latestSnapshot = new RuntimeRunContext(
                _latestSnapshot.RunId,
                RunState.Stopping,
                _latestSnapshot.StartedAtUtc,
                stoppedAtUtc: null,
                reason);
            stoppingSnapshot = _latestSnapshot;
            _activeStopTask = StopCoreAsync(stoppingSnapshot, reason, cancellationToken);
            return _activeStopTask;
        }
    }

    private async Task<RuntimeRunContext> StopCoreAsync(
        RuntimeRunContext stoppingSnapshot,
        StopReason reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sessionRegistry.StopAllAsync(reason, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            var stoppedAtUtc = DateTimeOffset.UtcNow;

            lock (_syncRoot)
            {
                _latestSnapshot = new RuntimeRunContext(
                    stoppingSnapshot.RunId,
                    RunState.Idle,
                    stoppingSnapshot.StartedAtUtc,
                    stoppedAtUtc,
                    reason);
                _activeStopTask = null;
            }
        }

        return LatestSnapshot;
    }
}
