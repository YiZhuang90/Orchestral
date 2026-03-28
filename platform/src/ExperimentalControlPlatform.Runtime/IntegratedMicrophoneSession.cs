using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.Audio;

namespace ExperimentalControlPlatform.Runtime;

public sealed class IntegratedMicrophoneSession : IDeviceSession
{
    private readonly IMicrophoneService _service;
    private readonly MicrophoneDeviceInfo _device;
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _liveCancellation;
    private Task? _liveTask;

    public IntegratedMicrophoneSession(IMicrophoneService service, MicrophoneDeviceInfo device)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _device = device ?? throw new ArgumentNullException(nameof(device));
        SessionId = new DeviceSessionId("Microphone", device.DeviceId);
        State = new SnapshotOutputPort<IntegratedMicrophoneSessionState>(new IntegratedMicrophoneSessionState
        {
            DeviceId = device.DeviceId,
            DeviceName = device.DisplayName
        });
        AppliedSettings = new SnapshotOutputPort<MicrophoneCaptureSettings?>();
        Diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot());
        SessionEnd = new SnapshotOutputPort<DeviceSessionEndSnapshot?>();
        LatestFrame = new SnapshotOutputPort<MicrophoneFrame?>();
        Frames = new StreamOutputPort<MicrophoneFrame>();
    }

    public DeviceSessionId SessionId { get; }

    public ISnapshotOutputPort<IntegratedMicrophoneSessionState> State { get; }

    public ISnapshotOutputPort<MicrophoneCaptureSettings?> AppliedSettings { get; }

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics { get; }

    public ISnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEnd { get; }

    public ISnapshotOutputPort<MicrophoneFrame?> LatestFrame { get; }

    public IStreamOutputPort<MicrophoneFrame> Frames { get; }

    public async Task ConnectAsync(MicrophoneCaptureSettings settings, CancellationToken cancellationToken = default)
    {
        var validation = ValidateSettings(settings);
        validation.ThrowIfInvalid();
        PublishSessionEnd(null);
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Connect integrated microphone session",
            LastValidationResult = validation.Summary,
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = $"Connecting to {_device.DisplayName}..."
        });

        try
        {
            var frame = await Task.Run(() => _service.CaptureSnapshot(settings), cancellationToken).ConfigureAwait(false);
            AppliedSettingsPort.Publish(settings);
            PublishFrame(frame, "connect");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = "Integrated microphone connection probe succeeded.",
                LastStateTransition = $"Connected to {_device.DisplayName}",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Connected = true,
                Busy = false,
                LiveReading = false,
                StatusMessage = $"Connected to {_device.DisplayName}.",
                TargetUpdateRateHz = settings.TargetUpdateRateHz,
                WindowMilliseconds = settings.WindowMilliseconds,
                ChannelMode = settings.ChannelMode
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
                Connected = false,
                LiveReading = false,
                StatusMessage = $"Unable to connect microphone: {ex.Message}"
            });
            throw;
        }
    }

    public async Task ReadOnceAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var settings = RequireAppliedSettings();
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Capture microphone snapshot",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = "Capturing microphone snapshot..."
        });

        try
        {
            var frame = await Task.Run(() => _service.CaptureSnapshot(settings), cancellationToken).ConfigureAwait(false);
            PublishFrame(frame, "snapshot");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = "Microphone snapshot captured.",
                LastStateTransition = "Captured microphone snapshot",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = "Microphone snapshot captured."
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
                StatusMessage = $"Snapshot failed: {ex.Message}"
            });
            throw;
        }
    }

    public async Task ApplySettingsAsync(MicrophoneCaptureSettings settings, CancellationToken cancellationToken = default)
    {
        var validation = ValidateSettings(settings);
        validation.ThrowIfInvalid();
        var previousSettings = AppliedSettings.Current;
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Apply integrated microphone settings",
            LastValidationResult = validation.Summary,
            LastError = null
        });

        var wasLive = State.Current!.LiveReading;
        if (wasLive)
        {
            await StopLiveAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!State.Current!.Connected)
        {
            AppliedSettingsPort.Publish(settings);
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastStateTransition = "Staged settings for the next capture"
            });
            PublishState(State.Current! with
            {
                TargetUpdateRateHz = settings.TargetUpdateRateHz,
                WindowMilliseconds = settings.WindowMilliseconds,
                ChannelMode = settings.ChannelMode,
                StatusMessage = "Microphone settings staged for the next capture."
            });
            return;
        }

        try
        {
            var frame = await Task.Run(() => _service.CaptureSnapshot(settings), cancellationToken).ConfigureAwait(false);
            AppliedSettingsPort.Publish(settings);
            PublishFrame(frame, "apply");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = "Applied microphone settings snapshot captured.",
                LastStateTransition = wasLive
                    ? "Applied settings and restarted live microphone read"
                    : "Applied settings",
                LastError = null
            });
            PublishState(State.Current! with
            {
                TargetUpdateRateHz = settings.TargetUpdateRateHz,
                WindowMilliseconds = settings.WindowMilliseconds,
                ChannelMode = settings.ChannelMode,
                StatusMessage = wasLive
                    ? "Microphone settings applied. Restarting live read..."
                    : "Microphone settings applied."
            });

            if (wasLive)
            {
                await StartLiveAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (previousSettings is not null)
            {
                AppliedSettingsPort.Publish(previousSettings);
            }

            var currentState = State.Current!;
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastError = ex.Message
            });
            PublishState(currentState with
            {
                TargetUpdateRateHz = previousSettings?.TargetUpdateRateHz ?? currentState.TargetUpdateRateHz,
                WindowMilliseconds = previousSettings?.WindowMilliseconds ?? currentState.WindowMilliseconds,
                ChannelMode = previousSettings?.ChannelMode ?? currentState.ChannelMode,
                StatusMessage = $"Applying microphone settings failed: {ex.Message}"
            });

            if (wasLive && previousSettings is not null)
            {
                await StartLiveAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    public Task StartLiveAsync()
    {
        EnsureConnected();
        var settings = RequireAppliedSettings();

        lock (_syncRoot)
        {
            if (_liveTask is not null)
            {
                return Task.CompletedTask;
            }

            _liveCancellation = new CancellationTokenSource();
            _liveTask = RunLiveLoopAsync(settings, _liveCancellation.Token);
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Start live microphone read",
            LastStateTransition = "Started live microphone read",
            LastError = null
        });
        PublishState(State.Current! with
        {
            LiveReading = true,
            StatusMessage = "Live microphone read started."
        });
        return Task.CompletedTask;
    }

    public async Task StopLiveAsync(CancellationToken cancellationToken = default)
    {
        Task? liveTask;
        CancellationTokenSource? liveCancellation;
        lock (_syncRoot)
        {
            liveTask = _liveTask;
            liveCancellation = _liveCancellation;
        }

        if (liveTask is null || liveCancellation is null)
        {
            return;
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastStateTransition = "Stopped live microphone read"
        });
        liveCancellation.Cancel();
        await liveTask.ConfigureAwait(false);
    }

    public async Task DisconnectAsync(StopReason? reason = null, CancellationToken cancellationToken = default)
    {
        var stopReason = reason ?? StopReason.UserRequested("Disconnected integrated microphone session.");
        var liveWasActive = State.Current!.LiveReading;
        await StopLiveAsync(cancellationToken).ConfigureAwait(false);

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastStateTransition = "Disconnected microphone",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Connected = false,
            LiveReading = false,
            Busy = false,
            StatusMessage = "Disconnected."
        });
        PublishSessionEnd(new DeviceSessionEndSnapshot
        {
            EndedAt = DateTimeOffset.UtcNow,
            ReasonCode = stopReason.Code,
            ReasonMessage = stopReason.Message,
            ConnectionClosed = true,
            LiveStopped = liveWasActive
        });
    }

    public ValueTask StopAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        return new ValueTask(DisconnectAsync(reason, cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        await StopLiveAsync().ConfigureAwait(false);
        lock (_syncRoot)
        {
            _liveCancellation?.Dispose();
            _liveCancellation = null;
            _liveTask = null;
        }
    }

    private SnapshotOutputPort<MicrophoneCaptureSettings?> AppliedSettingsPort => (SnapshotOutputPort<MicrophoneCaptureSettings?>)AppliedSettings;

    private SnapshotOutputPort<DeviceDiagnosticsSnapshot> DiagnosticsPort => (SnapshotOutputPort<DeviceDiagnosticsSnapshot>)Diagnostics;

    private SnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEndPort => (SnapshotOutputPort<DeviceSessionEndSnapshot?>)SessionEnd;

    private SnapshotOutputPort<IntegratedMicrophoneSessionState> StatePort => (SnapshotOutputPort<IntegratedMicrophoneSessionState>)State;

    private SnapshotOutputPort<MicrophoneFrame?> LatestFramePort => (SnapshotOutputPort<MicrophoneFrame?>)LatestFrame;

    private StreamOutputPort<MicrophoneFrame> FramesPort => (StreamOutputPort<MicrophoneFrame>)Frames;

    private async Task RunLiveLoopAsync(MicrophoneCaptureSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _service.StreamFramesAsync(settings, frame =>
            {
                PublishFrame(frame, "live");
                PublishDiagnostics(Diagnostics.Current! with
                {
                    LastHardwareResponse = "Live microphone frame received.",
                    LastError = null
                });
                return Task.CompletedTask;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastError = ex.Message
            });
            PublishState(State.Current! with
            {
                StatusMessage = $"Live microphone read failed: {ex.Message}"
            });
        }
        finally
        {
            lock (_syncRoot)
            {
                _liveCancellation?.Dispose();
                _liveCancellation = null;
                _liveTask = null;
            }

            var currentState = State.Current!;
            PublishState(currentState with
            {
                LiveReading = false,
                StatusMessage = currentState.Connected ? "Live microphone read stopped." : currentState.StatusMessage
            });
        }
    }

    private void PublishFrame(MicrophoneFrame frame, string sourceMode)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var currentState = State.Current!;
        var nextState = currentState with
        {
            Connected = true,
            Busy = false,
            LastFrameCapturedAt = timestamp,
            FrameSequence = currentState.FrameSequence + 1,
            LastSourceMode = sourceMode
        };
        StatePort.Publish(nextState);
        LatestFramePort.Publish(frame);
        FramesPort.Publish(frame);
    }

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot)
    {
        DiagnosticsPort.Publish(snapshot);
    }

    private void PublishSessionEnd(DeviceSessionEndSnapshot? snapshot)
    {
        SessionEndPort.Publish(snapshot);
    }

    private void PublishState(IntegratedMicrophoneSessionState state)
    {
        StatePort.Publish(state);
    }

    public SessionValidationResult ValidateSettings(MicrophoneCaptureSettings settings) =>
        ValidateSettings(_device.DeviceId, settings);

    public static SessionValidationResult ValidateSettings(string expectedDeviceId, MicrophoneCaptureSettings settings)
    {
        var issues = new List<SessionValidationIssue>();
        if (!string.Equals(settings.DeviceId, expectedDeviceId, StringComparison.Ordinal))
        {
            issues.Add(new SessionValidationIssue(
                "DeviceId",
                $"Settings target '{settings.DeviceId}' do not match session device '{expectedDeviceId}'."));
        }

        if (settings.TargetUpdateRateHz <= 0)
        {
            issues.Add(new SessionValidationIssue(
                "TargetUpdateRateHz",
                "Target update rate must be positive."));
        }

        if (settings.WindowMilliseconds <= 0)
        {
            issues.Add(new SessionValidationIssue(
                "WindowMilliseconds",
                "Window must be positive."));
        }

        return SessionValidationResult.FromIssues("Validated microphone settings.", issues);
    }

    private void EnsureConnected()
    {
        if (!State.Current!.Connected)
        {
            throw new InvalidOperationException("Microphone session is not connected.");
        }
    }

    private MicrophoneCaptureSettings RequireAppliedSettings()
    {
        return AppliedSettings.Current ?? throw new InvalidOperationException("Microphone session has no applied capture settings.");
    }
}
