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
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Scalar;
using ExperimentalControlPlatform.App.Widgets;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;

namespace ExperimentalControlPlatform.App.DevicePanels.Pt104;

public sealed class Pt104PanelViewModel : ObservableObject, IScalarSensorPanelViewModel
{
    private const double PlotCanvasWidth = 504;
    private const double PlotCanvasHeight = 280;
    private const int MaxSamples = 120;
    private static readonly IReadOnlyList<IntegrationPanelLifecycleAction> ConnectedLifecycleActions =
    [
        IntegrationPanelLifecycleAction.Apply
    ];

    private sealed class ChannelState
    {
        public Pt104MeasurementType MeasurementType { get; set; } = Pt104MeasurementType.Pt100;

        public int WireCount { get; set; } = 4;

        public int MainsFrequency { get; set; } = 50;

        public bool FilteredRead { get; set; } = true;

        public List<(DateTime Timestamp, double Value)> Samples { get; } = [];

        public string LatestTemperature { get; set; } = "No reading";

        public string LastReadLabel { get; set; } = "Last read: waiting for first sample";

        public bool IsLiveReading { get; set; }

        public DateTimeOffset? LastSampleTimestamp { get; set; }

        public double? LastSampleValue { get; set; }

        public string? LastSourceMode { get; set; }
    }

    public sealed class ChannelTabOption : ObservableObject
    {
        private bool _isAvailable;

        public ChannelTabOption(int channelNumber)
        {
            ChannelNumber = channelNumber;
        }

        public int ChannelNumber { get; }

        public string DisplayLabel => ChannelNumber.ToString(CultureInfo.InvariantCulture);

        public bool IsAvailable
        {
            get => _isAvailable;
            set => SetProperty(ref _isAvailable, value);
        }
    }

    private readonly Pt104Driver _driver;
    private readonly Dictionary<int, ChannelState> _channelStates;
    private readonly IReadOnlyList<ChannelTabOption> _channelOptions;
    private readonly AsyncRelayCommand _lifecycleActionCommand;
    private List<(DateTime Timestamp, double Value)> _samples;
    private CancellationTokenSource? _liveReadCancellation;
    private Task? _liveReadTask;
    private bool _isBusy;
    private bool _isConnected;
    private int _selectedChannel = 4;
    private Pt104MeasurementType _selectedMeasurementType = Pt104MeasurementType.Pt100;
    private int _selectedWireCount = 4;
    private int _selectedMainsFrequency = 50;
    private bool _filteredRead = true;
    private string _connectedDeviceId = "Logger-Offline";
    private string _latestTemperature = "No reading";
    private string _statusMessage = "Ready to connect";
    private string _lastReadLabel = "Last read: waiting for first sample";
    private string _systemHealthLabel = "Idle";
    private string _footerConnectionLabel = "Hardware: Disconnected";
    private string _footerChannelLabel = "Channel 4";
    private string _footerSensorLabel = "PT100";
    private string _footerWireLabel = "4-Wire";
    private string _footerMainsLabel = "50 Hz";
    private string _footerFilterLabel = "Filtered";
    private string _footerSystemStateLabel = "System Ready";
    private string _peakHighLabel = "--";
    private string _peakLowLabel = "--";
    private string _varianceLabel = "--";
    private string _sampleCountLabel = "0";
    private string _xAxisStartLabel = "--";
    private string _xAxisMidLeftLabel = "--";
    private string _xAxisMidRightLabel = "--";
    private string _xAxisEndLabel = "Now";
    private PointCollection _plotPoints = [];
    private PointCollection _plotFillPoints = [];
    private string _plotMinLabel = "--";
    private string _plotLowerMidLabel = "--";
    private string _plotMidLabel = "--";
    private string _plotUpperMidLabel = "--";
    private string _plotMaxLabel = "--";
    private string _plotWindowLabel = "No samples yet";
    private readonly ValueCardItem _peakHighCard = new("Peak high", "--");
    private readonly ValueCardItem _peakLowCard = new("Peak low", "--");
    private readonly ValueCardItem _varianceCard = new("RMS variance", "--");
    private readonly ValueCardItem _sampleCountCard = new("Samples", "0");
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

    public Pt104PanelViewModel(Pt104Driver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _lifecycleActionCommand = new AsyncRelayCommand(ExecuteLifecycleActionAsync, CanExecuteLifecycleAction, HandleLifecycleCommandException);
        _channelOptions = new[] { 1, 2, 3, 4 }.Select(channel => new ChannelTabOption(channel)).ToArray();
        _channelStates = _channelOptions.ToDictionary(channel => channel.ChannelNumber, _ => new ChannelState());
        _samples = _channelStates[_selectedChannel].Samples;
        LoadSelectedChannelState();
        SyncFooterConfig();
        StatisticsCards = new[] { _peakHighCard, _peakLowCard, _varianceCard, _sampleCountCard };
        RefreshStatusOutput();
        RefreshDiagnosticsOutput();
    }

    public string Title => "PT-104";

    public string Subtitle => "USB PT-104 RTD logger test panel";

    public string DeviceSelectorLabel => string.Equals(ConnectedDeviceId, "Logger-Offline", StringComparison.Ordinal)
        ? Title
        : $"{Title} ({ShortDeviceId(ConnectedDeviceId)})";

    public IReadOnlyList<string> DeviceOptions => new[] { DeviceSelectorLabel };

    IEnumerable IScalarSensorPanelViewModel.DeviceOptions => DeviceOptions;

    public object? SelectedDeviceItem
    {
        get => DeviceSelectorLabel;
        set { }
    }

    public string PrimaryValueLabel => "Precision temperature: ";

    public string CurrentPrimaryValue => LatestTemperature;

    public IReadOnlyList<ChannelTabOption> ChannelOptions => _channelOptions
        .Where(channel => channel.IsAvailable)
        .ToArray();

    public IReadOnlyList<Pt104MeasurementType> MeasurementTypeOptions { get; } =
        new[] { Pt104MeasurementType.Pt100, Pt104MeasurementType.Pt1000 };

    public IReadOnlyList<int> WireCountOptions { get; } = new[] { 2, 3, 4 };

    public IReadOnlyList<int> MainsFrequencyOptions { get; } = new[] { 50, 60 };

    public IReadOnlyList<ValueCardItem> StatisticsCards { get; }

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

    public IReadOnlyList<IntegrationPanelLifecycleAction> SupportedLifecycleActions =>
        IsConnected && !AnyChannelLiveReading ? ConnectedLifecycleActions : [];

    public ICommand LifecycleActionCommand => _lifecycleActionCommand;

    IEnumerable IScalarSensorPanelViewModel.ChannelOptions => ChannelOptions;

    public object? SelectedChannelItem
    {
        get => CurrentChannelTab;
        set
        {
            if (value is ChannelTabOption channel)
            {
                SelectedChannel = channel.ChannelNumber;
            }
        }
    }

    IEnumerable IScalarSensorPanelViewModel.MeasurementTypeOptions => MeasurementTypeOptions;

    public object? SelectedMeasurementTypeItem
    {
        get => SelectedMeasurementType;
        set
        {
            if (value is Pt104MeasurementType measurementType)
            {
                SelectedMeasurementType = measurementType;
            }
        }
    }

    IEnumerable IScalarSensorPanelViewModel.WireCountOptions => WireCountOptions;

    public object? SelectedWireCountItem
    {
        get => SelectedWireCount;
        set
        {
            if (value is int wireCount)
            {
                SelectedWireCount = wireCount;
            }
        }
    }

    IEnumerable IScalarSensorPanelViewModel.MainsFrequencyOptions => MainsFrequencyOptions;

    public object? SelectedMainsFrequencyItem
    {
        get => SelectedMainsFrequency;
        set
        {
            if (value is int mainsFrequency)
            {
                SelectedMainsFrequency = mainsFrequency;
            }
        }
    }

    public double PlotWidth => PlotCanvasWidth;

    public double PlotHeight => PlotCanvasHeight;

    public string DiagnosticsTitle => "PT-104 diagnostics";

    public string DiagnosticsSubtitle => "Hardware state and channel configuration";

    public int SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            if (SetProperty(ref _selectedChannel, value))
            {
                OnPropertyChanged(nameof(SelectedChannelItem));
                LoadSelectedChannelState();
                SyncFooterConfig();
                OnPropertyChanged(nameof(IsLiveReading));
                OnPropertyChanged(nameof(CanReadOnce));
                OnPropertyChanged(nameof(CanStartLive));
                OnPropertyChanged(nameof(CanStopLive));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(LiveToggleLabel));
                OnPropertyChanged(nameof(LiveToggleIconKind));
                OnPropertyChanged(nameof(SupportedLifecycleActions));
                RefreshStatusOutput();
                HandleChannelContextSwitch();
            }
        }
    }

    public Pt104MeasurementType SelectedMeasurementType
    {
        get => _selectedMeasurementType;
        set
        {
            if (SetProperty(ref _selectedMeasurementType, value))
            {
                CurrentChannelState.MeasurementType = value;
                OnPropertyChanged(nameof(SelectedMeasurementTypeItem));
                SyncFooterConfig();
            }
        }
    }

    public int SelectedWireCount
    {
        get => _selectedWireCount;
        set
        {
            if (SetProperty(ref _selectedWireCount, value))
            {
                CurrentChannelState.WireCount = value;
                OnPropertyChanged(nameof(SelectedWireCountItem));
                SyncFooterConfig();
            }
        }
    }

    public int SelectedMainsFrequency
    {
        get => _selectedMainsFrequency;
        set
        {
            if (SetProperty(ref _selectedMainsFrequency, value))
            {
                CurrentChannelState.MainsFrequency = value;
                OnPropertyChanged(nameof(SelectedMainsFrequencyItem));
                SyncFooterConfig();
            }
        }
    }

    public bool FilteredRead
    {
        get => _filteredRead;
        set
        {
            if (SetProperty(ref _filteredRead, value))
            {
                CurrentChannelState.FilteredRead = value;
                SyncFooterConfig();
            }
        }
    }

    public string ConnectedDeviceId
    {
        get => _connectedDeviceId;
        private set
        {
            if (SetProperty(ref _connectedDeviceId, value))
            {
                OnPropertyChanged(nameof(DeviceSelectorLabel));
                OnPropertyChanged(nameof(DeviceOptions));
                OnPropertyChanged(nameof(SelectedDeviceItem));
            }
        }
    }

    public string LatestTemperature
    {
        get => _latestTemperature;
        private set
        {
            if (SetProperty(ref _latestTemperature, value))
            {
                OnPropertyChanged(nameof(CurrentPrimaryValue));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string LastReadLabel
    {
        get => _lastReadLabel;
        private set => SetProperty(ref _lastReadLabel, value);
    }

    public string SystemHealthLabel
    {
        get => _systemHealthLabel;
        private set => SetProperty(ref _systemHealthLabel, value);
    }

    public string FooterConnectionLabel
    {
        get => _footerConnectionLabel;
        private set => SetProperty(ref _footerConnectionLabel, value);
    }

    public string FooterChannelLabel
    {
        get => _footerChannelLabel;
        private set => SetProperty(ref _footerChannelLabel, value);
    }

    public string FooterSensorLabel
    {
        get => _footerSensorLabel;
        private set => SetProperty(ref _footerSensorLabel, value);
    }

    public string FooterWireLabel
    {
        get => _footerWireLabel;
        private set => SetProperty(ref _footerWireLabel, value);
    }

    public string FooterMainsLabel
    {
        get => _footerMainsLabel;
        private set => SetProperty(ref _footerMainsLabel, value);
    }

    public string FooterFilterLabel
    {
        get => _footerFilterLabel;
        private set => SetProperty(ref _footerFilterLabel, value);
    }

    public string FooterSystemStateLabel
    {
        get => _footerSystemStateLabel;
        private set => SetProperty(ref _footerSystemStateLabel, value);
    }

    public string PeakHighLabel
    {
        get => _peakHighLabel;
        private set => SetProperty(ref _peakHighLabel, value);
    }

    public string PeakLowLabel
    {
        get => _peakLowLabel;
        private set => SetProperty(ref _peakLowLabel, value);
    }

    public string VarianceLabel
    {
        get => _varianceLabel;
        private set => SetProperty(ref _varianceLabel, value);
    }

    public string SampleCountLabel
    {
        get => _sampleCountLabel;
        private set => SetProperty(ref _sampleCountLabel, value);
    }

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

    public string PlotMinLabel
    {
        get => _plotMinLabel;
        private set => SetProperty(ref _plotMinLabel, value);
    }

    public string PlotLowerMidLabel
    {
        get => _plotLowerMidLabel;
        private set => SetProperty(ref _plotLowerMidLabel, value);
    }

    public string PlotMidLabel
    {
        get => _plotMidLabel;
        private set => SetProperty(ref _plotMidLabel, value);
    }

    public string PlotUpperMidLabel
    {
        get => _plotUpperMidLabel;
        private set => SetProperty(ref _plotUpperMidLabel, value);
    }

    public string PlotMaxLabel
    {
        get => _plotMaxLabel;
        private set => SetProperty(ref _plotMaxLabel, value);
    }

    public string PlotWindowLabel
    {
        get => _plotWindowLabel;
        private set => SetProperty(ref _plotWindowLabel, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(CanConnect));
                OnPropertyChanged(nameof(CanDisconnect));
                OnPropertyChanged(nameof(CanToggleConnection));
                OnPropertyChanged(nameof(CanReadOnce));
                OnPropertyChanged(nameof(CanStartLive));
                OnPropertyChanged(nameof(CanStopLive));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(ConnectionToggleLabel));
                OnPropertyChanged(nameof(ConnectionToggleIconKind));
                OnPropertyChanged(nameof(LiveToggleLabel));
                OnPropertyChanged(nameof(LiveToggleIconKind));
                OnPropertyChanged(nameof(CanClearData));
                OnPropertyChanged(nameof(CanExportData));
                OnPropertyChanged(nameof(SupportedLifecycleActions));
                RefreshStatusOutput();
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanConnect));
                OnPropertyChanged(nameof(CanDisconnect));
                OnPropertyChanged(nameof(CanToggleConnection));
                OnPropertyChanged(nameof(CanReadOnce));
                OnPropertyChanged(nameof(CanStartLive));
                OnPropertyChanged(nameof(CanStopLive));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(LiveToggleLabel));
                OnPropertyChanged(nameof(LiveToggleIconKind));
                OnPropertyChanged(nameof(CanClearData));
                OnPropertyChanged(nameof(CanExportData));
                OnPropertyChanged(nameof(SupportedLifecycleActions));
                RefreshStatusOutput();
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLiveReading => CurrentChannelState.IsLiveReading;

    public bool CanConnect => !IsConnected && !IsBusy;

    public bool CanDisconnect => IsConnected && !IsBusy && !AnyChannelLiveReading;

    public bool CanToggleConnection => CanConnect || CanDisconnect;

    public bool CanReadOnce => IsConnected && !IsBusy && !AnyChannelLiveReading;

    public bool CanStartLive => IsConnected && !IsBusy && !IsLiveReading;

    public bool CanStopLive => IsLiveReading;

    public bool CanToggleLive => CanStartLive || CanStopLive;

    public string ConnectionToggleLabel => IsConnected ? "Disconnect" : "Connect";

    public PackIconMaterialKind ConnectionToggleIconKind => IsConnected
        ? PackIconMaterialKind.LinkVariantOff
        : PackIconMaterialKind.LinkVariant;

    public string LiveToggleLabel => IsLiveReading ? "Stop live" : "Start live";

    public PackIconMaterialKind LiveToggleIconKind => IsLiveReading
        ? PackIconMaterialKind.Stop
        : PackIconMaterialKind.Play;

    public bool CanClearData => !IsBusy && _samples.Count > 0;

    public bool CanExportData => !IsBusy && _samples.Count > 0;

    public async Task ConnectAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var settings = BuildSettings();
            SetLastCommand($"Connect channel {SelectedChannel}");
            await Task.Run(() => _driver.Connect(settings));
            await VerifyChannelAvailabilityAsync();
            settings = BuildSettings();
            await Task.Run(() => _driver.ApplySettings(settings));

            IsConnected = true;
            ConnectedDeviceId = _driver.ConnectedDeviceId ?? "PT-104 connected";
            StatusMessage = $"Connected. {ChannelOptions.Count} active channel(s) verified.";
            SystemHealthLabel = "Nominal Operation";
            FooterConnectionLabel = "Hardware: Connected";
            FooterSystemStateLabel = "System Ready";
            CaptureAppliedSettingsSnapshot(settings, "Applied to connected PT-104 hardware.");
            SessionEndOutput = null;
            ClearLastError();
            SetLastHardwareResponse($"Connected to {ConnectedDeviceId}.");
            SetLastStateTransition("Disconnected -> Connected");
        });
    }

    public async Task DisconnectAsync()
    {
        if (!IsConnected)
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var hadLiveReads = AnyChannelLiveReading;
            SetLastCommand($"Disconnect channel {SelectedChannel}");
            StopAllLiveReads();
            await Task.Run(() => _driver.Disconnect());
            IsConnected = false;
            ConnectedDeviceId = "Logger-Offline";
            StatusMessage = "Disconnected.";
            SystemHealthLabel = "Idle";
            FooterConnectionLabel = "Hardware: Disconnected";
            FooterSystemStateLabel = "Idle";
            ClearLastError();
            SetLastHardwareResponse("PT-104 connection closed.");
            SetLastStateTransition("Connected -> Disconnected");
            SessionEndOutput = new IntegrationPanelSessionEndOutput
            {
                EndedAt = DateTimeOffset.Now,
                ExitReason = "Disconnected from PT-104 panel.",
                ConnectionClosed = true,
                LiveStopped = hadLiveReads,
                AppliedSettingsSnapshot = AppliedSettingsOutput,
                FinalStatus = BuildStatusOutput(),
                OpenIssues = null
            };
        });
    }

    public async Task ReadOnceAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var settings = BuildSettings();
            SetLastCommand($"Read once on channel {SelectedChannel}");
            await Task.Run(() => _driver.ApplySettings(settings));
            CaptureAppliedSettingsSnapshot(settings, "Applied to connected PT-104 hardware before a single read.");
            var reading = await Task.Run(() => _driver.ReadTemperatureC(FilteredRead));
            SetChannelAvailability(CurrentChannelTab, true);
            ApplyReading(SelectedChannel, reading, "ReadOnce");
            StatusMessage = $"Read channel {SelectedChannel} successfully.";
            ClearLastError();
            SetLastHardwareResponse($"Read {reading:F3} C from channel {SelectedChannel}.");
        });
    }

    public Task StartLiveReadAsync()
    {
        if (!CanStartLive)
        {
            return Task.CompletedTask;
        }

        try
        {
            SetLastCommand($"Start live read on channel {SelectedChannel}");
            CurrentChannelState.IsLiveReading = true;
            SetChannelAvailability(CurrentChannelTab, true);
            RaiseLiveStateChanged();
            EnsureLiveReadLoop();
            StatusMessage = $"Live read started on channel {SelectedChannel}.";
            SystemHealthLabel = "Streaming";
            FooterSystemStateLabel = "Streaming";
            ClearLastError();
            SetLastStateTransition($"Channel {SelectedChannel} idle -> live");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            SystemHealthLabel = "Fault";
            FooterSystemStateLabel = "Fault";
            SetLastError(ex.Message);
        }

        return Task.CompletedTask;
    }

    public void StopLiveRead()
    {
        if (!CurrentChannelState.IsLiveReading)
        {
            return;
        }

        SetLastCommand($"Stop live read on channel {SelectedChannel}");
        CurrentChannelState.IsLiveReading = false;
        RaiseLiveStateChanged();

        if (!AnyChannelLiveReading)
        {
            _liveReadCancellation?.Cancel();
            StatusMessage = "Live read stopped.";
            SystemHealthLabel = IsConnected ? "Nominal Operation" : "Idle";
            FooterSystemStateLabel = IsConnected ? "System Ready" : "Idle";
        }
        else
        {
            StatusMessage = $"Live read stopped on channel {SelectedChannel}.";
            SystemHealthLabel = "Streaming";
            FooterSystemStateLabel = "Streaming";
        }

        SetLastHardwareResponse($"Live acquisition stopped on channel {SelectedChannel}.");
        SetLastStateTransition($"Channel {SelectedChannel} live -> idle");
    }

    public void ClearData()
    {
        SetLastCommand($"Clear buffered data on channel {SelectedChannel}");
        _samples.Clear();
        LatestTemperature = "No reading";
        LastReadLabel = "Last read: cleared";
        CurrentChannelState.LatestTemperature = LatestTemperature;
        CurrentChannelState.LastReadLabel = LastReadLabel;
        PeakHighLabel = "--";
        PeakLowLabel = "--";
        VarianceLabel = "--";
        SampleCountLabel = "0";
        _peakHighCard.Value = "--";
        _peakLowCard.Value = "--";
        _varianceCard.Value = "--";
        _sampleCountCard.Value = "0";
        PlotPoints = [];
        PlotFillPoints = [];
        PlotMinLabel = "--";
        PlotMaxLabel = "--";
        PlotWindowLabel = "No samples yet";
        XAxisStartLabel = "--";
        XAxisMidLeftLabel = "--";
        XAxisMidRightLabel = "--";
        XAxisEndLabel = "Now";
        StatusMessage = "Data cleared.";
        CurrentChannelState.LastSampleTimestamp = null;
        CurrentChannelState.LastSampleValue = null;
        CurrentChannelState.LastSourceMode = null;
        RefreshDataOutput();
        SetLastHardwareResponse("Buffered PT-104 samples cleared in panel.");
        OnPropertyChanged(nameof(CanClearData));
        OnPropertyChanged(nameof(CanExportData));
    }

    public void ExportCsv()
    {
        if (_samples.Count == 0)
        {
            StatusMessage = "No samples available to export.";
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"pt104-channel-{SelectedChannel}-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() != true)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("timestamp_iso,temperature_c");

        foreach (var sample in _samples)
        {
            builder.AppendLine($"{sample.Timestamp:O},{sample.Value.ToString("F6", CultureInfo.InvariantCulture)}");
        }

        File.WriteAllText(saveDialog.FileName, builder.ToString(), Encoding.UTF8);
        StatusMessage = $"Exported {_samples.Count} samples to {Path.GetFileName(saveDialog.FileName)}.";
        SetLastCommand($"Export CSV for channel {SelectedChannel}");
        SetLastHardwareResponse($"Exported {_samples.Count} samples from channel {SelectedChannel}.");
    }

    public string GetDiagnosticsSummary()
    {
        return string.Join(
            Environment.NewLine,
            $"Device: {ConnectedDeviceId}",
            $"Connection: {(IsConnected ? "Connected" : "Disconnected")}",
            $"Channel: {SelectedChannel}",
            $"Sensor: {SelectedMeasurementType}",
            $"Wire Count: {SelectedWireCount}",
            $"Mains Filter: {SelectedMainsFrequency} Hz",
            $"Filtered Read: {(FilteredRead ? "Enabled" : "Disabled")}",
            $"Samples Buffered: {_samples.Count}",
            $"Last Read: {LastReadLabel}",
            $"Status: {StatusMessage}");
    }

    public void Dispose()
    {
        StopAllLiveReads();
        _liveReadCancellation?.Cancel();
        _liveReadCancellation?.Dispose();
        _driver.Dispose();
    }

    private Pt104ConnectionSettings BuildSettings()
    {
        return BuildSettings(SelectedChannel);
    }

    private Pt104ConnectionSettings BuildSettings(int channel)
    {
        var state = _channelStates[channel];
        return new Pt104ConnectionSettings(
            channel,
            state.MeasurementType,
            state.WireCount,
            state.MainsFrequency,
            state.FilteredRead);
    }

    private ChannelState CurrentChannelState => _channelStates[_selectedChannel];

    private ChannelTabOption CurrentChannelTab => _channelOptions.First(option => option.ChannelNumber == _selectedChannel);

    private bool AnyChannelLiveReading => _channelStates.Values.Any(state => state.IsLiveReading);

    private void LoadSelectedChannelState()
    {
        var state = CurrentChannelState;
        _samples = state.Samples;

        if (SetProperty(ref _selectedMeasurementType, state.MeasurementType))
        {
            OnPropertyChanged(nameof(SelectedMeasurementTypeItem));
        }

        if (SetProperty(ref _selectedWireCount, state.WireCount))
        {
            OnPropertyChanged(nameof(SelectedWireCountItem));
        }

        if (SetProperty(ref _selectedMainsFrequency, state.MainsFrequency))
        {
            OnPropertyChanged(nameof(SelectedMainsFrequencyItem));
        }

        SetProperty(ref _filteredRead, state.FilteredRead);
        LatestTemperature = state.LatestTemperature;
        LastReadLabel = state.LastReadLabel;
        UpdatePlot();
        UpdateStats();
        RefreshDataOutput();
        OnPropertyChanged(nameof(CanClearData));
        OnPropertyChanged(nameof(CanExportData));
    }

    private void HandleChannelContextSwitch()
    {
        if (AnyChannelLiveReading)
        {
            StatusMessage = CurrentChannelState.IsLiveReading
                ? $"Loaded channel {SelectedChannel}. Live read continues."
                : $"Loaded channel {SelectedChannel}.";
            SystemHealthLabel = "Streaming";
            FooterSystemStateLabel = "Streaming";
            return;
        }

        if (IsConnected && !IsBusy)
        {
            _ = ReconfigureCurrentChannelAsync();
        }
        else
        {
            StatusMessage = $"Loaded channel {SelectedChannel}.";
        }
    }

    private async Task ReconfigureCurrentChannelAsync()
    {
        try
        {
            IsBusy = true;
            var settings = BuildSettings();
            SetLastCommand($"Reconfigure channel {SelectedChannel}");
            await Task.Run(() => _driver.ApplySettings(settings));
            CaptureAppliedSettingsSnapshot(settings, "Applied to connected PT-104 hardware.");
            StatusMessage = $"Loaded channel {SelectedChannel}.";
            ClearLastError();
            SetLastHardwareResponse($"Applied settings to channel {SelectedChannel}.");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            SystemHealthLabel = "Fault";
            FooterSystemStateLabel = "Fault";
            SetLastError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunBusyOperationAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            SystemHealthLabel = "Fault";
            FooterSystemStateLabel = "Fault";
            SetLastError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunLiveReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var activeChannels = _channelOptions
                    .Where(channel => _channelStates[channel.ChannelNumber].IsLiveReading)
                    .ToArray();

                if (activeChannels.Length == 0)
                {
                    break;
                }

                foreach (var channel in activeChannels)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var settings = BuildSettings(channel.ChannelNumber);
                        var filtered = _channelStates[channel.ChannelNumber].FilteredRead;

                        await Task.Run(() => _driver.ApplySettings(settings), cancellationToken);
                        var reading = await Task.Run(
                            () => _driver.ReadTemperatureC(filtered, attempts: 3, delayMilliseconds: 500),
                            cancellationToken);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SetChannelAvailability(channel, true);
                            ApplyReading(channel.ChannelNumber, reading, "LiveRead");
                            CaptureAppliedSettingsSnapshot(settings, "Applied during PT-104 live acquisition.");
                            if (channel.ChannelNumber == SelectedChannel)
                            {
                                StatusMessage = $"Live read updated at {DateTime.Now:HH:mm:ss}.";
                            }
                            SetLastHardwareResponse($"Live sample {reading:F3} C from channel {channel.ChannelNumber}.");
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SetChannelAvailability(channel, false);
                            _channelStates[channel.ChannelNumber].IsLiveReading = false;
                            if (channel.ChannelNumber == SelectedChannel)
                            {
                                StatusMessage = ex.Message;
                            }
                            SetLastError(ex.Message);
                            RaiseLiveStateChanged();
                        });
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected stop path.
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = ex.Message;
                SystemHealthLabel = "Fault";
                FooterSystemStateLabel = "Fault";
                SetLastError(ex.Message);
            });
        }
        finally
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StopAllLiveReads();
                RaiseLiveStateChanged();
                StatusMessage = IsConnected ? "Live read stopped." : "Ready to connect";
                SystemHealthLabel = IsConnected ? "Nominal Operation" : "Idle";
                FooterSystemStateLabel = IsConnected ? "System Ready" : "Idle";
                SetLastStateTransition("Live acquisition stopped");
            });

            _liveReadCancellation?.Dispose();
            _liveReadCancellation = null;
            _liveReadTask = null;
        }
    }

    private void ApplyReading(int channel, double reading, string sourceMode)
    {
        var timestamp = DateTime.Now;
        var state = _channelStates[channel];
        var samples = state.Samples;
        samples.Add((timestamp, reading));

        if (samples.Count > MaxSamples)
        {
            samples.RemoveAt(0);
        }

        state.LatestTemperature = $"{reading:F3}°C";
        state.LastReadLabel = $"Last read: {timestamp:HH:mm:ss}";
        state.LastSampleTimestamp = new DateTimeOffset(timestamp);
        state.LastSampleValue = reading;
        state.LastSourceMode = sourceMode;

        if (channel == SelectedChannel)
        {
            LatestTemperature = state.LatestTemperature;
            LastReadLabel = state.LastReadLabel;
            UpdatePlot();
            UpdateStats();
            RefreshDataOutput();
            OnPropertyChanged(nameof(CanClearData));
            OnPropertyChanged(nameof(CanExportData));
        }
    }

    private async Task VerifyChannelAvailabilityAsync()
    {
        await FastVerifyChannelAvailabilityAsync(_channelOptions);

        if (_channelOptions.Any(option => option.IsAvailable))
        {
            if (!CurrentChannelTab.IsAvailable)
            {
                var firstAvailableFast = _channelOptions.FirstOrDefault(channel => channel.IsAvailable);
                if (firstAvailableFast is not null)
                {
                    SelectedChannel = firstAvailableFast.ChannelNumber;
                }
            }

            SetLastValidationResult($"{_channelOptions.Count(option => option.IsAvailable)} channel(s) available.");
            return;
        }

        foreach (var channel in _channelOptions)
        {
            var state = _channelStates[channel.ChannelNumber];
            var detectedSettings = await DetectWorkingChannelSettingsAsync(channel.ChannelNumber, state);
            if (detectedSettings is not null)
            {
                state.MeasurementType = detectedSettings.MeasurementType;
                state.WireCount = detectedSettings.WireCount;
                state.MainsFrequency = detectedSettings.MainsFrequencyHz;
                state.FilteredRead = detectedSettings.FilteredRead;
                SetChannelAvailability(channel, true);
            }
            else
            {
                SetChannelAvailability(channel, false);
            }
        }

        if (!CurrentChannelTab.IsAvailable)
        {
            var firstAvailable = _channelOptions.FirstOrDefault(channel => channel.IsAvailable);
            if (firstAvailable is not null)
            {
                SelectedChannel = firstAvailable.ChannelNumber;
            }
        }

        SetLastValidationResult($"{_channelOptions.Count(option => option.IsAvailable)} channel(s) available after fallback scan.");
    }

    private async Task FastVerifyChannelAvailabilityAsync(IEnumerable<ChannelTabOption> channels)
    {
        var channelList = channels.ToArray();
        foreach (var channel in channelList)
        {
            await Task.Run(() => _driver.ConfigureChannel(BuildSettings(channel.ChannelNumber)));
        }

        // PT-104 samples continuously after configuration; let one normal conversion window complete
        // before reading back latest values instead of reconfiguring and waiting channel-by-channel.
        var settleDelay = TimeSpan.FromMilliseconds(Math.Max(2000, channelList.Length * 950));
        await Task.Delay(settleDelay);

        foreach (var channel in channelList)
        {
            var state = _channelStates[channel.ChannelNumber];
            try
            {
                _ = await Task.Run(() => _driver.ReadTemperatureC(channel.ChannelNumber, state.FilteredRead, attempts: 1, delayMilliseconds: 0, allowRepeatValue: false));
                SetChannelAvailability(channel, true);
            }
            catch
            {
                SetChannelAvailability(channel, false);
            }
        }
    }

    private async Task<Pt104ConnectionSettings?> DetectWorkingChannelSettingsAsync(int channel, ChannelState state)
    {
        foreach (var candidate in GetProbeSettings(channel, state))
        {
            try
            {
                await Task.Run(() => _driver.ApplySettings(candidate));
                _ = await Task.Run(() => _driver.ReadTemperatureC(candidate.FilteredRead, attempts: 3, delayMilliseconds: 700, allowRepeatValue: false));
                return candidate;
            }
            catch
            {
                // Try the next plausible profile.
            }
        }

        return null;
    }

    private static IEnumerable<Pt104ConnectionSettings> GetProbeSettings(int channel, ChannelState state)
    {
        static IEnumerable<Pt104ConnectionSettings> BuildCandidates(int channel, Pt104MeasurementType type, int mains)
        {
            foreach (var wire in new[] { 4, 3, 2 })
            {
                yield return new Pt104ConnectionSettings(channel, type, wire, mains, true);
                yield return new Pt104ConnectionSettings(channel, type, wire, mains, false);
            }
        }

        var seen = new HashSet<Pt104ConnectionSettings>();

        IEnumerable<Pt104ConnectionSettings> candidates =
        [
            new Pt104ConnectionSettings(channel, state.MeasurementType, state.WireCount, state.MainsFrequency, state.FilteredRead),
            new Pt104ConnectionSettings(channel, state.MeasurementType, state.WireCount, state.MainsFrequency, !state.FilteredRead),
            .. BuildCandidates(channel, state.MeasurementType, state.MainsFrequency),
            .. BuildCandidates(channel, state.MeasurementType, state.MainsFrequency == 50 ? 60 : 50),
        ];

        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private void EnsureLiveReadLoop()
    {
        if (_liveReadTask is { IsCompleted: false })
        {
            return;
        }

        _liveReadCancellation = new CancellationTokenSource();
        _liveReadTask = RunLiveReadLoopAsync(_liveReadCancellation.Token);
    }

    private void StopAllLiveReads()
    {
        foreach (var state in _channelStates.Values)
        {
            state.IsLiveReading = false;
        }
    }

    private void RaiseLiveStateChanged()
    {
        OnPropertyChanged(nameof(IsLiveReading));
        OnPropertyChanged(nameof(CanReadOnce));
        OnPropertyChanged(nameof(CanStartLive));
        OnPropertyChanged(nameof(CanStopLive));
        OnPropertyChanged(nameof(CanToggleConnection));
        OnPropertyChanged(nameof(CanToggleLive));
        OnPropertyChanged(nameof(CanDisconnect));
        OnPropertyChanged(nameof(LiveToggleLabel));
        OnPropertyChanged(nameof(LiveToggleIconKind));
        OnPropertyChanged(nameof(SupportedLifecycleActions));
        RefreshStatusOutput();
        _lifecycleActionCommand.NotifyCanExecuteChanged();
    }

    private void SetChannelAvailability(ChannelTabOption channel, bool isAvailable)
    {
        if (channel.IsAvailable == isAvailable)
        {
            return;
        }

        channel.IsAvailable = isAvailable;
        OnPropertyChanged(nameof(ChannelOptions));
        OnPropertyChanged(nameof(SelectedChannelItem));
        OnPropertyChanged(nameof(SupportedLifecycleActions));
        RefreshStatusOutput();
    }

    private static string ShortDeviceId(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return "Offline";
        }

        var trimmed = deviceId.Trim();
        var colonIndex = trimmed.IndexOf(':');
        return colonIndex >= 0 && colonIndex < trimmed.Length - 1
            ? trimmed[(colonIndex + 1)..]
            : trimmed;
    }

    private void UpdatePlot()
    {
        if (_samples.Count == 0)
        {
            PlotPoints = [];
            PlotFillPoints = [];
            PlotMinLabel = "--";
            PlotLowerMidLabel = "--";
            PlotMidLabel = "--";
            PlotUpperMidLabel = "--";
            PlotMaxLabel = "--";
            PlotWindowLabel = "No samples yet";
            XAxisStartLabel = "--";
            XAxisMidLeftLabel = "--";
            XAxisMidRightLabel = "--";
            XAxisEndLabel = "Now";
            return;
        }

        var minTimestamp = _samples[0].Timestamp;
        var maxTimestamp = _samples[^1].Timestamp;
        var minValue = _samples.Min(x => x.Value);
        var maxValue = _samples.Max(x => x.Value);

        if (Math.Abs(maxValue - minValue) < 0.001)
        {
            minValue -= 0.5;
            maxValue += 0.5;
        }

        var spanSeconds = Math.Max((maxTimestamp - minTimestamp).TotalSeconds, 1.0);
        var points = new List<Point>(_samples.Count);

        foreach (var sample in _samples)
        {
            var x = ((sample.Timestamp - minTimestamp).TotalSeconds / spanSeconds) * PlotCanvasWidth;
            var normalized = (sample.Value - minValue) / (maxValue - minValue);
            var y = PlotCanvasHeight - (normalized * PlotCanvasHeight);
            points.Add(new Point(x, y));
        }

        PlotPoints = new PointCollection(points);

        var fillPoints = new List<Point>(points.Count + 2)
        {
            new(points[0].X, PlotCanvasHeight)
        };
        fillPoints.AddRange(points);
        fillPoints.Add(new Point(points[^1].X, PlotCanvasHeight));
        PlotFillPoints = new PointCollection(fillPoints);

        PlotMinLabel = $"{minValue:F2}";
        PlotLowerMidLabel = $"{(minValue + ((maxValue - minValue) * 0.25)):F2}";
        PlotMidLabel = $"{(minValue + maxValue) / 2:F2}";
        PlotUpperMidLabel = $"{(minValue + ((maxValue - minValue) * 0.75)):F2}";
        PlotMaxLabel = $"{maxValue:F2}";
        PlotWindowLabel = _samples.Count == 1
            ? "1 sample buffered"
            : $"{_samples.Count} samples over {(maxTimestamp - minTimestamp).TotalSeconds:F0} s";

        XAxisStartLabel = $"-{spanSeconds:F0}s";
        XAxisMidLeftLabel = $"-{(spanSeconds * 0.66):F0}s";
        XAxisMidRightLabel = $"-{(spanSeconds * 0.33):F0}s";
        XAxisEndLabel = "Now";
    }

    private void UpdateStats()
    {
        if (_samples.Count == 0)
        {
            PeakHighLabel = "--";
            PeakLowLabel = "--";
            VarianceLabel = "--";
            SampleCountLabel = "0";
            return;
        }

        var values = _samples.Select(sample => sample.Value).ToArray();
        var average = values.Average();
        var rmsVariance = Math.Sqrt(values.Select(value => Math.Pow(value - average, 2)).Average());

        PeakHighLabel = $"{values.Max():F2}°C";
        PeakLowLabel = $"{values.Min():F2}°C";
        VarianceLabel = $"{rmsVariance:F3}°C";
        SampleCountLabel = values.Length.ToString("N0", CultureInfo.InvariantCulture);
        _peakHighCard.Value = PeakHighLabel;
        _peakLowCard.Value = PeakLowLabel;
        _varianceCard.Value = VarianceLabel;
        _sampleCountCard.Value = SampleCountLabel;
    }

    private void SyncFooterConfig()
    {
        FooterChannelLabel = $"Channel {SelectedChannel}";
        FooterSensorLabel = SelectedMeasurementType.ToString().ToUpperInvariant();
        FooterWireLabel = $"{SelectedWireCount}-Wire";
        FooterMainsLabel = $"{SelectedMainsFrequency} Hz";
        FooterFilterLabel = FilteredRead ? "Filtered" : "Raw";
    }

    private bool CanExecuteLifecycleAction(object? parameter)
    {
        return parameter is IntegrationPanelLifecycleAction.Apply && IsConnected && !IsBusy && !AnyChannelLiveReading;
    }

    private async Task ExecuteLifecycleActionAsync(object? parameter)
    {
        if (parameter is not IntegrationPanelLifecycleAction.Apply)
        {
            return;
        }

        await ApplyCurrentSettingsAsync();
    }

    private async Task ApplyCurrentSettingsAsync()
    {
        if (!IsConnected || AnyChannelLiveReading)
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            var settings = BuildSettings();
            SetLastCommand($"Apply settings for channel {SelectedChannel}");
            await Task.Run(() => _driver.ApplySettings(settings));
            SetLastHardwareResponse($"Applied settings to channel {SelectedChannel}.");
            StatusMessage = $"Applied settings for channel {SelectedChannel}.";
            SystemHealthLabel = "Nominal Operation";
            FooterSystemStateLabel = "System Ready";
            CaptureAppliedSettingsSnapshot(settings, "Applied to connected PT-104 hardware.");

            SessionEndOutput = null;
            ClearLastError();
            SetLastStateTransition($"Applied settings for channel {SelectedChannel}");
        });
    }

    private void HandleLifecycleCommandException(Exception exception)
    {
        SetLastError(exception.Message);
        StatusMessage = exception.Message;
    }

    private void CaptureAppliedSettingsSnapshot(Pt104ConnectionSettings settings, string note)
    {
        AppliedSettingsOutput = new IntegrationPanelAppliedSettingsOutput
        {
            AppliedAt = DateTimeOffset.Now,
            DeviceSettings = new Dictionary<string, string?>
            {
                ["Device"] = Title,
                ["Connection"] = IsConnected ? ConnectedDeviceId : "Disconnected"
            },
            EndpointSettings = new Dictionary<string, string?>
            {
                ["Channel"] = settings.Channel.ToString(CultureInfo.InvariantCulture),
                ["MeasurementType"] = settings.MeasurementType.ToString(),
                ["WireCount"] = settings.WireCount.ToString(CultureInfo.InvariantCulture),
                ["MainsFrequencyHz"] = settings.MainsFrequencyHz.ToString(CultureInfo.InvariantCulture),
                ["FilteredRead"] = settings.FilteredRead ? "true" : "false"
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["SelectedChannel"] = SelectedChannel.ToString(CultureInfo.InvariantCulture),
                ["AnyChannelLiveReading"] = AnyChannelLiveReading ? "true" : "false",
                ["AvailableChannelCount"] = _channelOptions.Count(option => option.IsAvailable).ToString(CultureInfo.InvariantCulture)
            },
            NormalizationNotes = new[] { note }
        };
    }

    private IntegrationPanelStatusOutput BuildStatusOutput()
    {
        var backgroundLiveChannels = _channelOptions
            .Where(channel => channel.ChannelNumber != SelectedChannel && _channelStates[channel.ChannelNumber].IsLiveReading)
            .Select(channel => $"Channel {channel.ChannelNumber}")
            .ToArray();

        return new IntegrationPanelStatusOutput
        {
            Connected = IsConnected,
            ReadyState = IsBusy ? "Busy" : IsConnected ? "Ready" : "Disconnected",
            FaultState = _lastError,
            LiveState = CurrentChannelState.IsLiveReading
                ? "Live on selected channel"
                : AnyChannelLiveReading
                    ? "Live on other channels"
                    : "Stopped",
            SelectedEndpoint = $"Channel {SelectedChannel}",
            BackgroundActiveEndpoints = backgroundLiveChannels
        };
    }

    private void RefreshStatusOutput()
    {
        StatusOutput = BuildStatusOutput();
    }

    private void RefreshDataOutput()
    {
        var state = CurrentChannelState;
        if (state.LastSampleTimestamp is null || state.LastSampleValue is null || _samples.Count == 0)
        {
            DataOutput = null;
            return;
        }

        var captureRate = _samples.Count > 1
            ? (_samples.Count - 1) / Math.Max((_samples[^1].Timestamp - _samples[0].Timestamp).TotalSeconds, 1.0)
            : (double?)null;

        DataOutput = new IntegrationPanelDataOutput
        {
            Timestamp = state.LastSampleTimestamp,
            EndpointId = $"PT-104:Channel-{SelectedChannel}",
            PayloadType = "TemperatureCelsius",
            PayloadValue = state.LastSampleValue.Value.ToString("F3", CultureInfo.InvariantCulture),
            Units = "C",
            SequenceNumber = _samples.Count,
            CaptureRate = captureRate,
            SourceMode = state.LastSourceMode
        };
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
    }

    private void SetLastValidationResult(string value)
    {
        _lastValidationResult = value;
        RefreshDiagnosticsOutput();
    }
}

