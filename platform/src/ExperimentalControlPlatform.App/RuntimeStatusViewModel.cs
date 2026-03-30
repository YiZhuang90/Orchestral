using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App;

public sealed class RuntimeStatusViewModel : INotifyPropertyChanged
{
    private RuntimeRunContext _snapshot;
    private string? _statusMessageOverride;

    public RuntimeStatusViewModel(RuntimeRunContext snapshot)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public RuntimeRunContext Snapshot
    {
        get => _snapshot;
        private set
        {
            if (_snapshot == value)
            {
                return;
            }

            _snapshot = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentState));
            OnPropertyChanged(nameof(StateSummary));
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanStop));
        }
    }

    public string CurrentState =>
        Snapshot.State switch
        {
            RunState.Running => "Running",
            RunState.Stopping => "Stopping",
            _ => "Ready"
        };

    public string StateSummary =>
        _statusMessageOverride
        ?? Snapshot.State switch
           {
               RunState.Running when Snapshot.StartedAtUtc is not null && Snapshot.Experiment is not null =>
                   $"Running {Snapshot.Experiment.Experiment.Name} since {Snapshot.StartedAtUtc.Value.ToLocalTime():HH:mm:ss}",
               RunState.Running when Snapshot.StartedAtUtc is not null =>
                   $"Running since {Snapshot.StartedAtUtc.Value.ToLocalTime():HH:mm:ss}",
               RunState.Stopping when Snapshot.StopReason is not null =>
                   $"Stopping: {Snapshot.StopReason.Message}",
               RunState.Idle when Snapshot.StopReason is not null =>
                   $"Ready after stop: {Snapshot.StopReason.Message}",
               _ => "Ready to start"
           };

    public bool CanStart => Snapshot.State is RunState.Idle;

    public bool CanStop => Snapshot.State is RunState.Running;

    public void Update(RuntimeRunContext snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _statusMessageOverride = null;
        Snapshot = snapshot;
    }

    public void ShowOperationError(string actionMessage, string detail)
    {
        _statusMessageOverride = $"{actionMessage} {detail}";
        OnPropertyChanged(nameof(StateSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
