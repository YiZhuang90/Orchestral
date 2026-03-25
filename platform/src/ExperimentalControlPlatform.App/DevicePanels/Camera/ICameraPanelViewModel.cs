using System.Collections;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Modals;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.Camera;

public interface ICameraPanelViewModel : IDeviceTestPanelViewModel, IIntegrationPanelViewModel
{
    IEnumerable CameraOptions { get; }
    object? SelectedCameraItem { get; set; }

    string PrimaryValueLabel { get; }
    string CurrentPrimaryValue { get; }

    string FrameRateInputDraft { get; set; }
    IReadOnlyList<string> ColorOptions { get; }
    string SelectedColorOptionDraft { get; set; }

    Visibility ExposureControlVisibility { get; }
    string ExposureInputDraft { get; set; }
    Visibility AdvancedSettingsVisibility { get; }
    Visibility RoiToolbarVisibility { get; }

    ImageSource? PreviewImage { get; }
    ImageSource? BackgroundFrameImage { get; }
    string EmptyStateText { get; }

    string ResolutionText { get; }
    string PixelFormatText { get; }
    string ExposureText { get; }
    string FrameRateText { get; }

    string FooterConnectionLabel { get; }
    string FooterCameraLabel { get; }
    string FooterFormatLabel { get; }
    string FooterTriggerLabel { get; }
    string FooterSystemStateLabel { get; }

    bool IsConnected { get; }
    bool IsLivePreviewing { get; }
    string ConnectionToggleLabel { get; }
    PackIconMaterialKind ConnectionToggleIconKind { get; }
    string LiveToggleLabel { get; }
    PackIconMaterialKind LiveToggleIconKind { get; }
    bool CanToggleConnection { get; }
    bool CanSnapFrame { get; }
    bool CanToggleLive { get; }
    bool CanApplySettings { get; }

    string DiagnosticsTitle { get; }
    string DiagnosticsSubtitle { get; }
    string GetDiagnosticsSummary();

    bool CanSetRoi { get; }
    bool CanClearRoi { get; }
    Visibility RoiOverlayVisibility { get; }
    Visibility RoiHandleVisibility { get; }
    Visibility RoiLiveVisibility { get; }
    Rect RoiRectNormalized { get; set; }

    Task RefreshCamerasAsync();
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SnapFrameAsync();
    Task StartLivePreviewAsync();
    Task StopLivePreviewAsync();
    Task ApplySettingsAsync();

    CameraSettingsDialogViewModel? CreateDetailedSettingsDialog();
    Task ApplyDetailedSettingsAsync(CameraSettingsDialogViewModel dialog);
    Task BeginRoiEditAsync();
    void ClearRoi();
    Task ApplyRoiAsync();
}
