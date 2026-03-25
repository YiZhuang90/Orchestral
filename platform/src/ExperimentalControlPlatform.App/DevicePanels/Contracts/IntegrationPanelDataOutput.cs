using System;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public sealed record IntegrationPanelDataOutput
{
    public DateTimeOffset? Timestamp { get; init; }

    public string? EndpointId { get; init; }

    public string? PayloadType { get; init; }

    public string? PayloadValue { get; init; }

    public string? Units { get; init; }

    public long? SequenceNumber { get; init; }

    public double? CaptureRate { get; init; }

    public string? SourceMode { get; init; }
}
