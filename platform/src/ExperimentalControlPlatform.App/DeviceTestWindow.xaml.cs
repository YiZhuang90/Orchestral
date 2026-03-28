using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ExperimentalControlPlatform.App.DevicePanels;

namespace ExperimentalControlPlatform.App;

public partial class DeviceTestWindow : Window
{
    private const int WmGetMinMaxInfoMessage = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private readonly MainViewModel _viewModel;
    private readonly IPanelCloseViewModel? _closeAwarePanel;
    private bool _closeInProgress;
    private bool _allowImmediateClose;

    public DeviceTestWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _closeAwarePanel = _viewModel.CurrentDevicePanel as IPanelCloseViewModel;
        if (_closeAwarePanel is not null)
        {
            _closeAwarePanel.CloseRequested += OnPanelCloseRequested;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.AddHook(WindowProc);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_allowImmediateClose || _closeAwarePanel is null)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        if (!_closeInProgress)
        {
            _ = CompleteSafeCloseAsync(_closeAwarePanel);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_closeAwarePanel is not null)
        {
            _closeAwarePanel.CloseRequested -= OnPanelCloseRequested;
        }

        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private async Task CompleteSafeCloseAsync(IPanelCloseViewModel panel)
    {
        _closeInProgress = true;
        try
        {
            await panel.CloseWithoutApplyAsync().ConfigureAwait(true);
            _allowImmediateClose = true;
            Close();
        }
        finally
        {
            _allowImmediateClose = false;
            _closeInProgress = false;
        }
    }

    private void OnPanelCloseRequested(object? sender, EventArgs e)
    {
        if (_closeInProgress)
        {
            return;
        }

        _allowImmediateClose = true;
        Close();
        _allowImmediateClose = false;
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetMinMaxInfoMessage)
        {
            WmGetMinMaxInfo(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo();
        monitorInfo.CbSize = Marshal.SizeOf<MonitorInfo>();
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var workArea = monitorInfo.RcWork;
        var monitorArea = monitorInfo.RcMonitor;

        minMaxInfo.PtMaxPosition.X = workArea.Left - monitorArea.Left;
        minMaxInfo.PtMaxPosition.Y = workArea.Top - monitorArea.Top;
        minMaxInfo.PtMaxSize.X = workArea.Right - workArea.Left;
        minMaxInfo.PtMaxSize.Y = workArea.Bottom - workArea.Top;

        Marshal.StructureToPtr(minMaxInfo, lParam, true);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point PtReserved;
        public Point PtMaxSize;
        public Point PtMaxPosition;
        public Point PtMinTrackSize;
        public Point PtMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int CbSize;
        public Rect RcMonitor;
        public Rect RcWork;
        public uint DwFlags;
    }
}
