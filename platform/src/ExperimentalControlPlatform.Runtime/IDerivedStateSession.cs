using System;

namespace ExperimentalControlPlatform.Runtime;

/// <summary>
/// Contract for experiment-logic derived-state providers.
/// Runtime infrastructure (e.g. ExperimentMonitorSession) consumes this interface
/// without depending on any specific experiment-logic implementation.
/// </summary>
public interface IDerivedStateSession
{
    event Action? DerivedStateChanged;

    IDerivedStateSnapshot? CurrentDerivedState { get; }
}
