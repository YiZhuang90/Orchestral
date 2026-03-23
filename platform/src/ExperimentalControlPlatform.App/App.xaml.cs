using System.Windows;
using ExperimentalControlPlatform.App.DevicePanels.Integrated;
using ExperimentalControlPlatform.App.DevicePanels.HuaTeng;
using ExperimentalControlPlatform.App.DevicePanels.Pt104;
using ExperimentalControlPlatform.Devices.Uvc;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var runtimeCoordinator = new RuntimeCoordinator();
        var integratedPanel = new IntegratedCameraPanelViewModel(
            new IntegratedCameraClient(new OpenCvUvcCameraService()));
        var mainViewModel = new MainViewModel(runtimeCoordinator, new[] { integratedPanel });
        var deviceTestWindow = new DeviceTestWindow(mainViewModel);

        MainWindow = deviceTestWindow;
        deviceTestWindow.Show();
    }
}
