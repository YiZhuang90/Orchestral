namespace ExperimentalControlPlatform.App.DevicePanels.HyperCam;

public sealed record HyperCamOption(string DisplayName)
{
    public string HeaderDisplayName => DisplayName;

    public override string ToString() => DisplayName;
}
