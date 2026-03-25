using System.Windows;
using ExperimentalControlPlatform.App.DevicePanels.HyperCam;
using ExperimentalControlPlatform.App.DevicePanels.Integrated;
using ExperimentalControlPlatform.App.DevicePanels.HuaTeng;
using ExperimentalControlPlatform.App.DevicePanels.Microphone;
using ExperimentalControlPlatform.App.DevicePanels.Pt104;
using ExperimentalControlPlatform.Devices.Audio;
using ExperimentalControlPlatform.Devices.Uvc;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var runtimeCoordinator = new RuntimeCoordinator();
        var hyperCamPanel = new HyperCamPanelViewModel();
        var mainViewModel = new MainViewModel(runtimeCoordinator, new[] { hyperCamPanel });
        var deviceTestWindow = new DeviceTestWindow(mainViewModel);

        MainWindow = deviceTestWindow;
        deviceTestWindow.Show();
    }
}
