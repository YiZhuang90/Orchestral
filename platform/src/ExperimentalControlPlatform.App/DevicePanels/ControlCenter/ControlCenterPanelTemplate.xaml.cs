using System.Windows;
using System.Windows.Controls;
using ExperimentalControlPlatform.App.Modals;

namespace ExperimentalControlPlatform.App.DevicePanels.ControlCenter;

public partial class ControlCenterPanelTemplate : UserControl
{
    public ControlCenterPanelTemplate()
    {
        InitializeComponent();
    }

    private async void ControlCenterPanelTemplate_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IControlCenterPanelViewModel panel)
        {
            await panel.RefreshDevicesAsync();
        }
    }

    private async void ToggleConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IControlCenterPanelViewModel panel) return;
        if (panel.IsConnected) await panel.DisconnectAsync();
        else await panel.ConnectAsync();
    }

    private async void ReadPulseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IControlCenterPanelViewModel panel)
        {
            await panel.ReadPulseCountAsync();
        }
    }

    private async void EmergencyStopButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IControlCenterPanelViewModel panel)
        {
            await panel.EmergencyStopAsync();
        }
    }

    private void ClearLogButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IControlCenterPanelViewModel panel)
        {
            panel.ClearLog();
        }
    }

    private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IControlCenterPanelViewModel panel) return;

        var diagnosticsDialog = new OrchestralModalWindow(
            "Device diagnostics",
            DiagnosticsDialogViewModel.FromSummary(
                panel.DiagnosticsTitle,
                panel.DiagnosticsSubtitle,
                panel.GetDiagnosticsSummary()))
        {
            Owner = Window.GetWindow(this)
        };

        diagnosticsDialog.ShowDialog();
    }
}
