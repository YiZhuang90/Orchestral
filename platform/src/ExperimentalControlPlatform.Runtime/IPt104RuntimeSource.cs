namespace ExperimentalControlPlatform.Runtime;

public interface IPt104RuntimeSource
{
    ISnapshotOutputPort<Pt104SessionState> State { get; }

    ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics { get; }

    ISnapshotOutputPort<Pt104Reading?> LatestReading { get; }

    IStreamOutputPort<Pt104Reading> Readings { get; }
}
