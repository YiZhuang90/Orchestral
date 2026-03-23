using System.Windows;
using System.Windows.Controls;
using ExperimentalControlPlatform.App.Modals;

namespace ExperimentalControlPlatform.App.DevicePanels.Pt104;

public partial class Pt104PanelView : UserControl
{
    public Pt104PanelView()
    {
        InitializeComponent();
    }

    private async void ToggleConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Pt104PanelViewModel panel) return;
        if (panel.IsConnected) await panel.DisconnectAsync();
        else await panel.ConnectAsync();
    }

    private async void ReadOnceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is Pt104PanelViewModel panel) await panel.ReadOnceAsync();
    }

    private async void ToggleLiveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Pt104PanelViewModel panel) return;
        if (panel.IsLiveReading) panel.StopLiveRead();
        else await panel.StartLiveReadAsync();
    }

    private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Pt104PanelViewModel panel) return;

        var diagnosticsDialog = new OrchestralModalWindow(
            "Device diagnostics",
            DiagnosticsDialogViewModel.FromSummary(
                "PT-104 diagnostics",
                "Hardware state and channel configuration",
                panel.GetDiagnosticsSummary()))
        {
            Owner = Window.GetWindow(this)
        };

        diagnosticsDialog.ShowDialog();
    }

    private void ClearDataButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is Pt104PanelViewModel panel) panel.ClearData();
    }

    private void ExportDataButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is Pt104PanelViewModel panel) panel.ExportCsv();
    }
}
