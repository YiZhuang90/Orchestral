using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.Widgets;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App.ExperimentMonitor;

public sealed class ExperimentMonitorPanelViewModel : ObservableObject, IDeviceTestPanelViewModel, IPanelCloseViewModel
{
    private readonly object _syncRoot = new();
    private readonly ISnapshotOutputPort<ExperimentMonitorSnapshot> _monitorSnapshotPort;
    private readonly Func<string, string?, string?, Task<string?>> _initializeAsync;
    private readonly Func<Task> _startAsync;
    private readonly Func<Task> _stopAsync;
    private readonly Func<Task> _closeWithoutApplyAsync;
    private readonly IDisplayTicker _displayTicker;
    private ExperimentMonitorSnapshot _pendingSnapshot;
    private ExperimentMonitorSnapshot _displaySnapshot;
    private string _runIndexDraft = $"run-{DateTimeOffset.Now:yyyyMMdd-HHmmss}";
    private string _primaryTargetDraft = "1600";
    private string? _operatorNoteDraft;
    private int _selectedDisplayRateHz = 10;
    private bool _isInitialized;
    private string _initializationStatus = "Not initialized.";
    private string _currentRunStateLabel = "Ready";
    private string _liveSummary = "Ready to initialize.";
    private string _primaryControlSummary = "No active control target.";
    private string _footerHealthLabel = "Healthy";
    private string _footerRunLabel = "Run not started.";
    private IReadOnlyList<ExperimentMonitorItem> _displayItems = Array.Empty<ExperimentMonitorItem>();
    private IReadOnlyList<ExperimentMonitorDeviceSnapshot> _deviceSnapshots = Array.Empty<ExperimentMonitorDeviceSnapshot>();
    private readonly ValueCardItem _runStateCard = new("Run state", "Ready");
    private readonly ValueCardItem _controlCard = new("Primary control", "No active control target.");
    private readonly ValueCardItem _healthCard = new("Health", "Healthy");

    public ExperimentMonitorPanelViewModel(
        ISnapshotOutputPort<ExperimentMonitorSnapshot> monitorSnapshotPort,
        Func<string, string?, string?, Task<string?>> initializeAsync,
        Func<Task> startAsync,
        Func<Task> stopAsync,
        Func<Task> closeWithoutApplyAsync,
        IDisplayTicker? displayTicker = null)
    {
        _monitorSnapshotPort = monitorSnapshotPort ?? throw new ArgumentNullException(nameof(monitorSnapshotPort));
        _initializeAsync = initializeAsync ?? throw new ArgumentNullException(nameof(initializeAsync));
        _startAsync = startAsync ?? throw new ArgumentNullException(nameof(startAsync));
        _stopAsync = stopAsync ?? throw new ArgumentNullException(nameof(stopAsync));
        _closeWithoutApplyAsync = closeWithoutApplyAsync ?? throw new ArgumentNullException(nameof(closeWithoutApplyAsync));
        _displayTicker = displayTicker ?? new DispatcherDisplayTicker(GetInterval(_selectedDisplayRateHz));
        _displayTicker.Tick += HandleDisplayTick;
        _monitorSnapshotPort.Changed += HandleSnapshotChanged;
        _pendingSnapshot = _monitorSnapshotPort.Current ?? new ExperimentMonitorSnapshot();
        _displaySnapshot = _pendingSnapshot;
        ApplyPendingSnapshotForDisplay();
        _displayTicker.Start();
    }

    public string Title => "Experiment Monitor";

    public event EventHandler? CloseRequested;

    public IReadOnlyList<int> DisplayRateOptions { get; } = [5, 10, 15, 20, 25, 30];

    public IReadOnlyList<ValueCardItem> SummaryCards => [_runStateCard, _controlCard, _healthCard];

    public string RunIndexDraft
    {
        get => _runIndexDraft;
        set => SetProperty(ref _runIndexDraft, value);
    }

    public string PrimaryTargetDraft
    {
        get => _primaryTargetDraft;
        set => SetProperty(ref _primaryTargetDraft, value);
    }

    public string? OperatorNoteDraft
    {
        get => _operatorNoteDraft;
        set => SetProperty(ref _operatorNoteDraft, value);
    }

    public int SelectedDisplayRateHz
    {
        get => _selectedDisplayRateHz;
        set
        {
            if (SetProperty(ref _selectedDisplayRateHz, value))
            {
                _displayTicker.Interval = GetInterval(value);
            }
        }
    }

    public bool CanStart => _isInitialized && _displaySnapshot.RunState == RunState.Idle;

    public bool CanStop => _displaySnapshot.RunState == RunState.Running;

    public string InitializationStatus => _initializationStatus;

    public string CurrentRunStateLabel
    {
        get => _currentRunStateLabel;
        private set => SetProperty(ref _currentRunStateLabel, value);
    }

    public string LiveSummary
    {
        get => _liveSummary;
        private set => SetProperty(ref _liveSummary, value);
    }

    public string PrimaryControlSummary
    {
        get => _primaryControlSummary;
        private set => SetProperty(ref _primaryControlSummary, value);
    }

    public string FooterHealthLabel
    {
        get => _footerHealthLabel;
        private set => SetProperty(ref _footerHealthLabel, value);
    }

    public string FooterRunLabel
    {
        get => _footerRunLabel;
        private set => SetProperty(ref _footerRunLabel, value);
    }

    public IReadOnlyList<ExperimentMonitorItem> DisplayItems
    {
        get => _displayItems;
        private set => SetProperty(ref _displayItems, value);
    }

    public IReadOnlyList<ExperimentMonitorDeviceSnapshot> DeviceSnapshots
    {
        get => _deviceSnapshots;
        private set => SetProperty(ref _deviceSnapshots, value);
    }

    public async Task InitializeAsync()
    {
        var summary = await _initializeAsync(
            RunIndexDraft,
            string.IsNullOrWhiteSpace(PrimaryTargetDraft) ? null : PrimaryTargetDraft,
            OperatorNoteDraft).ConfigureAwait(true);
        _isInitialized = true;
        _initializationStatus = summary ?? $"Initialized {RunIndexDraft}.";
        OnPropertyChanged(nameof(InitializationStatus));
        OnPropertyChanged(nameof(CanStart));
        ApplyPendingSnapshotForDisplay();
    }

    public async Task StartAsync()
    {
        await _startAsync().ConfigureAwait(true);
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
    }

    public async Task StopAsync()
    {
        try
        {
            await _stopAsync().ConfigureAwait(true);
        }
        finally
        {
            _isInitialized = false;
            _initializationStatus = "Not initialized.";
            OnPropertyChanged(nameof(InitializationStatus));
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanStop));
        }
    }

    public Task CloseWithoutApplyAsync()
    {
        return _closeWithoutApplyAsync();
    }

    public void Dispose()
    {
        _displayTicker.Tick -= HandleDisplayTick;
        _displayTicker.Dispose();
        _monitorSnapshotPort.Changed -= HandleSnapshotChanged;
    }

    public void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HandleSnapshotChanged(ExperimentMonitorSnapshot snapshot)
    {
        lock (_syncRoot)
        {
            _pendingSnapshot = snapshot;
        }
    }

    private void HandleDisplayTick()
    {
        ApplyPendingSnapshotForDisplay();
    }

    private void ApplyPendingSnapshotForDisplay()
    {
        lock (_syncRoot)
        {
            _displaySnapshot = _pendingSnapshot;
        }

        CurrentRunStateLabel = _displaySnapshot.RunState switch
        {
            RunState.Running => "Running",
            RunState.Stopping => "Stopping",
            _ when _isInitialized => "Initialized",
            _ => "Ready"
        };
        LiveSummary = _displaySnapshot.RunState == RunState.Idle && _isInitialized && _displaySnapshot.StateSummary == "Ready to initialize."
            ? _initializationStatus
            : $"{_displaySnapshot.RunDisplayName}: {_displaySnapshot.StateSummary}";
        PrimaryControlSummary = _displaySnapshot.PrimaryControlSummary;
        FooterHealthLabel = _displaySnapshot.HighestSeverity switch
        {
            ExperimentMonitorSeverity.Alarm => $"Alarms: {_displaySnapshot.AlarmCount}",
            ExperimentMonitorSeverity.Warning => $"Warnings: {_displaySnapshot.WarningCount}",
            _ => "Healthy"
        };
        FooterRunLabel = _displaySnapshot.StateSummary;
        DisplayItems = _displaySnapshot.Items;
        DeviceSnapshots = _displaySnapshot.Devices;
        _runStateCard.Value = CurrentRunStateLabel;
        _controlCard.Value = PrimaryControlSummary;
        _healthCard.Value = FooterHealthLabel;
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
    }

    private static TimeSpan GetInterval(int displayRateHz) =>
        TimeSpan.FromSeconds(1d / Math.Max(1, displayRateHz));
}
