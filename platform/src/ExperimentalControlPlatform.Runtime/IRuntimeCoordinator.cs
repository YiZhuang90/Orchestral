namespace ExperimentalControlPlatform.Runtime;

public interface IRuntimeCoordinator
{
    RuntimeRunContext LatestSnapshot { get; }

    RuntimeRunContext Start();

    RuntimeRunContext RequestStop(StopReason reason);
}
