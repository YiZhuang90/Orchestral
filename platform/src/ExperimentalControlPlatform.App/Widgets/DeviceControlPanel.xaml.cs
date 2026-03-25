using System.Windows;
using System.Windows.Controls;

namespace ExperimentalControlPlatform.App.Widgets;

public partial class DeviceControlPanel : UserControl
{
    public DeviceControlPanel()
    {
        InitializeComponent();
    }

    public object? ConfigurationContent
    {
        get => GetValue(ConfigurationContentProperty);
        set => SetValue(ConfigurationContentProperty, value);
    }

    public static readonly DependencyProperty ConfigurationContentProperty =
        DependencyProperty.Register(nameof(ConfigurationContent), typeof(object), typeof(DeviceControlPanel), new PropertyMetadata(null));

    public object? DiagnosticsContent
    {
        get => GetValue(DiagnosticsContentProperty);
        set => SetValue(DiagnosticsContentProperty, value);
    }

    public static readonly DependencyProperty DiagnosticsContentProperty =
        DependencyProperty.Register(nameof(DiagnosticsContent), typeof(object), typeof(DeviceControlPanel), new PropertyMetadata(null));

    public object? ActionsContent
    {
        get => GetValue(ActionsContentProperty);
        set => SetValue(ActionsContentProperty, value);
    }

    public static readonly DependencyProperty ActionsContentProperty =
        DependencyProperty.Register(nameof(ActionsContent), typeof(object), typeof(DeviceControlPanel), new PropertyMetadata(null));

    public object? LifecycleContent
    {
        get => GetValue(LifecycleContentProperty);
        set => SetValue(LifecycleContentProperty, value);
    }

    public static readonly DependencyProperty LifecycleContentProperty =
        DependencyProperty.Register(nameof(LifecycleContent), typeof(object), typeof(DeviceControlPanel), new PropertyMetadata(null));
}
