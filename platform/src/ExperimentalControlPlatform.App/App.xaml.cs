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
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _sessionRegistry = new DeviceSessionRegistry();
        var runtimeCoordinator = new RuntimeCoordinator();
        var controlCenterPanel = new ControlCenterPanelViewModel(new SerialControlCenterService(), _sessionRegistry);
        _mainViewModel = new MainViewModel(runtimeCoordinator, new[] { controlCenterPanel });
        var deviceTestWindow = new DeviceTestWindow(_mainViewModel);

        MainWindow = deviceTestWindow;
        deviceTestWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _sessionRegistry?.StopAllAsync(StopReason.UserRequested("Application shutdown.")).GetAwaiter().GetResult();
        }
        finally
        {
            _mainViewModel?.Dispose();
            base.OnExit(e);
        }
    }
}
