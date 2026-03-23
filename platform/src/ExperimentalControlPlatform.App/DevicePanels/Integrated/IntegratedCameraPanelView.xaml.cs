using System.Windows;
using System.Windows.Controls;
using ExperimentalControlPlatform.App.Modals;

namespace ExperimentalControlPlatform.App.DevicePanels.Integrated;

public partial class IntegratedCameraPanelView : UserControl
{
    private bool _hasLoaded;

    public IntegratedCameraPanelView()
    {
        InitializeComponent();
    }

    private async void IntegratedCameraPanelView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded || DataContext is not IntegratedCameraPanelViewModel panel)
        {
            return;
        }

        _hasLoaded = true;
        await panel.RefreshCamerasAsync();
    }

    private async void ToggleConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IntegratedCameraPanelViewModel panel) return;
        if (panel.IsConnected) await panel.DisconnectAsync();
        else await panel.ConnectAsync();
    }

    private async void SnapFrameButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IntegratedCameraPanelViewModel panel) await panel.SnapFrameAsync();
    }

    private async void ToggleLiveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IntegratedCameraPanelViewModel panel) return;
        if (panel.IsLivePreviewing) await panel.StopLivePreviewAsync();
        else await panel.StartLivePreviewAsync();
    }

    private async void ApplySettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IntegratedCameraPanelViewModel panel)
        {
            await panel.ApplySettingsAsync();
        }
    }

    private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IntegratedCameraPanelViewModel panel) return;

        var diagnosticsDialog = new OrchestralModalWindow(
            "Device diagnostics",
            DiagnosticsDialogViewModel.FromSummary(
                "Integrated camera diagnostics",
                "Windows UVC camera state",
                panel.GetDiagnosticsSummary()))
        {
            Owner = Window.GetWindow(this)
        };

        diagnosticsDialog.ShowDialog();
    }
}
