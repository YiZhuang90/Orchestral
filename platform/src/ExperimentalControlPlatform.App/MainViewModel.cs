using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public sealed class MainViewModel : IDisposable
{
    private readonly IRuntimeCoordinator _runtimeCoordinator;

    public MainViewModel(IRuntimeCoordinator runtimeCoordinator, IEnumerable<IDeviceTestPanelViewModel> devicePanels)
    {
        _runtimeCoordinator = runtimeCoordinator ?? throw new ArgumentNullException(nameof(runtimeCoordinator));
        ArgumentNullException.ThrowIfNull(devicePanels);
        RuntimeStatus = new RuntimeStatusViewModel(_runtimeCoordinator.LatestSnapshot);
        DevicePanels = new ReadOnlyCollection<IDeviceTestPanelViewModel>(new List<IDeviceTestPanelViewModel>(devicePanels));
    }

    public string ProductName => "Orchestral";

    public string HeaderTitle => DevicePanels.Count == 0
        ? ProductName
        : $"{ProductName} -- {string.Join(" / ", DevicePanels.Select(panel => panel.Title))}";

    public RuntimeStatusViewModel RuntimeStatus { get; }

    public IReadOnlyList<IDeviceTestPanelViewModel> DevicePanels { get; }

    public IDeviceTestPanelViewModel? CurrentDevicePanel => DevicePanels.FirstOrDefault();

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

    public void Dispose()
    {
        foreach (var panel in DevicePanels)
        {
            panel.Dispose();
        }
    }
}
