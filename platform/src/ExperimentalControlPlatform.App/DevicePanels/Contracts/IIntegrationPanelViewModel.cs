using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public interface IIntegrationPanelViewModel : INotifyPropertyChanged
{
    IntegrationPanelDataOutput? DataOutput { get; }

    IntegrationPanelAppliedSettingsOutput? AppliedSettingsOutput { get; }

    IntegrationPanelStatusOutput? StatusOutput { get; }

    IntegrationPanelDiagnosticsOutput? DiagnosticsOutput { get; }

    IntegrationPanelSessionEndOutput? SessionEndOutput { get; }

    IReadOnlyList<IntegrationPanelLifecycleAction> SupportedLifecycleActions { get; }

    ICommand LifecycleActionCommand { get; }
}
