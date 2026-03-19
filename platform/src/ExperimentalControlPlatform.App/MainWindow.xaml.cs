using System.Windows;

namespace ExperimentalControlPlatform.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StartRuntime();
    }

    private void StopButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.StopRuntime();
    }
}
