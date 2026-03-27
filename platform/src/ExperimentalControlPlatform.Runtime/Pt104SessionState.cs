using System.Collections.Generic;

namespace ExperimentalControlPlatform.Runtime;

public sealed record Pt104SessionState
{
    public required string DeviceId { get; init; }

    public required string DeviceName { get; init; }

    public bool Connected { get; init; }

    public bool Busy { get; init; }

    public string StatusMessage { get; init; } = "PT-104 session ready.";

    public IReadOnlyDictionary<int, Pt104ChannelState> Channels { get; init; } = new Dictionary<int, Pt104ChannelState>();
}
