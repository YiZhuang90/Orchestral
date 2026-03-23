namespace ExperimentalControlPlatform.App.DevicePanels.Pt104;

public sealed record Pt104ConnectionSettings(
    int Channel,
    Pt104MeasurementType MeasurementType,
    int WireCount,
    int MainsFrequencyHz,
    bool FilteredRead);
