namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public sealed record IntegrationPanelOutputSettings(
    IntegrationPanelOutputPayloadType PayloadType,
    IntegrationPanelOutputEmissionMode EmissionMode,
    double OutputFrequencyHz,
    bool IncludeMetadata);
