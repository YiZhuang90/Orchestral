using System;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.ControlCenter;

namespace ExperimentalControlPlatform.Runtime;

public sealed class ControlCenterSession : IDeviceSession
{
    private readonly IControlCenterService _service;
    private readonly ControlCenterDeviceInfo _device;
    private readonly object _syncRoot = new();
    private IControlCenterConnection? _connection;

    public ControlCenterSession(IControlCenterService service, ControlCenterDeviceInfo device)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _device = device ?? throw new ArgumentNullException(nameof(device));
        SessionId = new DeviceSessionId("ControlCenter", device.DeviceId);
        State = new SnapshotOutputPort<ControlCenterSessionState>(new ControlCenterSessionState
        {
            DeviceId = device.DeviceId,
            DeviceName = device.DisplayName,
            StatusMessage = "Control center session ready."
        });
        AppliedState = new SnapshotOutputPort<ControlCenterAppliedState?>();
        Diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot());
        SessionEnd = new SnapshotOutputPort<DeviceSessionEndSnapshot?>();
        LatestPulse = new SnapshotOutputPort<ControlCenterPulseReadback?>();
        PulseReads = new StreamOutputPort<ControlCenterPulseReadback>();
    }

    public DeviceSessionId SessionId { get; }

    public ISnapshotOutputPort<ControlCenterSessionState> State { get; }

    public ISnapshotOutputPort<ControlCenterAppliedState?> AppliedState { get; }

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics { get; }

    public ISnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEnd { get; }

    public ISnapshotOutputPort<ControlCenterPulseReadback?> LatestPulse { get; }

    public IStreamOutputPort<ControlCenterPulseReadback> PulseReads { get; }

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

    public async Task ApplyCommandAsync(ControlCenterCommand command, CancellationToken cancellationToken = default)
    {
        ValidateCommand(command);
        var connection = RequireConnection();

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Send control center command",
            LastValidationResult = "Validated control center command.",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = "Writing control center command..."
        });

        try
        {
            await Task.Run(() => _service.SendCommand(connection, command), cancellationToken).ConfigureAwait(false);
            var appliedAt = DateTimeOffset.UtcNow;
            AppliedStatePort.Publish(new ControlCenterAppliedState(
                command.PuffEnabled,
                command.LaserEnabled,
                command.StepCount,
                appliedAt,
                "Serial transport write succeeded; hardware acknowledgement not available."));
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = "Control center command written to serial transport.",
                LastStateTransition = "Sent control center command",
                LastError = null
            });

            var currentState = State.Current!;
            PublishState(currentState with
            {
                Busy = false,
                LastCommandedPuffEnabled = command.PuffEnabled,
                LastCommandedLaserEnabled = command.LaserEnabled,
                LastStepCount = command.StepCount,
                CommandSequence = currentState.CommandSequence + 1,
                StatusMessage = "Control center command written to transport."
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
                StatusMessage = $"Control center command failed: {ex.Message}"
            });
            throw;
        }
    }

    public async Task ReadPulseCountAsync(CancellationToken cancellationToken = default)
    {
        var connection = RequireConnection();
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Read control center pulse count",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = "Reading control center pulse count..."
        });

        try
        {
            var readback = await Task.Run(() => _service.ReadPulseCount(connection), cancellationToken).ConfigureAwait(false);
            LatestPulsePort.Publish(readback);
            PulseReadsPort.Publish(readback);
            var currentState = State.Current!;
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = $"Pulse readback received: {readback.PulseCount}.",
                LastStateTransition = "Captured control center pulse readback",
                LastError = null
            });
            PublishState(currentState with
            {
                Busy = false,
                LastPulseCount = readback.PulseCount,
                LastControllerTimestampSeconds = readback.ControllerTimestampSeconds,
                LastPulseCapturedAt = readback.ReceivedAt,
                PulseSequence = currentState.PulseSequence + 1,
                StatusMessage = "Control center pulse count captured."
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
                StatusMessage = $"Pulse readback failed: {ex.Message}"
            });
            throw;
        }
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
        await DisconnectAsync(StopReason.UserRequested("Disposed control center session.")).ConfigureAwait(false);
    }

    private SnapshotOutputPort<ControlCenterAppliedState?> AppliedStatePort => (SnapshotOutputPort<ControlCenterAppliedState?>)AppliedState;

    private SnapshotOutputPort<DeviceDiagnosticsSnapshot> DiagnosticsPort => (SnapshotOutputPort<DeviceDiagnosticsSnapshot>)Diagnostics;

    private SnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEndPort => (SnapshotOutputPort<DeviceSessionEndSnapshot?>)SessionEnd;

    private SnapshotOutputPort<ControlCenterSessionState> StatePort => (SnapshotOutputPort<ControlCenterSessionState>)State;

    private SnapshotOutputPort<ControlCenterPulseReadback?> LatestPulsePort => (SnapshotOutputPort<ControlCenterPulseReadback?>)LatestPulse;

    private StreamOutputPort<ControlCenterPulseReadback> PulseReadsPort => (StreamOutputPort<ControlCenterPulseReadback>)PulseReads;

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot)
    {
        DiagnosticsPort.Publish(snapshot);
    }

    private void PublishSessionEnd(DeviceSessionEndSnapshot? snapshot)
    {
        SessionEndPort.Publish(snapshot);
    }

    private void PublishState(ControlCenterSessionState state)
    {
        StatePort.Publish(state);
    }

    private IControlCenterConnection RequireConnection()
    {
        lock (_syncRoot)
        {
            return _connection ?? throw new InvalidOperationException("Control center session is not connected.");
        }
    }

    private static void ValidateCommand(ControlCenterCommand command)
    {
        if (command.StepCount < 0)
        {
            throw new InvalidOperationException("Step count must be zero or greater.");
        }
    }
}
