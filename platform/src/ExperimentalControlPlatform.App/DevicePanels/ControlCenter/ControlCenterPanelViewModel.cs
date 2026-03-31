using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Widgets;
using ExperimentalControlPlatform.Devices.ControlCenter;
using ExperimentalControlPlatform.Runtime;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.ControlCenter;

public sealed class ControlCenterPanelViewModel : ObservableObject, IControlCenterPanelViewModel, IPanelCloseViewModel
{
    private static readonly IReadOnlyList<IntegrationPanelLifecycleAction> ConnectedLifecycleActions =
    [
        IntegrationPanelLifecycleAction.Apply,
        IntegrationPanelLifecycleAction.ApplyAndExit
    ];

    private readonly IControlCenterService _service;
    private readonly IDeviceSessionRegistry _sessionRegistry;
    private readonly AsyncRelayCommand _lifecycleActionCommand;
    private readonly ValueCardItem _laserCard = new("Laser cmd", "Unknown");
    private readonly ValueCardItem _puffCard = new("Puff cmd", "Unknown");
    private readonly ValueCardItem _stepCard = new("Last step", "--");
    private readonly ValueCardItem _pulseCard = new("Pulse count", "--");
    private readonly List<ValueCardItem> _statisticsCards;
    private readonly List<ControlCenterDeviceInfo> _devices = [];
    private readonly IReadOnlyList<ControlStateOption> _controlStateOptions =
    [
        new("Off", false),
        new("On", true)
    ];

    private ControlCenterSession? _session;
    private ControlCenterDeviceInfo? _selectedDevice;
    private ControlStateOption _selectedLaserState;
    private ControlStateOption _selectedPuffState;
    private string _stepCountInputDraft = "0";
    private string _currentPrimaryValue = "--";
    private string _commandHistory = "No control-center activity yet.";
    private string _footerConnectionLabel = "Hardware: Disconnected";
    private string _footerDeviceLabel = "No port selected";
    private string _footerLaserLabel = "Laser: Unknown";
    private string _footerPuffLabel = "Puff: Unknown";
    private string _footerSystemStateLabel = "System Ready";
    private bool _isConnected;
    private string _statusMessage = "Control center ready.";
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

    public ControlCenterPanelViewModel(IControlCenterService service, IDeviceSessionRegistry sessionRegistry)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        _selectedLaserState = _controlStateOptions[0];
        _selectedPuffState = _controlStateOptions[0];
        _statisticsCards = [_laserCard, _puffCard, _stepCard, _pulseCard];
        _lifecycleActionCommand = new AsyncRelayCommand(ExecuteLifecycleActionAsync, CanExecuteLifecycleAction, HandleLifecycleException);
        RefreshStatusOutput();
        RefreshDiagnosticsOutput();
        SyncFooter();
    }

    public string Title => "Control Center";

    public event EventHandler? CloseRequested;

    IEnumerable IControlCenterPanelViewModel.DeviceOptions => DeviceOptions;

    public IReadOnlyList<ControlCenterDeviceInfo> DeviceOptions => _devices;

    public object? SelectedDeviceItem
    {
        get => _selectedDevice;
        set
        {
            if (value is ControlCenterDeviceInfo device && SetProperty(ref _selectedDevice, device))
            {
                OnPropertyChanged(nameof(CanToggleConnection));
                SyncFooter();
                RefreshStatusOutput();
            }
        }
    }

    IEnumerable IControlCenterPanelViewModel.ControlStateOptions => ControlStateOptions;

    public IReadOnlyList<ControlStateOption> ControlStateOptions => _controlStateOptions;

    public object? SelectedLaserStateItem
    {
        get => _selectedLaserState;
        set
        {
            if (value is ControlStateOption option && SetProperty(ref _selectedLaserState, option))
            {
                OnPropertyChanged(nameof(CanApplyCommand));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public object? SelectedPuffStateItem
    {
        get => _selectedPuffState;
        set
        {
            if (value is ControlStateOption option && SetProperty(ref _selectedPuffState, option))
            {
                OnPropertyChanged(nameof(CanApplyCommand));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StepCountInputDraft
    {
        get => _stepCountInputDraft;
        set
        {
            if (SetProperty(ref _stepCountInputDraft, value))
            {
                OnPropertyChanged(nameof(CanApplyCommand));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string PrimaryValueLabel => "Last pulse count:";

    public string CurrentPrimaryValue
    {
        get => _currentPrimaryValue;
        private set => SetProperty(ref _currentPrimaryValue, value);
    }

    public string CommandHistory
    {
        get => _commandHistory;
        private set => SetProperty(ref _commandHistory, value);
    }

    public IReadOnlyList<ValueCardItem> StatisticsCards => _statisticsCards;

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

    public string FooterLaserLabel
    {
        get => _footerLaserLabel;
        private set => SetProperty(ref _footerLaserLabel, value);
    }

    public string FooterPuffLabel
    {
        get => _footerPuffLabel;
        private set => SetProperty(ref _footerPuffLabel, value);
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
                OnPropertyChanged(nameof(CanReadPulseCount));
                OnPropertyChanged(nameof(CanApplyCommand));
                OnPropertyChanged(nameof(CanEmergencyStop));
                OnPropertyChanged(nameof(SupportedLifecycleActions));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
                SyncFooter();
                RefreshStatusOutput();
            }
        }
    }

    public bool CanToggleConnection => SelectedDeviceItem is not null;

    public bool CanReadPulseCount => IsConnected;

    public bool CanApplyCommand => IsConnected && TryBuildCommand(out _);

    public bool CanEmergencyStop => IsConnected;

    public bool CanClearLog => !string.IsNullOrWhiteSpace(CommandHistory);

    public string ConnectionToggleLabel => IsConnected ? "Disconnect" : "Connect";

    public PackIconMaterialKind ConnectionToggleIconKind => IsConnected
        ? PackIconMaterialKind.LinkOff
        : PackIconMaterialKind.Link;

    public string DiagnosticsTitle => "Control center diagnostics";

    public string DiagnosticsSubtitle => "Serial command and pulse readback runtime pilot";

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

    public Task RefreshDevicesAsync()
    {
        _devices.Clear();
        _devices.AddRange(_service.ListDevices());
        OnPropertyChanged(nameof(DeviceOptions));

        if (_devices.Count > 0 && (_selectedDevice is null || !_devices.Contains(_selectedDevice)))
        {
            SelectedDeviceItem = _devices[0];
        }

        AppendHistory($"[{DateTimeOffset.Now:HH:mm:ss}] Refreshed control-center ports.");
        return Task.CompletedTask;
    }

    public async Task ConnectAsync()
    {
        if (_selectedDevice is null)
        {
            return;
        }

        var sessionId = new DeviceSessionId("ControlCenter", _selectedDevice.DeviceId);
        var session = _sessionRegistry.GetOrAdd(sessionId, () => new ControlCenterSession(_service, _selectedDevice));
        BindSession(session);
        await session.ConnectAsync().ConfigureAwait(false);
    }

    public async Task DisconnectAsync()
    {
        await DisconnectWithReasonAsync(StopReason.UserRequested("Disconnected control center session.")).ConfigureAwait(false);
    }

    public async Task ReadPulseCountAsync()
    {
        if (_session is null)
        {
            return;
        }

        await _session.ReadFlowTelemetryAsync().ConfigureAwait(false);
    }

    public async Task ApplyCommandAsync()
    {
        if (_session is null || !TryBuildCommand(out var command))
        {
            return;
        }

        await _session.ApplyCommandAsync(command).ConfigureAwait(false);
    }

    public async Task EmergencyStopAsync()
    {
        if (_session is null)
        {
            return;
        }

        await _session.EmergencyStopAsync().ConfigureAwait(false);
    }

    public async Task ApplyAndExitAsync()
    {
        if (_session is null || !TryBuildCommand(out var command))
        {
            return;
        }

        var stagedCommandNote = BuildStagedCommandNote(command);

        await _session.EmergencyStopAsync().ConfigureAwait(true);
        await DisconnectWithReasonAsync(new StopReason("ApplyAndExit", "Applied settings were staged and the control center returned to idle wait state.")).ConfigureAwait(true);
        RunOnUi(() =>
        {
            if (AppliedSettingsOutput is not null)
            {
                var updatedAppliedSettings = AppliedSettingsOutput with
                {
                    NormalizationNotes = AppendNote(AppliedSettingsOutput.NormalizationNotes, stagedCommandNote)
                };
                AppliedSettingsOutput = updatedAppliedSettings;

                if (SessionEndOutput is not null)
                {
                    SessionEndOutput = SessionEndOutput with
                    {
                        AppliedSettingsSnapshot = updatedAppliedSettings
                    };
                }
            }
        });

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ClearLog()
    {
        CommandHistory = string.Empty;
        OnPropertyChanged(nameof(CanClearLog));
    }

    public string GetDiagnosticsSummary()
    {
        return string.Join(Environment.NewLine, new[]
        {
            $"Device: {_selectedDevice?.DisplayName ?? "None"}",
            $"Connected: {IsConnected}",
            $"Laser draft: {_selectedLaserState.DisplayLabel}",
            $"Puff draft: {_selectedPuffState.DisplayLabel}",
            $"Step count draft: {StepCountInputDraft}",
            $"Status: {_statusMessage}",
            $"Last command: {_lastCommand ?? "--"}",
            $"Last transport response: {_lastHardwareResponse ?? "--"}",
            $"Last error: {_lastError ?? "--"}"
        });
    }

    public void Dispose()
    {
        if (_session is null)
        {
            return;
        }

        var session = _session;
        _ = DisposeSessionAsync(session, _sessionRegistry, UnbindSession);
    }

    public Task CloseWithoutApplyAsync() => DisconnectAsync();

    private bool CanExecuteLifecycleAction(object? parameter)
    {
        return parameter switch
        {
            IntegrationPanelLifecycleAction.Apply => CanApplyCommand,
            IntegrationPanelLifecycleAction.ApplyAndExit => CanApplyCommand,
            _ => false
        };
    }

    private async Task ExecuteLifecycleActionAsync(object? parameter)
    {
        switch (parameter)
        {
            case IntegrationPanelLifecycleAction.Apply:
                await ApplyCommandAsync().ConfigureAwait(false);
                return;
            case IntegrationPanelLifecycleAction.ApplyAndExit:
                await ApplyAndExitAsync().ConfigureAwait(false);
                return;
            default:
                return;
        }
    }

    private void HandleLifecycleException(Exception exception)
    {
        AppendHistory($"[{DateTimeOffset.Now:HH:mm:ss}] Apply failed: {exception.Message}");
    }

    private async Task DisconnectWithReasonAsync(StopReason reason)
    {
        if (_session is null)
        {
            return;
        }

        var session = _session;
        try
        {
            await session.DisconnectAsync(reason).ConfigureAwait(false);
            await session.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (_sessionRegistry.Remove(session.SessionId))
            {
                UnbindSession();
            }
            else
            {
                RunOnUi(UnbindSession);
            }
        }
    }

    private void BindSession(ControlCenterSession session)
    {
        if (ReferenceEquals(_session, session))
        {
            return;
        }

        UnbindSession();
        _session = session;
        session.State.Changed += HandleStateChanged;
        session.LaserControl.Changed += HandleLaserControlChanged;
        session.PuffActuation.Changed += HandlePuffActuationChanged;
        session.FlowTelemetry.Changed += HandleFlowTelemetryChanged;
        session.Diagnostics.Changed += HandleDiagnosticsChanged;
        session.AppliedState.Changed += HandleAppliedStateChanged;
        session.LatestPulse.Changed += HandleLatestPulseChanged;
        session.FlowTelemetryReads.Produced += HandleFlowTelemetryProduced;
        session.SessionEnd.Changed += HandleSessionEndChanged;

        if (session.State.Current is not null)
        {
            HandleStateChanged(session.State.Current);
        }

        if (session.Diagnostics.Current is not null)
        {
            HandleDiagnosticsChanged(session.Diagnostics.Current);
        }

        if (session.LaserControl.Current is not null)
        {
            HandleLaserControlChanged(session.LaserControl.Current);
        }

        if (session.PuffActuation.Current is not null)
        {
            HandlePuffActuationChanged(session.PuffActuation.Current);
        }

        if (session.FlowTelemetry.Current is not null)
        {
            HandleFlowTelemetryChanged(session.FlowTelemetry.Current);
        }
    }

    private void UnbindSession()
    {
        if (_session is null)
        {
            return;
        }

        _session.State.Changed -= HandleStateChanged;
        _session.LaserControl.Changed -= HandleLaserControlChanged;
        _session.PuffActuation.Changed -= HandlePuffActuationChanged;
        _session.FlowTelemetry.Changed -= HandleFlowTelemetryChanged;
        _session.Diagnostics.Changed -= HandleDiagnosticsChanged;
        _session.AppliedState.Changed -= HandleAppliedStateChanged;
        _session.LatestPulse.Changed -= HandleLatestPulseChanged;
        _session.FlowTelemetryReads.Produced -= HandleFlowTelemetryProduced;
        _session.SessionEnd.Changed -= HandleSessionEndChanged;
        _session = null;
    }

    private void HandleStateChanged(ControlCenterSessionState state)
    {
        RunOnUi(() =>
        {
            IsConnected = state.Connected;
            _statusMessage = state.StatusMessage;
            FooterSystemStateLabel = state.Busy ? "Busy" : (state.Connected ? "Ready" : "System Ready");
            RefreshStatusOutput();
        });
    }

    private void HandleLaserControlChanged(ControlCenterLaserCapabilityState state)
    {
        RunOnUi(() =>
        {
            _laserCard.Value = FormatState(state.LastCommandedEnabled);
            FooterLaserLabel = $"Laser cmd: {FormatState(state.LastCommandedEnabled)}";
        });
    }

    private void HandlePuffActuationChanged(ControlCenterPuffActuationCapabilityState state)
    {
        RunOnUi(() =>
        {
            _puffCard.Value = FormatState(state.LastCommandedEnabled);
            _stepCard.Value = state.LastStepCount?.ToString(CultureInfo.InvariantCulture) ?? "--";
            FooterPuffLabel = $"Puff cmd: {FormatState(state.LastCommandedEnabled)}";
        });
    }

    private void HandleFlowTelemetryChanged(ControlCenterFlowTelemetryState state)
    {
        RunOnUi(() =>
        {
            _pulseCard.Value = state.LastPulseCount?.ToString(CultureInfo.InvariantCulture) ?? "--";
        });
    }

    private void HandleDiagnosticsChanged(DeviceDiagnosticsSnapshot diagnostics)
    {
        RunOnUi(() =>
        {
            _lastCommand = diagnostics.LastCommand;
            _lastHardwareResponse = diagnostics.LastHardwareResponse;
            _lastError = diagnostics.LastError;
            _lastStateTransition = diagnostics.LastStateTransition;
            _lastValidationResult = diagnostics.LastValidationResult;
            RefreshDiagnosticsOutput();

            if (!string.IsNullOrWhiteSpace(diagnostics.LastStateTransition))
            {
                AppendHistory($"[{DateTimeOffset.Now:HH:mm:ss}] {diagnostics.LastStateTransition}");
            }
            else if (!string.IsNullOrWhiteSpace(diagnostics.LastHardwareResponse))
            {
                AppendHistory($"[{DateTimeOffset.Now:HH:mm:ss}] {diagnostics.LastHardwareResponse}");
            }
        });
    }

    private void HandleAppliedStateChanged(ControlCenterAppliedState? applied)
    {
        if (applied is null)
        {
            return;
        }

        RunOnUi(() =>
        {
            AppliedSettingsOutput = BuildAppliedSettingsSnapshot(
                new ControlCenterCommand(applied.PuffEnabled, applied.LaserEnabled, applied.StepCount),
                applied.TransportNote,
                applied.AppliedAt);
        });
    }

    private void HandleLatestPulseChanged(ControlCenterPulseReadback? pulse)
    {
        if (pulse is null)
        {
            return;
        }

        RunOnUi(() =>
        {
            CurrentPrimaryValue = pulse.PulseCount.ToString(CultureInfo.InvariantCulture);
            _pulseCard.Value = pulse.PulseCount.ToString(CultureInfo.InvariantCulture);
            DataOutput = new IntegrationPanelDataOutput
            {
                Timestamp = pulse.ReceivedAt,
                DeviceId = pulse.DeviceId,
                EndpointId = "flow_telemetry.pulse_count",
                PayloadType = "ControlCenterFlowTelemetryPulseCount",
                PayloadValue = pulse.PulseCount.ToString(CultureInfo.InvariantCulture),
                Units = "pulses",
                SequenceNumber = _session?.State.Current?.PulseSequence ?? 0,
                CaptureRate = 0,
                SourceMode = "snapshot"
            };
        });
    }

    private void HandleFlowTelemetryProduced(ControlCenterPulseReadback pulse)
    {
        RunOnUi(() =>
        {
            AppendHistory($"[{DateTimeOffset.Now:HH:mm:ss}] Pulse readback {pulse.PulseCount} at controller {pulse.ControllerTimestampSeconds:0.###} s");
        });
    }

    private void HandleSessionEndChanged(DeviceSessionEndSnapshot? sessionEnd)
    {
        if (sessionEnd is null)
        {
            return;
        }

        RunOnUi(() =>
        {
            SessionEndOutput = new IntegrationPanelSessionEndOutput
            {
                EndedAt = sessionEnd.EndedAt,
                ExitReason = sessionEnd.ReasonMessage,
                ConnectionClosed = sessionEnd.ConnectionClosed,
                LiveStopped = sessionEnd.LiveStopped,
                AppliedSettingsSnapshot = AppliedSettingsOutput,
                FinalStatus = StatusOutput
            };
        });
    }

    private void RefreshStatusOutput()
    {
        StatusOutput = new IntegrationPanelStatusOutput
        {
            Connected = IsConnected,
            ReadyState = IsConnected ? "Ready" : "Disconnected",
            FaultState = _lastError,
            LiveState = "Idle",
            SelectedEndpoint = _selectedDevice?.DisplayName,
            BackgroundActiveEndpoints = []
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

    private void SyncFooter()
    {
        FooterConnectionLabel = IsConnected ? "Hardware: Connected" : "Hardware: Disconnected";
        FooterDeviceLabel = _selectedDevice?.DisplayName ?? "No port selected";
        if (!IsConnected)
        {
            FooterSystemStateLabel = "System Ready";
        }
    }

    private bool TryBuildCommand(out ControlCenterCommand command)
    {
        if (!int.TryParse(StepCountInputDraft, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stepCount) || stepCount < 0)
        {
            command = new ControlCenterCommand(false, false, 0);
            return false;
        }

        command = new ControlCenterCommand(
            _selectedPuffState.Enabled,
            _selectedLaserState.Enabled,
            stepCount);

        if (!ControlCenterSession.ValidateCommand(command).IsValid)
        {
            command = new ControlCenterCommand(false, false, 0);
            return false;
        }

        return true;
    }

    private void AppendHistory(string line)
    {
        CommandHistory = string.IsNullOrWhiteSpace(CommandHistory)
            ? line
            : $"{line}{Environment.NewLine}{CommandHistory}";
        OnPropertyChanged(nameof(CanClearLog));
    }

    private static string FormatState(bool? state)
    {
        return state switch
        {
            true => "On",
            false => "Off",
            null => "Unknown"
        };
    }

    private static string BuildStagedCommandNote(ControlCenterCommand command)
    {
        return $"Operator staged next system-level command before exit: Laser={(command.LaserEnabled ? "On" : "Off")}, Puff={(command.PuffEnabled ? "On" : "Off")}, StepCount={command.StepCount}.";
    }

    private static string[] AppendNote(IReadOnlyList<string>? notes, string note)
    {
        if (notes is null || notes.Count == 0)
        {
            return [note];
        }

        var result = new string[notes.Count + 1];
        for (var index = 0; index < notes.Count; index++)
        {
            result[index] = notes[index];
        }

        result[^1] = note;
        return result;
    }

    private IntegrationPanelAppliedSettingsOutput BuildAppliedSettingsSnapshot(
        ControlCenterCommand command,
        string note,
        DateTimeOffset? appliedAt = null)
    {
        return new IntegrationPanelAppliedSettingsOutput
        {
            AppliedAt = appliedAt ?? DateTimeOffset.UtcNow,
            DeviceSettings = new Dictionary<string, string?>
            {
                ["Device"] = Title,
                ["Port"] = _selectedDevice?.PortName
            },
            EndpointSettings = new Dictionary<string, string?>
            {
                ["laser.enabled"] = command.LaserEnabled ? "true" : "false",
                ["puff.enabled"] = command.PuffEnabled ? "true" : "false",
                ["puff.step_count"] = command.StepCount.ToString(CultureInfo.InvariantCulture)
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["Connected"] = IsConnected ? "true" : "false"
            },
            NormalizationNotes = [note]
        };
    }

    private static void RunOnUi(Action action)
    {
        if (Application.Current?.Dispatcher is null || Application.Current.Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        Application.Current.Dispatcher.Invoke(action);
    }

    private static async Task DisposeSessionAsync(ControlCenterSession session, IDeviceSessionRegistry sessionRegistry, Action unbind)
    {
        try
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ControlCenterPanel] Dispose session failed: {ex}");
        }
        finally
        {
            sessionRegistry.Remove(session.SessionId);
            RunOnUi(unbind);
        }
    }
}
