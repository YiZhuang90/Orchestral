using System.Windows;
using System.Windows.Controls;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Modals;

namespace ExperimentalControlPlatform.App.DevicePanels.Audio;

public partial class AudioInputPanelTemplate : UserControl
{
    public AudioInputPanelTemplate()
    {
        InitializeComponent();
    }

    private async void AudioInputPanelTemplate_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IAudioInputPanelViewModel panel)
        {
            await panel.RefreshDevicesAsync();
        }
    }

    private async void ToggleConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IAudioInputPanelViewModel panel) return;
        if (panel.IsConnected) await panel.DisconnectAsync();
        else await panel.ConnectAsync();
    }

    private async void ReadOnceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IAudioInputPanelViewModel panel) await panel.ReadOnceAsync();
    }

    private async void ToggleLiveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IAudioInputPanelViewModel panel) return;
        if (panel.IsLiveReading) panel.StopLiveRead();
        else await panel.StartLiveReadAsync();
    }

    private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IAudioInputPanelViewModel panel) return;

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
        if (DataContext is IAudioInputPanelViewModel panel) panel.ClearData();
    }

    private void ExportDataButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is IAudioInputPanelViewModel panel) panel.ExportCsv();
    }

    private async void OutputSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IOutputSettingsPanelViewModel panel)
        {
            return;
        }

        var dialog = new OrchestralModalWindow(
            "Output settings",
            new OutputSettingsDialogViewModel(panel.SupportedOutputPayloadTypes, panel.CurrentOutputSettings),
            "Apply")
        {
            Owner = Window.GetWindow(this)
        };

        dialog.ShowDialog();
        if (!dialog.PrimaryActionInvoked || dialog.ModalContent is not OutputSettingsDialogViewModel content)
        {
            return;
        }

        await panel.ApplyOutputSettingsAsync(content.BuildSettings());
    }
}
