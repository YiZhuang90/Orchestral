using System;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public sealed class IntegrationPanelOutputPublisher
{
    private readonly object _syncRoot = new();
    private DateTimeOffset? _lastPublishedAt;
    private string? _lastSignature;

    public bool ShouldPublish(IntegrationPanelOutputSettings settings, DateTimeOffset timestamp, string? signature)
    {
        lock (_syncRoot)
        {
            switch (settings.EmissionMode)
            {
                case IntegrationPanelOutputEmissionMode.Periodic:
                    if (_lastPublishedAt.HasValue)
                    {
                        var interval = TimeSpan.FromSeconds(1.0 / Math.Max(settings.OutputFrequencyHz, 0.1));
                        if ((timestamp - _lastPublishedAt.Value) < interval)
                        {
                            return false;
                        }
                    }

                    break;

                case IntegrationPanelOutputEmissionMode.OnChange:
                    if (string.Equals(_lastSignature, signature, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    break;
            }

            _lastPublishedAt = timestamp;
            _lastSignature = signature;
            return true;
        }
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            _lastPublishedAt = null;
            _lastSignature = null;
        }
    }
}
