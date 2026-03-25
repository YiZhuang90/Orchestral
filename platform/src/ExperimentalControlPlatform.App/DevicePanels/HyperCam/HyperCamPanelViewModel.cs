using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Camera;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Modals;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.HyperCam;

public sealed class HyperCamPanelViewModel : ObservableObject, ICameraPanelViewModel
{
    private static readonly IReadOnlyList<IntegrationPanelLifecycleAction> ConnectedLifecycleActions =
    [
        IntegrationPanelLifecycleAction.Apply
    ];

    private readonly AsyncRelayCommand _lifecycleActionCommand;
    private readonly IReadOnlyList<HyperCamOption> _cameraOptions =
    [
        new("HyperCam")
    ];

    private HyperCamOption? _selectedCamera;
    private bool _isConnected;
    private bool _isLivePreviewing;
    private string _frameRateInput = "120";
    private string _draftFrameRateInput = "120";
    private string _selectedColorOption = "Mono";
    private string _draftSelectedColorOption = "Mono";
    private string _exposureInput = "850";
    private string _draftExposureInput = "850";
    private ImageSource? _previewImage;
    private string _currentPrimaryValue = "1280 x 720";
    private string _resolutionText = "1280 x 720";
    private string _pixelFormatText = "Mono12";
    private string _exposureText = "850 us";
    private string _frameRateText = "118.7 fps";
    private string _footerConnectionLabel = "Hardware: Disconnected";
    private string _footerCameraLabel = "HyperCam";
    private string _footerFormatLabel = "Mono";
    private string _footerTriggerLabel = "Continuous";
    private string _footerSystemStateLabel = "System Ready";
    private string _statusMessage = "Mock camera ready";
    private string? _lastCommand;
    private string? _lastHardwareResponse;
    private string? _lastError;
    private string? _lastStateTransition;
    private string? _lastValidationResult;
    private IntegrationPanelDataOutput? _dataOutput;
    private IntegrationPanelAppliedSettingsOutput? _appliedSettingsOutput;
    private IntegrationPanelStatusOutput? _statusOutput;
    private IntegrationPanelDiagnosticsOutput? _diagnosticsOutput;
    private IntegrationPanelSessionEndOutput? _sessionEndOutput;

    public HyperCamPanelViewModel()
    {
        _lifecycleActionCommand = new AsyncRelayCommand(ExecuteLifecycleActionAsync, CanExecuteLifecycleAction, HandleLifecycleCommandException);
        _selectedCamera = _cameraOptions[0];
        _previewImage = CreatePreviewImage(liveVariant: false);
        CaptureAppliedSettingsSnapshot("Mock HyperCam defaults loaded.");
        RefreshStatusOutput();
        RefreshDiagnosticsOutput();
        SyncFooter();
    }

    public string Title => "HyperCam";

    IEnumerable ICameraPanelViewModel.CameraOptions => CameraOptions;

    public IReadOnlyList<HyperCamOption> CameraOptions => _cameraOptions;

    public object? SelectedCameraItem
    {
        get => _selectedCamera;
        set
        {
            if (value is HyperCamOption option && SetProperty(ref _selectedCamera, option))
            {
                SyncFooter();
                RefreshStatusOutput();
            }
        }
    }

    public string PrimaryValueLabel => "Current frame: ";

    public string CurrentPrimaryValue
    {
        get => _currentPrimaryValue;
        private set => SetProperty(ref _currentPrimaryValue, value);
    }

    public string FrameRateInputDraft
    {
        get => _draftFrameRateInput;
        set
        {
            if (SetProperty(ref _draftFrameRateInput, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<string> ColorOptions { get; } = ["Mono", "Color"];

    public string SelectedColorOptionDraft
    {
        get => _draftSelectedColorOption;
        set
        {
            if (SetProperty(ref _draftSelectedColorOption, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public Visibility ExposureControlVisibility => Visibility.Visible;

    public string ExposureInputDraft
    {
        get => _draftExposureInput;
        set
        {
            if (SetProperty(ref _draftExposureInput, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public Visibility AdvancedSettingsVisibility => Visibility.Visible;

    public Visibility RoiToolbarVisibility => Visibility.Collapsed;

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        private set
        {
            if (SetProperty(ref _previewImage, value))
            {
                OnPropertyChanged(nameof(BackgroundFrameImage));
            }
        }
    }

    public ImageSource? BackgroundFrameImage => PreviewImage;

    public string EmptyStateText => _statusMessage;

    public string ResolutionText
    {
        get => _resolutionText;
        private set => SetProperty(ref _resolutionText, value);
    }

    public string PixelFormatText
    {
        get => _pixelFormatText;
        private set => SetProperty(ref _pixelFormatText, value);
    }

    public string ExposureText
    {
        get => _exposureText;
        private set => SetProperty(ref _exposureText, value);
    }

    public string FrameRateText
    {
        get => _frameRateText;
        private set => SetProperty(ref _frameRateText, value);
    }

    public string FooterConnectionLabel
    {
        get => _footerConnectionLabel;
        private set => SetProperty(ref _footerConnectionLabel, value);
    }

    public string FooterCameraLabel
    {
        get => _footerCameraLabel;
        private set => SetProperty(ref _footerCameraLabel, value);
    }

    public string FooterFormatLabel
    {
        get => _footerFormatLabel;
        private set => SetProperty(ref _footerFormatLabel, value);
    }

    public string FooterTriggerLabel
    {
        get => _footerTriggerLabel;
        private set => SetProperty(ref _footerTriggerLabel, value);
    }

    public string FooterSystemStateLabel
    {
        get => _footerSystemStateLabel;
        private set => SetProperty(ref _footerSystemStateLabel, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(ConnectionToggleLabel));
                OnPropertyChanged(nameof(ConnectionToggleIconKind));
                OnPropertyChanged(nameof(CanToggleConnection));
                OnPropertyChanged(nameof(CanSnapFrame));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(SupportedLifecycleActions));
                OnPropertyChanged(nameof(CanApplySettings));
                RefreshStatusOutput();
                _lifecycleActionCommand.NotifyCanExecuteChanged();
                SyncFooter();
            }
        }
    }

    public bool IsLivePreviewing
    {
        get => _isLivePreviewing;
        private set
        {
            if (SetProperty(ref _isLivePreviewing, value))
            {
                OnPropertyChanged(nameof(LiveToggleLabel));
                OnPropertyChanged(nameof(LiveToggleIconKind));
                OnPropertyChanged(nameof(CanSnapFrame));
                OnPropertyChanged(nameof(CanToggleLive));
                RefreshStatusOutput();
                SyncFooter();
            }
        }
    }

    public string ConnectionToggleLabel => IsConnected ? "Disconnect" : "Connect";

    public PackIconMaterialKind ConnectionToggleIconKind => IsConnected
        ? PackIconMaterialKind.LinkOff
        : PackIconMaterialKind.Link;

    public string LiveToggleLabel => IsLivePreviewing ? "Stop live" : "Start live";

    public PackIconMaterialKind LiveToggleIconKind => IsLivePreviewing
        ? PackIconMaterialKind.Stop
        : PackIconMaterialKind.Play;

    public bool CanToggleConnection => !IsLivePreviewing;

    public bool CanSnapFrame => IsConnected && !IsLivePreviewing;

    public bool CanToggleLive => IsConnected;

    public bool CanApplySettings =>
        IsConnected &&
        (!string.Equals(_frameRateInput, _draftFrameRateInput, StringComparison.Ordinal) ||
         !string.Equals(_selectedColorOption, _draftSelectedColorOption, StringComparison.Ordinal) ||
         !string.Equals(_exposureInput, _draftExposureInput, StringComparison.Ordinal));

    public string DiagnosticsTitle => "HyperCam diagnostics";

    public string DiagnosticsSubtitle => "Mock camera panel for design review";

    public bool CanSetRoi => false;

    public bool CanClearRoi => false;

    public Visibility RoiOverlayVisibility => Visibility.Collapsed;

    public Visibility RoiHandleVisibility => Visibility.Collapsed;

    public Visibility RoiLiveVisibility => Visibility.Collapsed;

    public Rect RoiRectNormalized
    {
        get => Rect.Empty;
        set { }
    }

    public IntegrationPanelDataOutput? DataOutput
    {
        get => _dataOutput;
        private set => SetProperty(ref _dataOutput, value);
    }

    public IntegrationPanelAppliedSettingsOutput? AppliedSettingsOutput
    {
        get => _appliedSettingsOutput;
        private set => SetProperty(ref _appliedSettingsOutput, value);
    }

    public IntegrationPanelStatusOutput? StatusOutput
    {
        get => _statusOutput;
        private set => SetProperty(ref _statusOutput, value);
    }

    public IntegrationPanelDiagnosticsOutput? DiagnosticsOutput
    {
        get => _diagnosticsOutput;
        private set => SetProperty(ref _diagnosticsOutput, value);
    }

    public IntegrationPanelSessionEndOutput? SessionEndOutput
    {
        get => _sessionEndOutput;
        private set => SetProperty(ref _sessionEndOutput, value);
    }

    public IReadOnlyList<IntegrationPanelLifecycleAction> SupportedLifecycleActions => IsConnected ? ConnectedLifecycleActions : [];

    public ICommand LifecycleActionCommand => _lifecycleActionCommand;

    public Task RefreshCamerasAsync()
    {
        SetLastCommand("List mock cameras");
        SetLastHardwareResponse("HyperCam visual mock ready.");
        return Task.CompletedTask;
    }

    public Task ConnectAsync()
    {
        IsConnected = true;
        SetLastCommand("Connect HyperCam");
        SetLastHardwareResponse("Mock connection established.");
        SetLastStateTransition("Connected HyperCam");
        _statusMessage = "HyperCam connected.";
        SessionEndOutput = null;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        var wasLive = IsLivePreviewing;
        IsLivePreviewing = false;
        IsConnected = false;
        SetLastCommand("Disconnect HyperCam");
        SetLastHardwareResponse("Mock connection closed.");
        SetLastStateTransition("Disconnected HyperCam");
        _statusMessage = "HyperCam disconnected.";
        SessionEndOutput = new IntegrationPanelSessionEndOutput
        {
            EndedAt = DateTimeOffset.Now,
            ExitReason = "Closed mock camera session",
            ConnectionClosed = true,
            LiveStopped = wasLive,
            AppliedSettingsSnapshot = AppliedSettingsOutput,
            FinalStatus = BuildStatusOutput()
        };
        return Task.CompletedTask;
    }

    public Task SnapFrameAsync()
    {
        PreviewImage = CreatePreviewImage(liveVariant: false);
        SetLastCommand("Snap HyperCam frame");
        SetLastHardwareResponse("Mock frame captured.");
        SetLastStateTransition("Captured mock frame");
        _statusMessage = "Mock frame captured.";
        UpdateFrameCards();
        return Task.CompletedTask;
    }

    public Task StartLivePreviewAsync()
    {
        IsLivePreviewing = true;
        PreviewImage = CreatePreviewImage(liveVariant: true);
        FrameRateText = "120.0 fps";
        SetLastCommand("Start HyperCam live");
        SetLastHardwareResponse("Mock live preview started.");
        SetLastStateTransition("Started mock live preview");
        _statusMessage = "Mock live preview running.";
        return Task.CompletedTask;
    }

    public Task StopLivePreviewAsync()
    {
        IsLivePreviewing = false;
        PreviewImage = CreatePreviewImage(liveVariant: false);
        FrameRateText = "118.7 fps";
        SetLastCommand("Stop HyperCam live");
        SetLastHardwareResponse("Mock live preview stopped.");
        SetLastStateTransition("Stopped mock live preview");
        _statusMessage = "Mock live preview stopped.";
        return Task.CompletedTask;
    }

    public Task ApplySettingsAsync()
    {
        _frameRateInput = _draftFrameRateInput;
        _selectedColorOption = _draftSelectedColorOption;
        _exposureInput = _draftExposureInput;
        ExposureText = $"{_exposureInput} us";
        PixelFormatText = _selectedColorOption == "Color" ? "RGB24" : "Mono12";
        FooterFormatLabel = _selectedColorOption;
        FrameRateText = $"{ParsePositiveDouble(_frameRateInput, 120):0.0} fps";
        SetLastCommand("Apply HyperCam settings");
        SetLastHardwareResponse("Mock settings applied.");
        SetLastStateTransition("Applied mock camera settings");
        _statusMessage = "Mock settings applied.";
        CaptureAppliedSettingsSnapshot("Applied to HyperCam mock panel.");
        return Task.CompletedTask;
    }

    public CameraSettingsDialogViewModel? CreateDetailedSettingsDialog()
    {
        return new CameraSettingsDialogViewModel(
            "Visual-only mock camera controls.",
            ["Continuous", "Triggered"],
            "Continuous",
            ["Neutral", "Warm", "Cool"],
            "Neutral");
    }

    public Task ApplyDetailedSettingsAsync(CameraSettingsDialogViewModel dialog)
    {
        FooterTriggerLabel = dialog.SelectedTriggerMode;
        SetLastCommand("Apply HyperCam detailed settings");
        SetLastHardwareResponse("Mock advanced settings applied.");
        SetLastStateTransition("Applied mock advanced settings");
        _statusMessage = "Mock advanced settings applied.";
        return Task.CompletedTask;
    }

    public Task BeginRoiEditAsync() => Task.CompletedTask;

    public void ClearRoi()
    {
    }

    public Task ApplyRoiAsync() => Task.CompletedTask;

    public string GetDiagnosticsSummary()
    {
        return string.Join(Environment.NewLine, new[]
        {
            $"Device: {_selectedCamera?.DisplayName ?? "HyperCam"}",
            $"Connection: {(IsConnected ? "Connected" : "Disconnected")}",
            $"Frame rate: {_frameRateInput} fps",
            $"Exposure: {_exposureInput} us",
            $"Color: {_selectedColorOption}",
            $"Trigger: {FooterTriggerLabel}",
            $"Status: {_statusMessage}"
        });
    }

    public void Dispose()
    {
    }

    private bool CanExecuteLifecycleAction(object? parameter)
    {
        return parameter is IntegrationPanelLifecycleAction.Apply && CanApplySettings;
    }

    private async Task ExecuteLifecycleActionAsync(object? parameter)
    {
        if (parameter is not IntegrationPanelLifecycleAction.Apply)
        {
            return;
        }

        await ApplySettingsAsync().ConfigureAwait(false);
    }

    private void HandleLifecycleCommandException(Exception exception)
    {
        SetLastError(exception.Message);
        _statusMessage = exception.Message;
    }

    private void UpdateFrameCards()
    {
        CurrentPrimaryValue = "1280 x 720";
        ResolutionText = "1280 x 720";
        DataOutput = new IntegrationPanelDataOutput
        {
            Timestamp = DateTimeOffset.Now,
            EndpointId = _selectedCamera?.DisplayName ?? "HyperCam",
            PayloadType = "MockCameraFrame",
            PayloadValue = $"{ResolutionText}; {PixelFormatText}",
            Units = "pixels",
            SequenceNumber = 1,
            CaptureRate = ParsePositiveDouble(_frameRateInput, 120),
            SourceMode = IsLivePreviewing ? "live" : "snapshot"
        };
    }

    private void SyncFooter()
    {
        FooterConnectionLabel = IsConnected ? "Hardware: Connected" : "Hardware: Disconnected";
        FooterCameraLabel = _selectedCamera?.DisplayName ?? "HyperCam";
        FooterFormatLabel = _selectedColorOption;
        FooterSystemStateLabel = IsLivePreviewing ? "Streaming" : "System Ready";
    }

    private void CaptureAppliedSettingsSnapshot(string note)
    {
        _lastValidationResult = "Mock settings are valid.";
        AppliedSettingsOutput = new IntegrationPanelAppliedSettingsOutput
        {
            AppliedAt = DateTimeOffset.Now,
            DeviceSettings = new Dictionary<string, string?>
            {
                ["Device"] = Title,
                ["Camera"] = _selectedCamera?.DisplayName ?? "HyperCam"
            },
            EndpointSettings = new Dictionary<string, string?>
            {
                ["FrameRateFps"] = _frameRateInput,
                ["ExposureUs"] = _exposureInput,
                ["ColorMode"] = _selectedColorOption
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["Connected"] = IsConnected ? "true" : "false",
                ["LivePreviewing"] = IsLivePreviewing ? "true" : "false"
            },
            NormalizationNotes = [note]
        };
    }

    private IntegrationPanelStatusOutput BuildStatusOutput()
    {
        return new IntegrationPanelStatusOutput
        {
            Connected = IsConnected,
            ReadyState = IsConnected ? "Ready" : "Disconnected",
            FaultState = _lastError,
            LiveState = IsLivePreviewing ? "Streaming" : "Stopped",
            SelectedEndpoint = _selectedCamera?.DisplayName ?? "HyperCam",
            BackgroundActiveEndpoints = []
        };
    }

    private void RefreshStatusOutput()
    {
        StatusOutput = BuildStatusOutput();
    }

    private void RefreshDiagnosticsOutput()
    {
        DiagnosticsOutput = new IntegrationPanelDiagnosticsOutput
        {
            LastCommand = _lastCommand,
            LastHardwareResponse = _lastHardwareResponse,
            LastError = _lastError,
            LastStateTransition = _lastStateTransition,
            LastValidationResult = _lastValidationResult
        };
    }

    private void SetLastCommand(string value)
    {
        _lastCommand = value;
        RefreshDiagnosticsOutput();
    }

    private void SetLastHardwareResponse(string value)
    {
        _lastHardwareResponse = value;
        RefreshDiagnosticsOutput();
    }

    private void SetLastError(string value)
    {
        _lastError = value;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private void SetLastStateTransition(string value)
    {
        _lastStateTransition = value;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private static double ParsePositiveDouble(string input, double fallback)
    {
        return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }

    private static ImageSource CreatePreviewImage(bool liveVariant)
    {
        const int width = 1280;
        const int height = 720;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var background = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(liveVariant ? "#F2F0EA" : "#F7F6F2"),
                (Color)ColorConverter.ConvertFromString(liveVariant ? "#D8D5CC" : "#E4E0D7"),
                new Point(0, 0),
                new Point(1, 1));
            context.DrawRectangle(background, null, new Rect(0, 0, width, height));

            var framePen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5F5E5E")), 4);
            context.DrawRectangle(null, framePen, new Rect(44, 44, width - 88, height - 88));

            context.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8C2B8")), null, new Rect(96, 110, 520, 316));
            context.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString(liveVariant ? "#9A462A" : "#7B7269")), null, new Rect(664, 118, 200, 150));
            context.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6A7170")), null, new Rect(698, 302, 284, 214));

            var rustBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9A462A"));
            context.DrawLine(new Pen(rustBrush, 8), new Point(118, 560), new Point(1090, 560));
            context.DrawLine(new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5F5E5E")), 2), new Point(118, 610), new Point(1090, 610));

            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
            var title = new FormattedText(
                "HYPERCAM LIVE VIEW",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                44,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E3430")),
                1.25);
            context.DrawText(title, new Point(118, 86));

            var labelTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var label = new FormattedText(
                liveVariant ? "Streaming diagnostic pattern" : "Static diagnostic pattern",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                labelTypeface,
                28,
                rustBrush,
                1.25);
            context.DrawText(label, new Point(118, 636));
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
