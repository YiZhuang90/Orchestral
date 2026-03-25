using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Audio;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Widgets;
using ExperimentalControlPlatform.Devices.Audio;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;

namespace ExperimentalControlPlatform.App.DevicePanels.Microphone;

public sealed class IntegratedMicrophonePanelViewModel : ObservableObject, IAudioInputPanelViewModel
{
    private const double PlotCanvasWidth = 504;
    private const double PlotCanvasHeight = 280;
    private const int PlotPointCount = 180;
    private static readonly IReadOnlyList<IntegrationPanelLifecycleAction> ConnectedLifecycleActions =
    [
        IntegrationPanelLifecycleAction.Apply
    ];

    private readonly IntegratedMicrophoneClient _client;
    private readonly AsyncRelayCommand _lifecycleActionCommand;
    private readonly ValueCardItem _peakCard = new("Peak", "--");
    private readonly ValueCardItem _sampleRateCard = new("Sample rate", "--");
    private readonly ValueCardItem _windowCard = new("Window", "--");
    private readonly ValueCardItem _clippingCard = new("Clipping", "No");
    private CancellationTokenSource? _liveReadCancellation;
    private Task? _liveReadTask;
    private bool _isBusy;
    private bool _isConnected;
    private bool _isLiveReading;
    private IReadOnlyList<MicrophoneDeviceInfo> _deviceOptions = [];
    private MicrophoneDeviceInfo? _selectedDevice;
    private string _targetUpdateRateInput = "20";
    private string _draftTargetUpdateRateInput = "20";
    private string _windowMillisecondsInput = "50";
    private string _draftWindowMillisecondsInput = "50";
    private string _selectedChannelModeLabel = "Mono mix";
    private string _draftSelectedChannelModeLabel = "Mono mix";
    private string _currentPrimaryValue = "-inf dBFS";
    private string _footerConnectionLabel = "Hardware: Disconnected";
    private string _footerDeviceLabel = "No microphone";
    private string _footerFormatLabel = "--";
    private string _footerModeLabel = "Mono mix";
    private string _footerSystemStateLabel = "System Ready";
    private string _statusMessage = "Ready to discover microphone";
    private string _xAxisStartLabel = "0 ms";
    private string _xAxisMidLeftLabel = "12.5 ms";
    private string _xAxisMidRightLabel = "37.5 ms";
    private string _xAxisEndLabel = "50 ms";
    private PointCollection _plotPoints = [];
    private PointCollection _plotFillPoints = [];
    private float[] _latestWaveformSamples = [];
    private IntegrationPanelDataOutput? _dataOutput;
    private IntegrationPanelAppliedSettingsOutput? _appliedSettingsOutput;
    private IntegrationPanelStatusOutput? _statusOutput;
    private IntegrationPanelDiagnosticsOutput? _diagnosticsOutput;
    private IntegrationPanelSessionEndOutput? _sessionEndOutput;
    private string? _lastCommand;
    private string? _lastHardwareResponse;
    private string? _lastError;
    private string? _lastStateTransition;
    private string? _lastValidationResult;
    private DateTimeOffset? _lastFrameCapturedAt;
    private long _frameSequence;
    private string? _lastSourceMode;

    public IntegratedMicrophonePanelViewModel(IntegratedMicrophoneClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _lifecycleActionCommand = new AsyncRelayCommand(ExecuteLifecycleActionAsync, CanExecuteLifecycleAction, HandleLifecycleCommandException);
        StatisticsCards = new[] { _peakCard, _sampleRateCard, _windowCard, _clippingCard };
        SyncXAxisLabels(ParseWindowMilliseconds(_windowMillisecondsInput));
        SyncFooter();
        RefreshStatusOutput();
        RefreshDiagnosticsOutput();
    }

    public string Title => "Integrated Microphone";

    IEnumerable IAudioInputPanelViewModel.DeviceOptions => DeviceOptions;

    public IReadOnlyList<MicrophoneDeviceInfo> DeviceOptions
    {
        get => _deviceOptions;
        private set
        {
            _deviceOptions = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedDeviceItem));
        }
    }

    public MicrophoneDeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                OnPropertyChanged(nameof(SelectedDeviceItem));
                OnPropertyChanged(nameof(CanToggleConnection));
                SyncFooter();
                RefreshStatusOutput();
            }
        }
    }

    public object? SelectedDeviceItem
    {
        get => SelectedDevice;
        set => SelectedDevice = value as MicrophoneDeviceInfo;
    }

    public IReadOnlyList<string> ChannelModeOptions { get; } = ["Mono mix", "Left", "Right"];

    IEnumerable IAudioInputPanelViewModel.ChannelModeOptions => ChannelModeOptions;

    public object? SelectedChannelModeItem
    {
        get => SelectedChannelModeLabelDraft;
        set
        {
            if (value is string label)
            {
                SelectedChannelModeLabelDraft = label;
            }
        }
    }

    public string SelectedChannelModeLabelDraft
    {
        get => _draftSelectedChannelModeLabel;
        set
        {
            if (SetProperty(ref _draftSelectedChannelModeLabel, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string TargetUpdateRateInputDraft
    {
        get => _draftTargetUpdateRateInput;
        set
        {
            if (SetProperty(ref _draftTargetUpdateRateInput, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string WindowMillisecondsInputDraft
    {
        get => _draftWindowMillisecondsInput;
        set
        {
            if (SetProperty(ref _draftWindowMillisecondsInput, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string PrimaryValueLabel => "RMS amplitude: ";

    public string CurrentPrimaryValue
    {
        get => _currentPrimaryValue;
        private set => SetProperty(ref _currentPrimaryValue, value);
    }

    public IReadOnlyList<ValueCardItem> StatisticsCards { get; }

    public double PlotWidth => PlotCanvasWidth;

    public double PlotHeight => PlotCanvasHeight;

    public PointCollection PlotPoints
    {
        get => _plotPoints;
        private set => SetProperty(ref _plotPoints, value);
    }

    public PointCollection PlotFillPoints
    {
        get => _plotFillPoints;
        private set => SetProperty(ref _plotFillPoints, value);
    }

    public string PlotMinLabel => "-1.0";

    public string PlotLowerMidLabel => "-0.5";

    public string PlotMidLabel => "0";

    public string PlotUpperMidLabel => "0.5";

    public string PlotMaxLabel => "1.0";

    public string XAxisStartLabel
    {
        get => _xAxisStartLabel;
        private set => SetProperty(ref _xAxisStartLabel, value);
    }

    public string XAxisMidLeftLabel
    {
        get => _xAxisMidLeftLabel;
        private set => SetProperty(ref _xAxisMidLeftLabel, value);
    }

    public string XAxisMidRightLabel
    {
        get => _xAxisMidRightLabel;
        private set => SetProperty(ref _xAxisMidRightLabel, value);
    }

    public string XAxisEndLabel
    {
        get => _xAxisEndLabel;
        private set => SetProperty(ref _xAxisEndLabel, value);
    }

    public string FooterConnectionLabel
    {
        get => _footerConnectionLabel;
        private set => SetProperty(ref _footerConnectionLabel, value);
    }

    public string FooterDeviceLabel
    {
        get => _footerDeviceLabel;
        private set => SetProperty(ref _footerDeviceLabel, value);
    }

    public string FooterFormatLabel
    {
        get => _footerFormatLabel;
        private set => SetProperty(ref _footerFormatLabel, value);
    }

    public string FooterModeLabel
    {
        get => _footerModeLabel;
        private set => SetProperty(ref _footerModeLabel, value);
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
                OnPropertyChanged(nameof(CanReadOnce));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(SupportedLifecycleActions));
                RefreshStatusOutput();
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLiveReading
    {
        get => _isLiveReading;
        private set
        {
            if (SetProperty(ref _isLiveReading, value))
            {
                OnPropertyChanged(nameof(LiveToggleLabel));
                OnPropertyChanged(nameof(LiveToggleIconKind));
                OnPropertyChanged(nameof(CanReadOnce));
                OnPropertyChanged(nameof(CanToggleLive));
                RefreshStatusOutput();
            }
        }
    }

    public bool CanToggleConnection => SelectedDevice is not null && !_isBusy && !IsLiveReading;

    public bool CanReadOnce => IsConnected && !_isBusy && !IsLiveReading;

    public bool CanToggleLive => IsConnected && !_isBusy;

    public bool CanClearData => _latestWaveformSamples.Length > 0 && !_isBusy;

    public bool CanExportData => _latestWaveformSamples.Length > 0 && !_isBusy;

    public bool CanApplySettings => !_isBusy &&
                                    (!string.Equals(_targetUpdateRateInput, _draftTargetUpdateRateInput, StringComparison.Ordinal) ||
                                     !string.Equals(_windowMillisecondsInput, _draftWindowMillisecondsInput, StringComparison.Ordinal) ||
                                     !string.Equals(_selectedChannelModeLabel, _draftSelectedChannelModeLabel, StringComparison.Ordinal));

    public string ConnectionToggleLabel => IsConnected ? "Disconnect" : "Connect";

    public PackIconMaterialKind ConnectionToggleIconKind => IsConnected
        ? PackIconMaterialKind.LinkOff
        : PackIconMaterialKind.Link;

    public string LiveToggleLabel => IsLiveReading ? "Stop live" : "Start live";

    public PackIconMaterialKind LiveToggleIconKind => IsLiveReading
        ? PackIconMaterialKind.Stop
        : PackIconMaterialKind.Play;

    public string DiagnosticsTitle => "Integrated microphone diagnostics";

    public string DiagnosticsSubtitle => "Windows audio-input capture state";

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

    public async Task RefreshDevicesAsync()
    {
        var devices = await Task.Run(() => _client.ListDevices()).ConfigureAwait(true);
        DeviceOptions = devices;
        SelectedDevice ??= DeviceOptions.FirstOrDefault();
        SetLastCommand("List microphones");
        SetLastHardwareResponse(SelectedDevice is null ? "No active microphone found." : "Integrated microphone ready.");
        _statusMessage = SelectedDevice is null ? "No active microphone found." : "Integrated microphone ready.";
        SyncFooter();
        OnPropertyChanged(nameof(CanToggleConnection));
        await Task.CompletedTask;
    }

    public async Task ConnectAsync()
    {
        if (SelectedDevice is null)
        {
            SetLastError("No microphone selected.");
            _statusMessage = "No microphone selected.";
            return;
        }

        _isBusy = true;
        RaiseCommandState();
        try
        {
            SetLastCommand("Connect integrated microphone");
            var deviceId = SelectedDevice.DeviceId;
            var targetUpdateRate = ParseTargetUpdateRate();
            var windowMilliseconds = ParseWindowMilliseconds(_windowMillisecondsInput);
            var channelMode = ParseChannelMode(_selectedChannelModeLabel);
            var frame = await Task.Run(
                () => _client.CaptureSnapshot(deviceId, targetUpdateRate, windowMilliseconds, channelMode))
                .ConfigureAwait(true);
            IsConnected = true;
            SessionEndOutput = null;
            ClearLastError();
            SetLastHardwareResponse("Integrated microphone connection probe succeeded.");
            SetLastStateTransition($"Connected to {SelectedDevice.DisplayName}");
            ApplyFrame(frame, "connect");
            _statusMessage = $"Connected to {SelectedDevice.DisplayName}.";
            SyncFooter();
        }
        catch (Exception ex)
        {
            SetLastError(ex.Message);
            _statusMessage = $"Unable to connect microphone: {ex.Message}";
        }
        finally
        {
            _isBusy = false;
            RaiseCommandState();
        }

        await Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        var liveWasActive = IsLiveReading;
        StopLiveRead();
        if (_liveReadTask is not null)
        {
            try
            {
                await _liveReadTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        IsConnected = false;
        _statusMessage = "Disconnected.";
        SyncFooter();
        SetLastStateTransition("Disconnected microphone");
        SessionEndOutput = new IntegrationPanelSessionEndOutput
        {
            EndedAt = DateTimeOffset.Now,
            ExitReason = "Disconnected by operator",
            ConnectionClosed = true,
            LiveStopped = liveWasActive,
            AppliedSettingsSnapshot = AppliedSettingsOutput,
            FinalStatus = BuildStatusOutput()
        };
    }

    public async Task ReadOnceAsync()
    {
        if (SelectedDevice is null)
        {
            return;
        }

        _isBusy = true;
        RaiseCommandState();
        try
        {
            SetLastCommand("Capture microphone snapshot");
            var deviceId = SelectedDevice.DeviceId;
            var targetUpdateRate = ParseTargetUpdateRate();
            var windowMilliseconds = ParseWindowMilliseconds(_windowMillisecondsInput);
            var channelMode = ParseChannelMode(_selectedChannelModeLabel);
            var frame = await Task.Run(
                () => _client.CaptureSnapshot(deviceId, targetUpdateRate, windowMilliseconds, channelMode))
                .ConfigureAwait(true);
            ClearLastError();
            SetLastHardwareResponse("Microphone snapshot captured.");
            SetLastStateTransition("Captured microphone snapshot");
            ApplyFrame(frame, "snapshot");
            _statusMessage = "Microphone snapshot captured.";
        }
        catch (Exception ex)
        {
            SetLastError(ex.Message);
            _statusMessage = $"Snapshot failed: {ex.Message}";
        }
        finally
        {
            _isBusy = false;
            RaiseCommandState();
        }

        await Task.CompletedTask;
    }

    public async Task StartLiveReadAsync()
    {
        if (SelectedDevice is null || IsLiveReading)
        {
            return;
        }

        _liveReadCancellation = new CancellationTokenSource();
        IsLiveReading = true;
        SetLastCommand("Start live microphone read");
        SetLastStateTransition("Started live microphone read");
        SyncFooter();
        _statusMessage = "Live microphone read started.";

        var deviceId = SelectedDevice.DeviceId;
        var targetUpdateRate = ParseTargetUpdateRate();
        var windowMilliseconds = ParseWindowMilliseconds(_windowMillisecondsInput);
        var channelMode = ParseChannelMode(_selectedChannelModeLabel);
        var cancellationToken = _liveReadCancellation.Token;

        _liveReadTask = RunLiveReadLoopAsync(deviceId, targetUpdateRate, windowMilliseconds, channelMode, cancellationToken);
        await Task.CompletedTask;
    }

    public void StopLiveRead()
    {
        if (_liveReadCancellation is null)
        {
            return;
        }

        _liveReadCancellation.Cancel();
        SetLastStateTransition("Stopped live microphone read");
    }

    public void ClearData()
    {
        _latestWaveformSamples = [];
        PlotPoints = [];
        PlotFillPoints = [];
        CurrentPrimaryValue = "-∞ dBFS";
        _peakCard.Value = "--";
        _sampleRateCard.Value = "--";
        _windowCard.Value = "--";
        _clippingCard.Value = "No";
        FooterFormatLabel = "--";
        _statusMessage = "Waveform cleared.";
        OnPropertyChanged(nameof(CanClearData));
        OnPropertyChanged(nameof(CanExportData));
    }

    public void ExportCsv()
    {
        if (_latestWaveformSamples.Length == 0)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = "microphone-window.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var sampleRate = SelectedDevice?.SampleRate ?? 0;
        var builder = new StringBuilder();
        builder.AppendLine("sample_index,time_ms,amplitude");
        for (var index = 0; index < _latestWaveformSamples.Length; index++)
        {
            var timeMs = sampleRate > 0
                ? index * 1000.0 / sampleRate
                : 0.0;
            builder.Append(index);
            builder.Append(',');
            builder.Append(timeMs.ToString("0.###", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.AppendLine(_latestWaveformSamples[index].ToString("0.######", CultureInfo.InvariantCulture));
        }

        File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.UTF8);
        _statusMessage = $"Exported waveform to {Path.GetFileName(dialog.FileName)}.";
    }

    public string GetDiagnosticsSummary()
    {
        return string.Join(Environment.NewLine, new[]
        {
            $"Device: {SelectedDevice?.DisplayName ?? "No microphone"}",
            $"Connection: {(IsConnected ? "Connected" : "Disconnected")}",
            $"Target update rate: {_targetUpdateRateInput} Hz",
            $"Window: {_windowMillisecondsInput} ms",
            $"Channel mode: {_selectedChannelModeLabel}",
            $"Status: {_statusMessage}"
        });
    }

    public void Dispose()
    {
        _liveReadCancellation?.Cancel();
        _liveReadCancellation?.Dispose();
    }

    private async Task ApplyLiveFrameAsync(MicrophoneFrame frame)
    {
        await RunOnUiAsync(() =>
        {
            _lastSourceMode = "live";
            SetLastHardwareResponse("Live microphone frame received.");
            ApplyFrame(frame, "live");
        }).ConfigureAwait(false);
    }

    private async Task RunLiveReadLoopAsync(
        string deviceId,
        double targetUpdateRate,
        int windowMilliseconds,
        MicrophoneChannelMode channelMode,
        CancellationToken cancellationToken)
    {
        try
        {
            await _client.StreamFramesAsync(
                deviceId,
                targetUpdateRate,
                windowMilliseconds,
                channelMode,
                ApplyLiveFrameAsync,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await RunOnUiAsync(() =>
            {
                SetLastError(ex.Message);
                _statusMessage = $"Live microphone read failed: {ex.Message}";
            }).ConfigureAwait(false);
        }
        finally
        {
            await RunOnUiAsync(() =>
            {
                IsLiveReading = false;
                _liveReadCancellation?.Dispose();
                _liveReadCancellation = null;
                _liveReadTask = null;
                SyncFooter();
            }).ConfigureAwait(false);
        }
    }

    public async Task ApplySettingsAsync()
    {
        if (!TryParsePositiveDouble(_draftTargetUpdateRateInput, out _))
        {
            SetLastValidationResult("Target update rate must be a positive number.");
            _statusMessage = "Target update rate must be a positive number.";
            return;
        }

        if (!TryParsePositiveInt(_draftWindowMillisecondsInput, out _))
        {
            SetLastValidationResult("Window must be a positive integer.");
            _statusMessage = "Window must be a positive integer.";
            return;
        }

        SetLastValidationResult("Validated microphone settings.");
        var previousUpdateRate = _targetUpdateRateInput;
        var previousWindow = _windowMillisecondsInput;
        var previousMode = _selectedChannelModeLabel;

        _targetUpdateRateInput = _draftTargetUpdateRateInput;
        _windowMillisecondsInput = _draftWindowMillisecondsInput;
        _selectedChannelModeLabel = _draftSelectedChannelModeLabel;
        SyncXAxisLabels(ParseWindowMilliseconds(_windowMillisecondsInput));
        SyncFooter();
        OnPropertyChanged(nameof(CanApplySettings));
        _lifecycleActionCommand.NotifyCanExecuteChanged();

        var restartLive = IsLiveReading;
        var appliedToHardware = true;
        if (restartLive)
        {
            StopLiveRead();
            if (_liveReadTask is not null)
            {
                try
                {
                    await _liveReadTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            await StartLiveReadAsync().ConfigureAwait(false);
            appliedToHardware = _lastError is null;
        }
        else if (IsConnected)
        {
            appliedToHardware = await TryApplySettingsSnapshotAsync().ConfigureAwait(true);
        }

        if (!appliedToHardware)
        {
            _targetUpdateRateInput = previousUpdateRate;
            _windowMillisecondsInput = previousWindow;
            _selectedChannelModeLabel = previousMode;
            SyncXAxisLabels(ParseWindowMilliseconds(_windowMillisecondsInput));
            SyncFooter();
            OnPropertyChanged(nameof(CanApplySettings));
            _lifecycleActionCommand.NotifyCanExecuteChanged();
            return;
        }

        _statusMessage = restartLive
            ? "Microphone settings applied. Restarting live read..."
            : IsConnected
                ? "Microphone settings applied."
                : "Microphone settings staged for the next capture.";

        ClearLastError();
        SetLastStateTransition(restartLive
            ? "Applied settings and restarted live microphone read"
            : IsConnected
                ? "Applied settings"
                : "Staged settings for the next capture");
        if (IsConnected)
        {
            CaptureAppliedSettingsSnapshot("Applied to connected integrated microphone.");
            SessionEndOutput = null;
        }
    }

    private void ApplyFrame(MicrophoneFrame frame, string sourceMode)
    {
        _latestWaveformSamples = frame.Samples.ToArray();
        _lastFrameCapturedAt = DateTimeOffset.Now;
        _frameSequence++;
        _lastSourceMode = sourceMode;

        CurrentPrimaryValue = FormatDbfs(frame.RmsDbfs);
        _peakCard.Value = FormatDbfs(frame.PeakDbfs);
        _sampleRateCard.Value = $"{frame.SampleRate} Hz";
        _windowCard.Value = $"{frame.WindowDuration.TotalMilliseconds:0} ms";
        _clippingCard.Value = frame.IsClipping ? "Yes" : "No";

        FooterFormatLabel = $"{frame.SampleRate / 1000.0:0.0} kHz / {frame.Channels} ch";
        FooterModeLabel = FormatChannelMode(frame.ChannelMode);
        FooterSystemStateLabel = IsLiveReading ? "Streaming" : "Frame ready";

        BuildPlot(frame.Samples, frame.WindowDuration);

        DataOutput = new IntegrationPanelDataOutput
        {
            Timestamp = _lastFrameCapturedAt,
            EndpointId = frame.DeviceId,
            PayloadType = "AudioWindow",
            PayloadValue = $"RMS={FormatDbfs(frame.RmsDbfs)}; Peak={FormatDbfs(frame.PeakDbfs)}; Samples={frame.Samples.Length}",
            Units = "dBFS",
            SequenceNumber = _frameSequence,
            CaptureRate = ParseTargetUpdateRate(),
            SourceMode = _lastSourceMode
        };

        CaptureAppliedSettingsSnapshot($"Verified during {sourceMode}.");
        OnPropertyChanged(nameof(CanClearData));
        OnPropertyChanged(nameof(CanExportData));
    }

    private void BuildPlot(float[] samples, TimeSpan windowDuration)
    {
        var waveform = MicrophoneAnalysis.BuildWaveformEnvelope(samples, PlotPointCount);
        if (waveform.Length == 0)
        {
            PlotPoints = [];
            PlotFillPoints = [];
            return;
        }

        var centerY = PlotCanvasHeight / 2.0;
        var amplitudeScale = centerY * 0.9;
        var points = new PointCollection(waveform.Length);
        var fillPoints = new PointCollection(waveform.Length + 2)
        {
            new Point(0, centerY)
        };

        for (var index = 0; index < waveform.Length; index++)
        {
            var x = waveform.Length == 1
                ? 0
                : index * PlotCanvasWidth / (waveform.Length - 1);
            var y = centerY - (waveform[index] * amplitudeScale);
            var point = new Point(x, y);
            points.Add(point);
            fillPoints.Add(point);
        }

        fillPoints.Add(new Point(PlotCanvasWidth, centerY));
        PlotPoints = points;
        PlotFillPoints = fillPoints;
        SyncXAxisLabels((int)Math.Round(windowDuration.TotalMilliseconds));
    }

    private double ParseTargetUpdateRate()
    {
        return TryParsePositiveDouble(_targetUpdateRateInput, out var value) ? value : 20.0;
    }

    private static int ParseWindowMilliseconds(string value)
    {
        return TryParsePositiveInt(value, out var parsed) ? parsed : 50;
    }

    private static bool TryParsePositiveDouble(string input, out double value)
    {
        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            value = parsed;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryParsePositiveInt(string input, out int value)
    {
        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            value = parsed;
            return true;
        }

        value = 0;
        return false;
    }

    private static string FormatDbfs(double dbfs)
    {
        return double.IsNegativeInfinity(dbfs)
            ? "-inf dBFS"
            : $"{dbfs:0.0} dBFS";
    }

    private static MicrophoneChannelMode ParseChannelMode(string label)
    {
        return label switch
        {
            "Left" => MicrophoneChannelMode.Left,
            "Right" => MicrophoneChannelMode.Right,
            _ => MicrophoneChannelMode.MonoMix
        };
    }

    private static string FormatChannelMode(MicrophoneChannelMode channelMode)
    {
        return channelMode switch
        {
            MicrophoneChannelMode.Left => "Left",
            MicrophoneChannelMode.Right => "Right",
            _ => "Mono mix"
        };
    }

    private bool CanExecuteLifecycleAction(object? parameter)
    {
        return parameter is IntegrationPanelLifecycleAction.Apply && IsConnected && CanApplySettings;
    }

    private async Task ExecuteLifecycleActionAsync(object? parameter)
    {
        if (parameter is not IntegrationPanelLifecycleAction.Apply)
        {
            return;
        }

        SetLastCommand("Apply integrated microphone settings");
        await ApplySettingsAsync().ConfigureAwait(false);
    }

    private void HandleLifecycleCommandException(Exception exception)
    {
        SetLastError(exception.Message);
        _statusMessage = exception.Message;
    }

    private async Task<bool> TryApplySettingsSnapshotAsync()
    {
        if (SelectedDevice is null)
        {
            SetLastError("No microphone selected.");
            _statusMessage = "No microphone selected.";
            return false;
        }

        try
        {
            _lastSourceMode = "apply";
            var deviceId = SelectedDevice.DeviceId;
            var targetUpdateRate = ParseTargetUpdateRate();
            var windowMilliseconds = ParseWindowMilliseconds(_windowMillisecondsInput);
            var channelMode = ParseChannelMode(_selectedChannelModeLabel);
            var frame = await Task.Run(
                () => _client.CaptureSnapshot(deviceId, targetUpdateRate, windowMilliseconds, channelMode))
                .ConfigureAwait(true);
            ApplyFrame(frame, "apply");
            ClearLastError();
            SetLastHardwareResponse("Applied microphone settings snapshot captured.");
            return true;
        }
        catch (Exception ex)
        {
            SetLastError(ex.Message);
            _statusMessage = $"Applying microphone settings failed: {ex.Message}";
            return false;
        }
    }

    private void CaptureAppliedSettingsSnapshot(string note)
    {
        AppliedSettingsOutput = new IntegrationPanelAppliedSettingsOutput
        {
            AppliedAt = DateTimeOffset.Now,
            DeviceSettings = new Dictionary<string, string?>
            {
                ["Device"] = Title,
                ["Microphone"] = SelectedDevice?.DisplayName
            },
            EndpointSettings = new Dictionary<string, string?>
            {
                ["TargetUpdateRateHz"] = _targetUpdateRateInput,
                ["WindowMilliseconds"] = _windowMillisecondsInput,
                ["ChannelMode"] = _selectedChannelModeLabel
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["Connected"] = IsConnected ? "true" : "false",
                ["LiveReading"] = IsLiveReading ? "true" : "false"
            },
            NormalizationNotes = new[] { note }
        };
    }

    private IntegrationPanelStatusOutput BuildStatusOutput()
    {
        return new IntegrationPanelStatusOutput
        {
            Connected = IsConnected,
            ReadyState = _isBusy ? "Busy" : IsConnected ? "Ready" : "Disconnected",
            FaultState = _lastError,
            LiveState = IsLiveReading ? "Streaming" : "Stopped",
            SelectedEndpoint = SelectedDevice?.DisplayName,
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

    private void ClearLastError()
    {
        if (_lastError is null)
        {
            return;
        }

        _lastError = null;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private void SetLastStateTransition(string value)
    {
        _lastStateTransition = value;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private void SetLastValidationResult(string value)
    {
        _lastValidationResult = value;
        RefreshDiagnosticsOutput();
    }

    private void SyncFooter()
    {
        FooterConnectionLabel = IsConnected ? "Hardware: Connected" : "Hardware: Disconnected";
        FooterDeviceLabel = SelectedDevice?.DisplayName ?? "No microphone";
        FooterModeLabel = _selectedChannelModeLabel;
        if (!IsConnected && _latestWaveformSamples.Length == 0)
        {
            FooterFormatLabel = "--";
        }

        if (!IsLiveReading && FooterSystemStateLabel == "Streaming")
        {
            FooterSystemStateLabel = "System Ready";
        }
    }

    private void SyncXAxisLabels(int windowMilliseconds)
    {
        var quarter = windowMilliseconds / 4.0;
        XAxisStartLabel = "0 ms";
        XAxisMidLeftLabel = $"{quarter:0.#} ms";
        XAxisMidRightLabel = $"{quarter * 3:0.#} ms";
        XAxisEndLabel = $"{windowMilliseconds:0} ms";
    }

    private void RaiseCommandState()
    {
        OnPropertyChanged(nameof(CanToggleConnection));
        OnPropertyChanged(nameof(CanReadOnce));
        OnPropertyChanged(nameof(CanToggleLive));
        OnPropertyChanged(nameof(CanClearData));
        OnPropertyChanged(nameof(CanExportData));
        OnPropertyChanged(nameof(CanApplySettings));
        RefreshStatusOutput();
        _lifecycleActionCommand.NotifyCanExecuteChanged();
    }

    private static Task RunOnUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }
}
