namespace ExperimentalControlPlatform.Runtime;

public interface IPt104RuntimeDriver : IDisposable
{
    bool IsConnected { get; }

    string? ConnectedDeviceId { get; }

    IReadOnlyList<string> EnumerateUsbUnits();

    void Connect(Pt104ChannelConfiguration configuration);

    void ApplySettings(Pt104ChannelConfiguration configuration);

    void ConfigureChannel(Pt104ChannelConfiguration configuration);

    double ReadTemperatureC(bool filtered, int attempts = 10, int delayMilliseconds = 800, bool allowRepeatValue = true);

    double ReadTemperatureC(int channel, bool filtered, int attempts = 10, int delayMilliseconds = 800, bool allowRepeatValue = true);

    void Disconnect();
}
