using System;
using System.Windows;
using System.Windows.Controls;
using ExperimentalControlPlatform.App.Modals;

namespace ExperimentalControlPlatform.App.DevicePanels.Camera;

public partial class CameraPanelTemplate : UserControl
{
    private bool _hasLoaded;

    public CameraPanelTemplate()
    {
        InitializeComponent();
    }

    private async void CameraPanelTemplate_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded || DataContext is not ICameraPanelViewModel panel)
        {
            return;
        }

        _hasLoaded = true;
        await panel.RefreshCamerasAsync();
    }

    private async void ToggleConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ICameraPanelViewModel panel) return;
        if (panel.IsConnected) await panel.DisconnectAsync();
        else await panel.ConnectAsync();
    }

    private async void SnapFrameButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ICameraPanelViewModel panel) await panel.SnapFrameAsync();
    }

    private async void ToggleLiveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ICameraPanelViewModel panel) return;
        if (panel.IsLivePreviewing) await panel.StopLivePreviewAsync();
        else await panel.StartLivePreviewAsync();
    }

    private async void ApplySettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ICameraPanelViewModel panel)
        {
            await panel.ApplySettingsAsync();
        }
    }

    private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ICameraPanelViewModel panel) return;

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

    private async void MoreSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ICameraPanelViewModel panel) return;

        var settingsDialogViewModel = panel.CreateDetailedSettingsDialog();
        if (settingsDialogViewModel is null)
        {
            return;
        }

        var dialog = new OrchestralModalWindow(
            "Camera settings",
            settingsDialogViewModel,
            "Apply")
        {
            Owner = Window.GetWindow(this)
        };

        dialog.ShowDialog();
        if (dialog.PrimaryActionInvoked)
        {
            await panel.ApplyDetailedSettingsAsync(settingsDialogViewModel);
        }
    }

    private async void SetRoiButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ICameraPanelViewModel panel)
        {
            await panel.BeginRoiEditAsync();
        }
    }

    private void ClearRoiButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ICameraPanelViewModel panel)
        {
            panel.ClearRoi();
        }
    }

    private async void CameraImageWindow_OnRoiEditStarted(object sender, EventArgs e)
    {
        if (DataContext is ICameraPanelViewModel panel)
        {
            await panel.BeginRoiEditAsync();
        }
    }

    private async void CameraImageWindow_OnRoiEditCompleted(object sender, EventArgs e)
    {
        if (DataContext is ICameraPanelViewModel panel)
        {
            await panel.ApplyRoiAsync();
        }
    }
}
