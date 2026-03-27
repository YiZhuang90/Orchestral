namespace ExperimentalControlPlatform.App.DevicePanels.ControlCenter;

public sealed record ControlStateOption(string DisplayLabel, bool Enabled)
{
    public override string ToString() => DisplayLabel;
}
