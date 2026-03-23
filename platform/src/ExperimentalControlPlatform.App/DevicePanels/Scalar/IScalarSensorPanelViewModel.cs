using System.Collections;
using System.Threading.Tasks;
using System.Windows.Media;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.Widgets;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.Scalar;

public interface IScalarSensorPanelViewModel : IDeviceTestPanelViewModel
{
    IEnumerable DeviceOptions { get; }
    object? SelectedDeviceItem { get; set; }

    IEnumerable ChannelOptions { get; }
    object? SelectedChannelItem { get; set; }

    IEnumerable MeasurementTypeOptions { get; }
    object? SelectedMeasurementTypeItem { get; set; }

    IEnumerable WireCountOptions { get; }
    object? SelectedWireCountItem { get; set; }

    IEnumerable MainsFrequencyOptions { get; }
    object? SelectedMainsFrequencyItem { get; set; }

    bool FilteredRead { get; set; }

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
    string FooterChannelLabel { get; }
    string FooterSensorLabel { get; }
    string FooterWireLabel { get; }
    string FooterMainsLabel { get; }
    string FooterFilterLabel { get; }
    string FooterSystemStateLabel { get; }

    bool IsConnected { get; }
    bool IsLiveReading { get; }
    bool CanToggleConnection { get; }
    bool CanReadOnce { get; }
    bool CanToggleLive { get; }
    bool CanClearData { get; }
    bool CanExportData { get; }
    string ConnectionToggleLabel { get; }
    PackIconMaterialKind ConnectionToggleIconKind { get; }
    string LiveToggleLabel { get; }
    PackIconMaterialKind LiveToggleIconKind { get; }

    string DiagnosticsTitle { get; }
    string DiagnosticsSubtitle { get; }
    string GetDiagnosticsSummary();

    Task ConnectAsync();
    Task DisconnectAsync();
    Task ReadOnceAsync();
    Task StartLiveReadAsync();
    void StopLiveRead();
    void ClearData();
    void ExportCsv();
}
