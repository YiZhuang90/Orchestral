using System.Windows;
using System.Windows.Controls;

namespace ExperimentalControlPlatform.App.Widgets;

public partial class SerialDataPanel : UserControl
{
    public SerialDataPanel()
    {
        InitializeComponent();
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(SerialDataPanel), new PropertyMetadata(null));

    public object? ToolbarContent
    {
        get => GetValue(ToolbarContentProperty);
        set => SetValue(ToolbarContentProperty, value);
    }

    public static readonly DependencyProperty ToolbarContentProperty =
        DependencyProperty.Register(nameof(ToolbarContent), typeof(object), typeof(SerialDataPanel), new PropertyMetadata(null));

    public object? PlotContent
    {
        get => GetValue(PlotContentProperty);
        set => SetValue(PlotContentProperty, value);
    }

    public static readonly DependencyProperty PlotContentProperty =
        DependencyProperty.Register(nameof(PlotContent), typeof(object), typeof(SerialDataPanel), new PropertyMetadata(null));

    public object? StatsContent
    {
        get => GetValue(StatsContentProperty);
        set => SetValue(StatsContentProperty, value);
    }

    public static readonly DependencyProperty StatsContentProperty =
        DependencyProperty.Register(nameof(StatsContent), typeof(object), typeof(SerialDataPanel), new PropertyMetadata(null));
}
