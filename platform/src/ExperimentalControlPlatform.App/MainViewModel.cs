using System;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public sealed class MainViewModel
{
    private readonly IRuntimeCoordinator _runtimeCoordinator;

    public MainViewModel(IRuntimeCoordinator runtimeCoordinator)
    {
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        RuntimeStatus = new RuntimeStatusViewModel(_runtimeCoordinator.LatestSnapshot);
    }

    public string ProductName => "Orchestral";

    public RuntimeStatusViewModel RuntimeStatus { get; }

    public void StartRuntime()
    {
        try
        {
            RuntimeStatus.Update(_runtimeCoordinator.Start());
        }
        catch (Exception ex)
        {
            RuntimeStatus.ShowOperationError("Unable to start runtime.", ex.Message);
        }
    }

    public void StopRuntime()
    {
        try
        {
            RuntimeStatus.Update(_runtimeCoordinator.RequestStop(StopReason.UserRequested("Stopped from app shell placeholder control.")));
        }
        catch (Exception ex)
        {
            RuntimeStatus.ShowOperationError("Unable to stop runtime.", ex.Message);
        }
    }
}
