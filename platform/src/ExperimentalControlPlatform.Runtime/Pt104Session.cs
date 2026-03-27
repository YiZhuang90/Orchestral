using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public sealed class Pt104Session : IDeviceSession
{
    private static readonly int[] ChannelNumbers = [1, 2, 3, 4];
    private readonly IPt104RuntimeDriver _driver;
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _liveCancellation;
    private Task? _liveTask;

    public Pt104Session(IPt104RuntimeDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        SessionId = new DeviceSessionId("Pt104", "usb-default");
        State = new SnapshotOutputPort<Pt104SessionState>(new Pt104SessionState
        {
            DeviceId = "Logger-Offline",
            DeviceName = "PT-104",
            Channels = CreateDefaultChannels()
        });
        AppliedSettings = new SnapshotOutputPort<Pt104ChannelConfiguration?>();
        Diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot());
        SessionEnd = new SnapshotOutputPort<DeviceSessionEndSnapshot?>();
        LatestReading = new SnapshotOutputPort<Pt104Reading?>();
        Readings = new StreamOutputPort<Pt104Reading>();
    }

    public DeviceSessionId SessionId { get; }

    public ISnapshotOutputPort<Pt104SessionState> State { get; }

    public ISnapshotOutputPort<Pt104ChannelConfiguration?> AppliedSettings { get; }

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics { get; }

    public ISnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEnd { get; }

    public ISnapshotOutputPort<Pt104Reading?> LatestReading { get; }

    public IStreamOutputPort<Pt104Reading> Readings { get; }

    public async Task ConnectAsync(Pt104ChannelConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(configuration);
        PublishSessionEnd(null);
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = $"Connect channel {configuration.Channel}",
            LastError = null
        });
        PublishState(State.Current! with { Busy = true, StatusMessage = "Connecting PT-104..." });

        try
        {
            await Task.Run(() => _driver.Connect(configuration), cancellationToken).ConfigureAwait(false);
            await VerifyChannelAvailabilityAsync(cancellationToken).ConfigureAwait(false);
            await Task.Run(() => _driver.ApplySettings(configuration), cancellationToken).ConfigureAwait(false);
            AppliedSettingsPort.Publish(configuration);

            var deviceId = _driver.ConnectedDeviceId ?? "PT-104 connected";
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = $"Connected to {deviceId}.",
                LastStateTransition = "Disconnected -> Connected",
                LastError = null
            });
            PublishState(State.Current! with
            {
                DeviceId = deviceId,
                Connected = true,
                Busy = false,
                StatusMessage = $"Connected. {State.Current!.Channels.Count(channel => channel.Value.Available)} active channel(s) verified."
            });
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with
            {
                Connected = false,
                Busy = false,
                StatusMessage = ex.Message
            });
            throw;
        }
    }

    public async Task ApplySettingsAsync(Pt104ChannelConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(configuration);
        EnsureConnected();
        if (AnyLiveChannels())
        {
            throw new InvalidOperationException("Cannot apply PT-104 settings while any channel is live.");
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = $"Apply settings for channel {configuration.Channel}",
            LastValidationResult = "Validated PT-104 channel settings.",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = $"Applying settings for channel {configuration.Channel}..."
        });

        try
        {
            await Task.Run(() => _driver.ApplySettings(configuration), cancellationToken).ConfigureAwait(false);
            AppliedSettingsPort.Publish(configuration);
            UpdateChannelState(configuration.Channel, channel => channel with
            {
                MeasurementMode = configuration.MeasurementMode,
                WireCount = configuration.WireCount,
                MainsFrequencyHz = configuration.MainsFrequencyHz,
                FilteredRead = configuration.FilteredRead,
                Available = true
            });

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = $"Applied settings to channel {configuration.Channel}.",
                LastStateTransition = $"Applied settings for channel {configuration.Channel}",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = $"Applied settings for channel {configuration.Channel}."
            });
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = ex.Message
            });
            throw;
        }
    }

    public async Task ReadOnceAsync(Pt104ChannelConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(configuration);
        EnsureConnected();
        if (AnyLiveChannels())
        {
            throw new InvalidOperationException("Cannot read a PT-104 channel once while any live channel is active.");
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = $"Read once on channel {configuration.Channel}",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = $"Reading channel {configuration.Channel}..."
        });

        try
        {
            await Task.Run(() => _driver.ApplySettings(configuration), cancellationToken).ConfigureAwait(false);
            AppliedSettingsPort.Publish(configuration);
            var reading = await Task.Run(
                () => _driver.ReadTemperatureC(configuration.Channel, configuration.FilteredRead),
                cancellationToken).ConfigureAwait(false);
            PublishReading(configuration.Channel, reading, "ReadOnce");
            UpdateChannelState(configuration.Channel, channel => channel with
            {
                Available = true,
                MeasurementMode = configuration.MeasurementMode,
                WireCount = configuration.WireCount,
                MainsFrequencyHz = configuration.MainsFrequencyHz,
                FilteredRead = configuration.FilteredRead
            });
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = $"Read {reading:F3} C from channel {configuration.Channel}.",
                LastStateTransition = $"Read channel {configuration.Channel}",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = $"Read channel {configuration.Channel} successfully."
            });
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = ex.Message
            });
            throw;
        }
    }

    public Task StartLiveAsync(int channel, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var channelState = RequireChannel(channel);
        UpdateChannelState(channel, state => state with { LiveReading = true, Available = true });

        lock (_syncRoot)
        {
            if (_liveTask is null)
            {
                _liveCancellation = new CancellationTokenSource();
                _liveTask = RunLiveReadLoopAsync(_liveCancellation.Token);
            }
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = $"Start live read on channel {channel}",
            LastStateTransition = $"Channel {channel} idle -> live",
            LastError = null
        });
        PublishState(State.Current! with { StatusMessage = $"Live read started on channel {channel}." });
        return Task.CompletedTask;
    }

    public async Task StopLiveAsync(int channel, CancellationToken cancellationToken = default)
    {
        if (!State.Current!.Channels.TryGetValue(channel, out var channelState) || !channelState.LiveReading)
        {
            return;
        }

        UpdateChannelState(channel, state => state with { LiveReading = false });
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = $"Stop live read on channel {channel}",
            LastHardwareResponse = $"Live acquisition stopped on channel {channel}.",
            LastStateTransition = $"Channel {channel} live -> idle"
        });

        if (!AnyLiveChannels())
        {
            CancellationTokenSource? liveCancellation;
            Task? liveTask;
            lock (_syncRoot)
            {
                liveCancellation = _liveCancellation;
                liveTask = _liveTask;
            }

            if (liveCancellation is not null && liveTask is not null)
            {
                liveCancellation.Cancel();
                await liveTask.ConfigureAwait(false);
            }
        }

        PublishState(State.Current! with
        {
            StatusMessage = AnyLiveChannels()
                ? $"Live read stopped on channel {channel}."
                : "Live read stopped."
        });
    }

    public async Task DisconnectAsync(StopReason? reason = null, CancellationToken cancellationToken = default)
    {
        if (!State.Current!.Connected && !_driver.IsConnected)
        {
            return;
        }

        var stopReason = reason ?? StopReason.UserRequested("Disconnected PT-104 session.");
        var hadLiveReads = AnyLiveChannels();
        await StopAllLiveAsync(cancellationToken).ConfigureAwait(false);

        await Task.Run(() => _driver.Disconnect(), cancellationToken).ConfigureAwait(false);
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastHardwareResponse = "PT-104 connection closed.",
            LastStateTransition = "Connected -> Disconnected",
            LastError = null
        });
        PublishState(new Pt104SessionState
        {
            DeviceId = "Logger-Offline",
            DeviceName = "PT-104",
            Connected = false,
            Busy = false,
            StatusMessage = "Disconnected.",
            Channels = State.Current!.Channels.ToDictionary(
                pair => pair.Key,
                pair => pair.Value with { LiveReading = false })
        });
        PublishSessionEnd(new DeviceSessionEndSnapshot
        {
            EndedAt = DateTimeOffset.UtcNow,
            ReasonCode = stopReason.Code,
            ReasonMessage = stopReason.Message,
            ConnectionClosed = true,
            LiveStopped = hadLiveReads
        });
    }

    public ValueTask StopAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        return new ValueTask(DisconnectAsync(reason, cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        if (State.Current!.Connected || _driver.IsConnected)
        {
            await DisconnectAsync(StopReason.UserRequested("Disposed PT-104 session.")).ConfigureAwait(false);
        }

        await StopAllLiveAsync().ConfigureAwait(false);
        _driver.Dispose();
    }

    private SnapshotOutputPort<Pt104ChannelConfiguration?> AppliedSettingsPort => (SnapshotOutputPort<Pt104ChannelConfiguration?>)AppliedSettings;
    private SnapshotOutputPort<DeviceDiagnosticsSnapshot> DiagnosticsPort => (SnapshotOutputPort<DeviceDiagnosticsSnapshot>)Diagnostics;
    private SnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEndPort => (SnapshotOutputPort<DeviceSessionEndSnapshot?>)SessionEnd;
    private SnapshotOutputPort<Pt104SessionState> StatePort => (SnapshotOutputPort<Pt104SessionState>)State;
    private SnapshotOutputPort<Pt104Reading?> LatestReadingPort => (SnapshotOutputPort<Pt104Reading?>)LatestReading;
    private StreamOutputPort<Pt104Reading> ReadingsPort => (StreamOutputPort<Pt104Reading>)Readings;

    private async Task RunLiveReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var activeChannels = State.Current!.Channels.Values.Where(channel => channel.LiveReading).Select(channel => channel.Channel).ToArray();
                if (activeChannels.Length == 0)
                {
                    break;
                }

                foreach (var channel in activeChannels)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var channelState = RequireChannel(channel);
                    var configuration = BuildConfiguration(channelState);

                    try
                    {
                        await Task.Run(() => _driver.ApplySettings(configuration), cancellationToken).ConfigureAwait(false);
                        AppliedSettingsPort.Publish(configuration);
                        var reading = await Task.Run(
                            () => _driver.ReadTemperatureC(channel, configuration.FilteredRead, attempts: 3, delayMilliseconds: 500),
                            cancellationToken).ConfigureAwait(false);
                        PublishReading(channel, reading, "LiveRead");
                        UpdateChannelState(channel, state => state with { Available = true });
                        PublishDiagnostics(Diagnostics.Current! with
                        {
                            LastHardwareResponse = $"Live sample {reading:F3} C from channel {channel}.",
                            LastError = null
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        UpdateChannelState(channel, state => state with { Available = false, LiveReading = false });
                        PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_syncRoot)
            {
                _liveCancellation?.Dispose();
                _liveCancellation = null;
                _liveTask = null;
            }

            var channels = State.Current!.Channels.ToDictionary(
                pair => pair.Key,
                pair => pair.Value with { LiveReading = false });
            PublishState(State.Current! with
            {
                Channels = channels,
                StatusMessage = State.Current!.Connected ? "Live read stopped." : State.Current!.StatusMessage
            });
            PublishDiagnostics(Diagnostics.Current! with { LastStateTransition = "Live acquisition stopped" });
        }
    }

    private async Task StopAllLiveAsync(CancellationToken cancellationToken = default)
    {
        var liveChannels = State.Current!.Channels.Values.Where(channel => channel.LiveReading).Select(channel => channel.Channel).ToArray();
        foreach (var channel in liveChannels)
        {
            await StopLiveAsync(channel, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task VerifyChannelAvailabilityAsync(CancellationToken cancellationToken)
    {
        await FastVerifyChannelAvailabilityAsync(cancellationToken).ConfigureAwait(false);
        if (State.Current!.Channels.Values.Any(channel => channel.Available))
        {
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastValidationResult = $"{State.Current!.Channels.Values.Count(channel => channel.Available)} channel(s) available."
            });
            return;
        }

        foreach (var channelNumber in ChannelNumbers)
        {
            var current = RequireChannel(channelNumber);
            var detected = await DetectWorkingChannelSettingsAsync(channelNumber, current, cancellationToken).ConfigureAwait(false);
            if (detected is not null)
            {
                UpdateChannelState(channelNumber, state => state with
                {
                    Available = true,
                    MeasurementMode = detected.MeasurementMode,
                    WireCount = detected.WireCount,
                    MainsFrequencyHz = detected.MainsFrequencyHz,
                    FilteredRead = detected.FilteredRead
                });
            }
            else
            {
                UpdateChannelState(channelNumber, state => state with { Available = false });
            }
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastValidationResult = $"{State.Current!.Channels.Values.Count(channel => channel.Available)} channel(s) available after fallback scan."
        });
    }

    private async Task FastVerifyChannelAvailabilityAsync(CancellationToken cancellationToken)
    {
        foreach (var channelNumber in ChannelNumbers)
        {
            var configuration = BuildConfiguration(RequireChannel(channelNumber));
            await Task.Run(() => _driver.ConfigureChannel(configuration), cancellationToken).ConfigureAwait(false);
        }

        var settleDelay = TimeSpan.FromMilliseconds(Math.Max(2000, ChannelNumbers.Length * 950));
        await Task.Delay(settleDelay, cancellationToken).ConfigureAwait(false);

        foreach (var channelNumber in ChannelNumbers)
        {
            var state = RequireChannel(channelNumber);
            try
            {
                _ = await Task.Run(
                    () => _driver.ReadTemperatureC(channelNumber, state.FilteredRead, attempts: 1, delayMilliseconds: 0, allowRepeatValue: false),
                    cancellationToken).ConfigureAwait(false);
                UpdateChannelState(channelNumber, channel => channel with { Available = true });
            }
            catch
            {
                UpdateChannelState(channelNumber, channel => channel with { Available = false });
            }
        }
    }

    private async Task<Pt104ChannelConfiguration?> DetectWorkingChannelSettingsAsync(int channel, Pt104ChannelState state, CancellationToken cancellationToken)
    {
        foreach (var candidate in GetProbeSettings(channel, state))
        {
            try
            {
                await Task.Run(() => _driver.ApplySettings(candidate), cancellationToken).ConfigureAwait(false);
                _ = await Task.Run(
                    () => _driver.ReadTemperatureC(channel, candidate.FilteredRead, attempts: 3, delayMilliseconds: 700, allowRepeatValue: false),
                    cancellationToken).ConfigureAwait(false);
                return candidate;
            }
            catch
            {
            }
        }

        return null;
    }

    private static IEnumerable<Pt104ChannelConfiguration> GetProbeSettings(int channel, Pt104ChannelState state)
    {
        static IEnumerable<Pt104ChannelConfiguration> BuildCandidates(int channel, Pt104MeasurementMode mode, int mains)
        {
            foreach (var wire in new[] { 4, 3, 2 })
            {
                yield return new Pt104ChannelConfiguration(channel, mode, wire, mains, true);
                yield return new Pt104ChannelConfiguration(channel, mode, wire, mains, false);
            }
        }

        var seen = new HashSet<Pt104ChannelConfiguration>();
        IEnumerable<Pt104ChannelConfiguration> candidates =
        [
            new Pt104ChannelConfiguration(channel, state.MeasurementMode, state.WireCount, state.MainsFrequencyHz, state.FilteredRead),
            new Pt104ChannelConfiguration(channel, state.MeasurementMode, state.WireCount, state.MainsFrequencyHz, !state.FilteredRead),
            .. BuildCandidates(channel, state.MeasurementMode, state.MainsFrequencyHz),
            .. BuildCandidates(channel, state.MeasurementMode, state.MainsFrequencyHz == 50 ? 60 : 50)
        ];

        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private void PublishReading(int channel, double valueCelsius, string sourceMode)
    {
        var capturedAt = DateTimeOffset.UtcNow;
        var reading = new Pt104Reading(State.Current!.DeviceId, channel, valueCelsius, capturedAt, sourceMode);
        LatestReadingPort.Publish(reading);
        ReadingsPort.Publish(reading);
        UpdateChannelState(channel, state => state with
        {
            LastSampleTimestamp = capturedAt,
            LastSampleValue = valueCelsius,
            LastSourceMode = sourceMode
        });
    }

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot) => DiagnosticsPort.Publish(snapshot);
    private void PublishSessionEnd(DeviceSessionEndSnapshot? snapshot) => SessionEndPort.Publish(snapshot);
    private void PublishState(Pt104SessionState state) => StatePort.Publish(state);

    private void UpdateChannelState(int channelNumber, Func<Pt104ChannelState, Pt104ChannelState> update)
    {
        var currentState = State.Current!;
        var channels = currentState.Channels.ToDictionary(pair => pair.Key, pair => pair.Value);
        channels[channelNumber] = update(channels[channelNumber]);
        PublishState(currentState with { Channels = channels });
    }

    private bool AnyLiveChannels() => State.Current!.Channels.Values.Any(channel => channel.LiveReading);

    private Pt104ChannelState RequireChannel(int channel)
    {
        if (!State.Current!.Channels.TryGetValue(channel, out var channelState))
        {
            throw new InvalidOperationException($"Unknown PT-104 channel {channel}.");
        }

        return channelState;
    }

    private static Pt104ChannelConfiguration BuildConfiguration(Pt104ChannelState state)
    {
        return new Pt104ChannelConfiguration(
            state.Channel,
            state.MeasurementMode,
            state.WireCount,
            state.MainsFrequencyHz,
            state.FilteredRead);
    }

    private static IReadOnlyDictionary<int, Pt104ChannelState> CreateDefaultChannels()
    {
        return ChannelNumbers.ToDictionary(
            channel => channel,
            channel => new Pt104ChannelState(channel, false, Pt104MeasurementMode.Pt100, 4, 50, true, false, null, null, null));
    }

    private static void ValidateConfiguration(Pt104ChannelConfiguration configuration)
    {
        if (configuration.Channel is < 1 or > 4)
        {
            throw new InvalidOperationException("PT-104 channel must be between 1 and 4.");
        }

        if (configuration.WireCount is not (2 or 3 or 4))
        {
            throw new InvalidOperationException("PT-104 wire count must be 2, 3, or 4.");
        }

        if (configuration.MainsFrequencyHz is not (50 or 60))
        {
            throw new InvalidOperationException("PT-104 mains frequency must be 50 or 60 Hz.");
        }
    }

    private void EnsureConnected()
    {
        if (!State.Current!.Connected)
        {
            throw new InvalidOperationException("PT-104 session is not connected.");
        }
    }
}
