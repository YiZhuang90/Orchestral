using System;
using System.IO;
using System.Windows;
using ExperimentalControlPlatform.App.DevicePanels.ControlCenter;
using ExperimentalControlPlatform.App.DevicePanels.HyperCam;
using ExperimentalControlPlatform.App.DevicePanels.Integrated;
using ExperimentalControlPlatform.App.DevicePanels.HuaTeng;
using ExperimentalControlPlatform.App.DevicePanels.Microphone;
using ExperimentalControlPlatform.App.DevicePanels.Pt104;
using ExperimentalControlPlatform.Devices.Audio;
using ExperimentalControlPlatform.Devices.ControlCenter;
using ExperimentalControlPlatform.Devices.Uvc;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public partial class App : Application
{
    private DeviceSessionRegistry? _sessionRegistry;
    private RuntimeCoordinator? _runtimeCoordinator;
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _sessionRegistry = new DeviceSessionRegistry();
        _runtimeCoordinator = new RuntimeCoordinator(_sessionRegistry);
        var controlCenterPanel = new ControlCenterPanelViewModel(new SerialControlCenterService(), _sessionRegistry);
        var runArtifactsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Orchestral",
            "runs");
        var runRecorder = new RunRecorder(runArtifactsRoot);
        _mainViewModel = new MainViewModel(_runtimeCoordinator, new[] { controlCenterPanel }, runRecorder);
        var deviceTestWindow = new DeviceTestWindow(_mainViewModel);

        MainWindow = deviceTestWindow;
        deviceTestWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_mainViewModel is not null
                && _runtimeCoordinator is not null
                && _runtimeCoordinator.LatestSnapshot.State is RunState.Running or RunState.Stopping)
            {
                _runtimeCoordinator.EnsureStoppedAsync(StopReason.UserRequested("Application shutdown.")).GetAwaiter().GetResult();
                _mainViewModel.FinalizeRunRecordingForLatestStoppedRun();
            }
            else
            {
                _sessionRegistry?.StopAllAsync(StopReason.UserRequested("Application shutdown.")).GetAwaiter().GetResult();
            }
        }
        finally
        {
            _mainViewModel?.Dispose();
            base.OnExit(e);
        }
    }
}
