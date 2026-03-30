using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ExperimentalControlPlatform.App.ExperimentMonitor;

public partial class ExperimentMonitorPanelTemplate : UserControl
{
    public ExperimentMonitorPanelTemplate()
    {
        InitializeComponent();
    }

    private async void InitializeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExperimentMonitorPanelViewModel viewModel)
        {
            await viewModel.InitializeAsync().ConfigureAwait(true);
        }
    }

    private async void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExperimentMonitorPanelViewModel viewModel)
        {
            await viewModel.StartAsync().ConfigureAwait(true);
        }
    }

    private async void StopButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExperimentMonitorPanelViewModel viewModel)
        {
            await viewModel.StopAsync().ConfigureAwait(true);
        }
    }
}
