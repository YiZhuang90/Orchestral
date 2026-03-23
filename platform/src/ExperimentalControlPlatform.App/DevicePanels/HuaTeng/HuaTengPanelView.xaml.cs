using System.Windows;
using System.Windows.Controls;
using ExperimentalControlPlatform.App.Modals;
using ExperimentalControlPlatform.App.Widgets;

namespace ExperimentalControlPlatform.App.DevicePanels.HuaTeng;

public partial class HuaTengPanelView : UserControl
{
    private bool _hasLoaded;

    public HuaTengPanelView()
    {
        InitializeComponent();
    }

    private async void HuaTengPanelView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded || DataContext is not HuaTengPanelViewModel panel)
        {
            return;
        }

        _hasLoaded = true;
        await panel.RefreshCamerasAsync();
    }

    private async void ToggleConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HuaTengPanelViewModel panel) return;
        if (panel.IsConnected) await panel.DisconnectAsync();
        else await panel.ConnectAsync();
    }

    private async void SnapFrameButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is HuaTengPanelViewModel panel) await panel.SnapFrameAsync();
    }

    private async void ToggleLiveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HuaTengPanelViewModel panel) return;
        if (panel.IsLivePreviewing) await panel.StopLivePreviewAsync();
        else await panel.StartLivePreviewAsync();
    }

    private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HuaTengPanelViewModel panel) return;

        var diagnosticsDialog = new OrchestralModalWindow(
            "Device diagnostics",
            DiagnosticsDialogViewModel.FromSummary(
                "HuaTeng camera diagnostics",
                "USB camera state and frame pipeline",
                panel.GetDiagnosticsSummary()))
        {
            Owner = Window.GetWindow(this)
        };

        diagnosticsDialog.ShowDialog();
    }

    private async void ApplySettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is HuaTengPanelViewModel panel)
        {
            await panel.ApplySettingsAsync();
        }
    }

    private async void MoreSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HuaTengPanelViewModel panel) return;

        var settingsDialogViewModel = panel.CreateDetailedSettingsDialog();
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
        if (DataContext is HuaTengPanelViewModel panel)
        {
            await panel.BeginRoiEditAsync();
        }
    }

    private void ClearRoiButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is HuaTengPanelViewModel panel)
        {
            panel.ClearRoi();
        }
    }

    private async void CameraImageWindow_OnRoiEditStarted(object sender, System.EventArgs e)
    {
        if (DataContext is HuaTengPanelViewModel panel)
        {
            await panel.BeginRoiEditAsync();
        }
    }

    private async void CameraImageWindow_OnRoiEditCompleted(object sender, System.EventArgs e)
    {
        if (DataContext is HuaTengPanelViewModel panel)
        {
            await panel.ApplyRoiAsync();
        }
    }
}
