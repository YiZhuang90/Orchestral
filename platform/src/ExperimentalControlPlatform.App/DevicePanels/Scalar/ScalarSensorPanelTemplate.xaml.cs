using System.Windows;
using System.Windows.Controls;
using ExperimentalControlPlatform.App.Modals;

namespace ExperimentalControlPlatform.App.DevicePanels.Scalar;

public partial class ScalarSensorPanelTemplate : UserControl
{
    public ScalarSensorPanelTemplate()
    {
        InitializeComponent();
    }

    private async void ToggleConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IScalarSensorPanelViewModel panel) return;
        if (panel.IsConnected) await panel.DisconnectAsync();
        else await panel.ConnectAsync();
    }

    private async void ReadOnceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IScalarSensorPanelViewModel panel) await panel.ReadOnceAsync();
    }

    private async void ToggleLiveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IScalarSensorPanelViewModel panel) return;
        if (panel.IsLiveReading) panel.StopLiveRead();
        else await panel.StartLiveReadAsync();
    }

    private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IScalarSensorPanelViewModel panel) return;

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

    private void ClearDataButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IScalarSensorPanelViewModel panel) panel.ClearData();
    }

    private void ExportDataButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IScalarSensorPanelViewModel panel) panel.ExportCsv();
    }
}
