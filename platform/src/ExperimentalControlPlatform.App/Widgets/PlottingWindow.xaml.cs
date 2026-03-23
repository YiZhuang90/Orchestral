using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExperimentalControlPlatform.App.Widgets;

public partial class PlottingWindow : UserControl
{
    public PlottingWindow()
    {
        InitializeComponent();
    }

    public double PlotWidth
    {
        get => (double)GetValue(PlotWidthProperty);
        set => SetValue(PlotWidthProperty, value);
    }

    public static readonly DependencyProperty PlotWidthProperty =
        DependencyProperty.Register(nameof(PlotWidth), typeof(double), typeof(PlottingWindow), new PropertyMetadata(504d));

    public double PlotHeight
    {
        get => (double)GetValue(PlotHeightProperty);
        set => SetValue(PlotHeightProperty, value);
    }

    public static readonly DependencyProperty PlotHeightProperty =
        DependencyProperty.Register(nameof(PlotHeight), typeof(double), typeof(PlottingWindow), new PropertyMetadata(280d));

    public double QuarterHeight => PlotHeight * 0.25;
    public double HalfHeight => PlotHeight * 0.5;
    public double ThreeQuarterHeight => PlotHeight * 0.75;

    public string PlotMinLabel
    {
        get => (string)GetValue(PlotMinLabelProperty);
        set => SetValue(PlotMinLabelProperty, value);
    }

    public static readonly DependencyProperty PlotMinLabelProperty =
        DependencyProperty.Register(nameof(PlotMinLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string PlotLowerMidLabel
    {
        get => (string)GetValue(PlotLowerMidLabelProperty);
        set => SetValue(PlotLowerMidLabelProperty, value);
    }

    public static readonly DependencyProperty PlotLowerMidLabelProperty =
        DependencyProperty.Register(nameof(PlotLowerMidLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string PlotMidLabel
    {
        get => (string)GetValue(PlotMidLabelProperty);
        set => SetValue(PlotMidLabelProperty, value);
    }

    public static readonly DependencyProperty PlotMidLabelProperty =
        DependencyProperty.Register(nameof(PlotMidLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string PlotUpperMidLabel
    {
        get => (string)GetValue(PlotUpperMidLabelProperty);
        set => SetValue(PlotUpperMidLabelProperty, value);
    }

    public static readonly DependencyProperty PlotUpperMidLabelProperty =
        DependencyProperty.Register(nameof(PlotUpperMidLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string PlotMaxLabel
    {
        get => (string)GetValue(PlotMaxLabelProperty);
        set => SetValue(PlotMaxLabelProperty, value);
    }

    public static readonly DependencyProperty PlotMaxLabelProperty =
        DependencyProperty.Register(nameof(PlotMaxLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string XAxisStartLabel
    {
        get => (string)GetValue(XAxisStartLabelProperty);
        set => SetValue(XAxisStartLabelProperty, value);
    }

    public static readonly DependencyProperty XAxisStartLabelProperty =
        DependencyProperty.Register(nameof(XAxisStartLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string XAxisMidLeftLabel
    {
        get => (string)GetValue(XAxisMidLeftLabelProperty);
        set => SetValue(XAxisMidLeftLabelProperty, value);
    }

    public static readonly DependencyProperty XAxisMidLeftLabelProperty =
        DependencyProperty.Register(nameof(XAxisMidLeftLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string XAxisMidRightLabel
    {
        get => (string)GetValue(XAxisMidRightLabelProperty);
        set => SetValue(XAxisMidRightLabelProperty, value);
    }

    public static readonly DependencyProperty XAxisMidRightLabelProperty =
        DependencyProperty.Register(nameof(XAxisMidRightLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("--"));

    public string XAxisEndLabel
    {
        get => (string)GetValue(XAxisEndLabelProperty);
        set => SetValue(XAxisEndLabelProperty, value);
    }

    public static readonly DependencyProperty XAxisEndLabelProperty =
        DependencyProperty.Register(nameof(XAxisEndLabel), typeof(string), typeof(PlottingWindow), new PropertyMetadata("Now"));

    public PointCollection PlotPoints
    {
        get => (PointCollection)GetValue(PlotPointsProperty);
        set => SetValue(PlotPointsProperty, value);
    }

    public static readonly DependencyProperty PlotPointsProperty =
        DependencyProperty.Register(nameof(PlotPoints), typeof(PointCollection), typeof(PlottingWindow), new PropertyMetadata(new PointCollection()));

    public PointCollection PlotFillPoints
    {
        get => (PointCollection)GetValue(PlotFillPointsProperty);
        set => SetValue(PlotFillPointsProperty, value);
    }

    public static readonly DependencyProperty PlotFillPointsProperty =
        DependencyProperty.Register(nameof(PlotFillPoints), typeof(PointCollection), typeof(PlottingWindow), new PropertyMetadata(new PointCollection()));

    private void PlotClipHost_OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyRoundedPlotClip(sender as FrameworkElement);
    }

    private void PlotClipHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyRoundedPlotClip(sender as FrameworkElement);
    }

    private static void ApplyRoundedPlotClip(FrameworkElement? element)
    {
        if (element is null) return;
        element.Clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), 18, 18);
    }
}
