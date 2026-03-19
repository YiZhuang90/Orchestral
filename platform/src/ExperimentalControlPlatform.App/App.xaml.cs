using System.Windows;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var runtimeCoordinator = new RuntimeCoordinator();
        var mainViewModel = new MainViewModel(runtimeCoordinator);
        var mainWindow = new MainWindow(mainViewModel);

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
