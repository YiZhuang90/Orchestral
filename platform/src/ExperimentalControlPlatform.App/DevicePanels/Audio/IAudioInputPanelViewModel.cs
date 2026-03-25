using System.Collections;
using System.Threading.Tasks;
using System.Windows.Media;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Widgets;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.Audio;

public interface IAudioInputPanelViewModel : IDeviceTestPanelViewModel, IIntegrationPanelViewModel
{
    IEnumerable DeviceOptions { get; }
    object? SelectedDeviceItem { get; set; }

    IEnumerable ChannelModeOptions { get; }
    object? SelectedChannelModeItem { get; set; }

    string TargetUpdateRateInputDraft { get; set; }
    string WindowMillisecondsInputDraft { get; set; }

    string PrimaryValueLabel { get; }
    string CurrentPrimaryValue { get; }

    IReadOnlyList<ValueCardItem> StatisticsCards { get; }

    double PlotWidth { get; }
    double PlotHeight { get; }
    PointCollection PlotPoints { get; }
    PointCollection PlotFillPoints { get; }
    string PlotMinLabel { get; }
    string PlotLowerMidLabel { get; }
    string PlotMidLabel { get; }
    string PlotUpperMidLabel { get; }
    string PlotMaxLabel { get; }
    string XAxisStartLabel { get; }
    string XAxisMidLeftLabel { get; }
    string XAxisMidRightLabel { get; }
    string XAxisEndLabel { get; }

    string FooterConnectionLabel { get; }
    string FooterDeviceLabel { get; }
    string FooterFormatLabel { get; }
    string FooterModeLabel { get; }
    string FooterSystemStateLabel { get; }

    bool IsConnected { get; }
    bool IsLiveReading { get; }
    bool CanToggleConnection { get; }
    bool CanReadOnce { get; }
    bool CanToggleLive { get; }
    bool CanClearData { get; }
    bool CanExportData { get; }
    bool CanApplySettings { get; }
    string ConnectionToggleLabel { get; }
    PackIconMaterialKind ConnectionToggleIconKind { get; }
    string LiveToggleLabel { get; }
    PackIconMaterialKind LiveToggleIconKind { get; }

    string DiagnosticsTitle { get; }
    string DiagnosticsSubtitle { get; }
    string GetDiagnosticsSummary();

    Task RefreshDevicesAsync();
    Task ConnectAsync();
    Task DisconnectAsync();
    Task ReadOnceAsync();
    Task StartLiveReadAsync();
    void StopLiveRead();
    void ClearData();
    void ExportCsv();
}
