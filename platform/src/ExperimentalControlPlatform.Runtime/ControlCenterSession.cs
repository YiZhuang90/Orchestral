using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.ControlCenter;

namespace ExperimentalControlPlatform.Runtime;

public sealed class ControlCenterSession : IDeviceSession
{
    private readonly IControlCenterService _service;
    private readonly ControlCenterDeviceInfo _device;
    private readonly object _syncRoot = new();
    private readonly SnapshotOutputPort<ControlCenterSessionState> _state;
    private readonly SnapshotOutputPort<ControlCenterAppliedState?> _appliedState;
    private readonly SnapshotOutputPort<DeviceDiagnosticsSnapshot> _diagnostics;
    private readonly SnapshotOutputPort<DeviceSessionEndSnapshot?> _sessionEnd;
    private readonly SnapshotOutputPort<ControlCenterPulseReadback?> _latestPulse;
    private readonly StreamOutputPort<ControlCenterPulseReadback> _flowTelemetryReads;
    private readonly SnapshotOutputPort<ControlCenterLaserCapabilityState> _laserControl;
    private readonly SnapshotOutputPort<ControlCenterPuffActuationCapabilityState> _puffActuation;
    private readonly SnapshotOutputPort<ControlCenterFlowTelemetryState> _flowTelemetry;
    private IControlCenterConnection? _connection;

    public ControlCenterSession(IControlCenterService service, ControlCenterDeviceInfo device)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _device = device ?? throw new ArgumentNullException(nameof(device));
        SessionId = new DeviceSessionId("ControlCenter", device.DeviceId);
        _state = new SnapshotOutputPort<ControlCenterSessionState>(new ControlCenterSessionState
        {
            DeviceId = device.DeviceId,
            DeviceName = device.DisplayName,
            StatusMessage = "Control center session ready."
        });
        _appliedState = new SnapshotOutputPort<ControlCenterAppliedState?>();
        _diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot());
        _sessionEnd = new SnapshotOutputPort<DeviceSessionEndSnapshot?>();
        _latestPulse = new SnapshotOutputPort<ControlCenterPulseReadback?>();
        _flowTelemetryReads = new StreamOutputPort<ControlCenterPulseReadback>();
        _laserControl = new SnapshotOutputPort<ControlCenterLaserCapabilityState>(new ControlCenterLaserCapabilityState());
        _puffActuation = new SnapshotOutputPort<ControlCenterPuffActuationCapabilityState>(new ControlCenterPuffActuationCapabilityState());
        _flowTelemetry = new SnapshotOutputPort<ControlCenterFlowTelemetryState>(new ControlCenterFlowTelemetryState());
    }

    public DeviceSessionId SessionId { get; }

    public ISnapshotOutputPort<ControlCenterSessionState> State => _state;

    public ISnapshotOutputPort<ControlCenterAppliedState?> AppliedState => _appliedState;

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics => _diagnostics;

    public ISnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEnd => _sessionEnd;

    public ISnapshotOutputPort<ControlCenterPulseReadback?> LatestPulse => _latestPulse;

    [Obsolete("Use FlowTelemetryReads to consume the explicit flow-telemetry capability surface.")]
    public IStreamOutputPort<ControlCenterPulseReadback> PulseReads => _flowTelemetryReads;

    public ISnapshotOutputPort<ControlCenterLaserCapabilityState> LaserControl => _laserControl;

    public ISnapshotOutputPort<ControlCenterPuffActuationCapabilityState> PuffActuation => _puffActuation;

    public ISnapshotOutputPort<ControlCenterFlowTelemetryState> FlowTelemetry => _flowTelemetry;

    public IStreamOutputPort<ControlCenterPulseReadback> FlowTelemetryReads => _flowTelemetryReads;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        PublishSessionEnd(null);
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Connect control center session",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = $"Connecting to {_device.DisplayName}..."
        });

        try
        {
            var connection = await Task.Run(() => _service.Open(_device), cancellationToken).ConfigureAwait(false);
            lock (_syncRoot)
            {
                _connection?.Dispose();
                _connection = connection;
            }

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = $"Opened {_device.PortName} at {ControlCenterProtocol.DefaultBaudRate} baud.",
                LastStateTransition = $"Connected to {_device.DisplayName}",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Connected = true,
                Busy = false,
                StatusMessage = $"Connected to {_device.DisplayName}."
            });
            PublishLaserControl(LaserControl.Current! with
            {
                StatusMessage = "Laser control connected and ready."
            });
            PublishPuffActuation(PuffActuation.Current! with
            {
                StatusMessage = "Puff actuation connected and ready."
            });
            PublishFlowTelemetry(FlowTelemetry.Current! with
            {
                StatusMessage = "Awaiting flow telemetry."
            });
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastError = ex.Message
            });
            PublishState(State.Current! with
            {
                Connected = false,
                Busy = false,
                StatusMessage = $"Unable to connect control center: {ex.Message}"
            });
            throw;
        }
    }

    public Task ApplyCommandAsync(ControlCenterCommand command, CancellationToken cancellationToken = default)
    {
        var connection = RequireConnection();
        return WriteCommandAsync(
            connection,
            command,
            "Send control center capability command",
            "Control-center capability command written to serial transport.",
            "Sent control-center capability command",
            "Control-center capability command written to transport.",
            "Serial transport write succeeded; hardware acknowledgement not available.",
            cancellationToken);
    }

    public Task ApplyLaserControlAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var currentState = State.Current!;
        var command = new ControlCenterCommand(
            PuffEnabled: currentState.LastCommandedPuffEnabled ?? false,
            LaserEnabled: enabled,
            StepCount: currentState.LastStepCount ?? 0);
        var connection = RequireConnection();
        return WriteCommandAsync(
            connection,
            command,
            "Send laser control command",
            "Laser control command written to serial transport.",
            "Sent laser control command",
            "Laser control command written to transport.",
            "Laser control command written to serial transport; hardware acknowledgement not available.",
            cancellationToken,
            validationSummary: BuildScopedCommandValidationSummary("laser control", currentState));
    }

    public Task ApplyPuffActuationAsync(bool enabled, int stepCount, CancellationToken cancellationToken = default)
    {
        var currentState = State.Current!;
        var command = new ControlCenterCommand(
            PuffEnabled: enabled,
            LaserEnabled: currentState.LastCommandedLaserEnabled ?? false,
            StepCount: stepCount);
        var connection = RequireConnection();
        return WriteCommandAsync(
            connection,
            command,
            "Send puff actuation command",
            "Puff actuation command written to serial transport.",
            "Sent puff actuation command",
            "Puff actuation command written to transport.",
            "Puff actuation command written to serial transport; hardware acknowledgement not available.",
            cancellationToken,
            validationSummary: BuildScopedCommandValidationSummary("puff actuation", currentState));
    }

    [Obsolete("Use ReadFlowTelemetryAsync to read the explicit flow-telemetry capability surface.")]
    public Task ReadPulseCountAsync(CancellationToken cancellationToken = default) => ReadFlowTelemetryAsync(cancellationToken);

    public async Task ReadFlowTelemetryAsync(CancellationToken cancellationToken = default)
    {
        var connection = RequireConnection();
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Read flow telemetry",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = "Reading control-center flow telemetry..."
        });

        try
        {
            var readback = await Task.Run(() => _service.ReadPulseCount(connection), cancellationToken).ConfigureAwait(false);
            _latestPulse.Publish(readback);
            _flowTelemetryReads.Publish(readback);
            var currentState = State.Current!;
            var updatedState = currentState with
            {
                Busy = false,
                LastPulseCount = readback.PulseCount,
                LastControllerTimestampSeconds = readback.ControllerTimestampSeconds,
                LastPulseCapturedAt = readback.ReceivedAt,
                PulseSequence = currentState.PulseSequence + 1,
                StatusMessage = "Control-center flow telemetry captured."
            };

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = $"Flow telemetry pulse readback received: {readback.PulseCount}.",
                LastStateTransition = "Captured control-center flow telemetry",
                LastError = null
            });
            PublishState(updatedState);
            PublishFlowTelemetry(new ControlCenterFlowTelemetryState
            {
                LastPulseCount = readback.PulseCount,
                LastControllerTimestampSeconds = readback.ControllerTimestampSeconds,
                LastObservedAtUtc = readback.ReceivedAt,
                PulseSequence = updatedState.PulseSequence,
                StatusMessage = "Flow telemetry pulse readback captured."
            });
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastError = ex.Message
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = $"Flow telemetry read failed: {ex.Message}"
            });
            throw;
        }
    }

    public Task EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        IControlCenterConnection? connection;
        lock (_syncRoot)
        {
            connection = _connection;
        }

        if (connection is null)
        {
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastCommand = "Emergency stop control center",
                LastValidationResult = "Skipped emergency stop because the session is already disconnected.",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = "Emergency stop ignored because the control center is disconnected."
            });
            return Task.CompletedTask;
        }

        var safeCommand = new ControlCenterCommand(PuffEnabled: false, LaserEnabled: false, StepCount: 0);
        return WriteCommandAsync(
            connection,
            safeCommand,
            "Emergency stop control center",
            "Emergency-stop safe command written to serial transport.",
            "Applied emergency-stop safe command",
            "Emergency stop applied; control center is idle.",
            "Emergency-stop safe command written to serial transport.",
            cancellationToken,
            validationSummary: "Validated emergency-stop safe command.");
    }

    public async Task DisconnectAsync(StopReason? reason = null, CancellationToken cancellationToken = default)
    {
        var stopReason = reason ?? StopReason.UserRequested("Disconnected control center session.");
        IControlCenterConnection? connection;
        lock (_syncRoot)
        {
            connection = _connection;
            _connection = null;
        }

        if (connection is not null)
        {
            await Task.Run(connection.Dispose, cancellationToken).ConfigureAwait(false);
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastStateTransition = "Disconnected control center",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Connected = false,
            Busy = false,
            StatusMessage = "Disconnected."
        });
        PublishLaserControl(LaserControl.Current! with
        {
            StatusMessage = "Laser control disconnected."
        });
        PublishPuffActuation(PuffActuation.Current! with
        {
            StatusMessage = "Puff actuation disconnected."
        });
        PublishFlowTelemetry(FlowTelemetry.Current! with
        {
            StatusMessage = "Flow telemetry disconnected."
        });
        PublishSessionEnd(new DeviceSessionEndSnapshot
        {
            EndedAt = DateTimeOffset.UtcNow,
            ReasonCode = stopReason.Code,
            ReasonMessage = stopReason.Message,
            ConnectionClosed = true,
            LiveStopped = false
        });
    }

    public ValueTask StopAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        return new ValueTask(DisconnectAsync(reason, cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        lock (_syncRoot)
        {
            if (_connection is null)
            {
                return;
            }
        }

        await DisconnectAsync(StopReason.UserRequested("Disposed control center session.")).ConfigureAwait(false);
    }

    private async Task WriteCommandAsync(
        IControlCenterConnection connection,
        ControlCenterCommand command,
        string diagnosticsCommand,
        string hardwareResponse,
        string stateTransition,
        string statusMessage,
        string transportNote,
        CancellationToken cancellationToken,
        string? validationSummary = null)
    {
        var validation = ValidateCommand(command);
        validation.ThrowIfInvalid();

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = diagnosticsCommand,
            LastValidationResult = validationSummary ?? validation.Summary,
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = "Writing control-center capability command..."
        });

        try
        {
            await Task.Run(() => _service.SendCommand(connection, command), cancellationToken).ConfigureAwait(false);
            var appliedAt = DateTimeOffset.UtcNow;
            _appliedState.Publish(new ControlCenterAppliedState(
                command.PuffEnabled,
                command.LaserEnabled,
                command.StepCount,
                appliedAt,
                transportNote));
            var currentState = State.Current!;
            var updatedState = currentState with
            {
                Busy = false,
                LastCommandedPuffEnabled = command.PuffEnabled,
                LastCommandedLaserEnabled = command.LaserEnabled,
                LastStepCount = command.StepCount,
                CommandSequence = currentState.CommandSequence + 1,
                StatusMessage = statusMessage
            };

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = hardwareResponse,
                LastStateTransition = stateTransition,
                LastError = null
            });
            PublishState(updatedState);
            PublishLaserControl(new ControlCenterLaserCapabilityState
            {
                LastCommandedEnabled = command.LaserEnabled,
                LastCommandedAtUtc = appliedAt,
                CommandSequence = updatedState.CommandSequence,
                StatusMessage = $"Laser control command {(command.LaserEnabled ? "enabled" : "disabled")}."
            });
            PublishPuffActuation(new ControlCenterPuffActuationCapabilityState
            {
                LastCommandedEnabled = command.PuffEnabled,
                LastStepCount = command.StepCount,
                LastCommandedAtUtc = appliedAt,
                CommandSequence = updatedState.CommandSequence,
                StatusMessage = $"Puff actuation command {(command.PuffEnabled ? "enabled" : "disabled")} with step count {command.StepCount}."
            });
            PublishFlowTelemetry(FlowTelemetry.Current! with
            {
                StatusMessage = "Capability command written; flow telemetry is awaiting refresh."
            });
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastError = ex.Message
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = $"Control-center capability command failed: {ex.Message}"
            });
            throw;
        }
    }

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot)
    {
        _diagnostics.Publish(snapshot);
    }

    private void PublishSessionEnd(DeviceSessionEndSnapshot? snapshot)
    {
        _sessionEnd.Publish(snapshot);
    }

    private void PublishState(ControlCenterSessionState state)
    {
        _state.Publish(state);
    }

    private void PublishLaserControl(ControlCenterLaserCapabilityState state)
    {
        _laserControl.Publish(state);
    }

    private void PublishPuffActuation(ControlCenterPuffActuationCapabilityState state)
    {
        _puffActuation.Publish(state);
    }

    private void PublishFlowTelemetry(ControlCenterFlowTelemetryState state)
    {
        _flowTelemetry.Publish(state);
    }

    private IControlCenterConnection RequireConnection()
    {
        lock (_syncRoot)
        {
            return _connection ?? throw new InvalidOperationException("Control center session is not connected.");
        }
    }

    public static SessionValidationResult ValidateCommand(ControlCenterCommand command)
    {
        var issues = new List<SessionValidationIssue>();
        if (command.StepCount < 0)
        {
            issues.Add(new SessionValidationIssue(
                "StepCount",
                "Step count must be zero or greater."));
        }

        return SessionValidationResult.FromIssues("Validated control center capability command.", issues);
    }

    private static string BuildScopedCommandValidationSummary(string capabilityName, ControlCenterSessionState state)
    {
        if (state.CommandSequence == 0)
        {
            return $"Validated {capabilityName} command; no prior session command exists, so unspecified capability fields defaulted to safe off/0 values.";
        }

        return $"Validated {capabilityName} command; unspecified capability fields were reconstructed from the last session command rather than hardware acknowledgement.";
    }
}
