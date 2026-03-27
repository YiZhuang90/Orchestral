using System.Collections;
using System.Threading.Tasks;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Widgets;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.ControlCenter;

public interface IControlCenterPanelViewModel : IDeviceTestPanelViewModel, IIntegrationPanelViewModel
{
    IEnumerable DeviceOptions { get; }
    object? SelectedDeviceItem { get; set; }

    IEnumerable ControlStateOptions { get; }
    object? SelectedLaserStateItem { get; set; }
    object? SelectedPuffStateItem { get; set; }
    string StepCountInputDraft { get; set; }

    string PrimaryValueLabel { get; }
    string CurrentPrimaryValue { get; }
    string CommandHistory { get; }
    IReadOnlyList<ValueCardItem> StatisticsCards { get; }

    string FooterConnectionLabel { get; }
    string FooterDeviceLabel { get; }
    string FooterLaserLabel { get; }
    string FooterPuffLabel { get; }
    string FooterSystemStateLabel { get; }

    bool IsConnected { get; }
    bool CanToggleConnection { get; }
    bool CanReadPulseCount { get; }
    bool CanApplyCommand { get; }
    bool CanEmergencyStop { get; }
    bool CanClearLog { get; }
    string ConnectionToggleLabel { get; }
    PackIconMaterialKind ConnectionToggleIconKind { get; }

    string DiagnosticsTitle { get; }
    string DiagnosticsSubtitle { get; }
    string GetDiagnosticsSummary();

    Task RefreshDevicesAsync();
    Task ConnectAsync();
    Task DisconnectAsync();
    Task ReadPulseCountAsync();
    Task ApplyCommandAsync();
    Task EmergencyStopAsync();
    Task ApplyAndExitAsync();
    void ClearLog();
}
