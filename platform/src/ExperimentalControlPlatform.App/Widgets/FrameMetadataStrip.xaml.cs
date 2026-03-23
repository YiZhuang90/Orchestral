using System.Windows;
using System.Windows.Controls;

namespace ExperimentalControlPlatform.App.Widgets;

public partial class FrameMetadataStrip : UserControl
{
    public FrameMetadataStrip()
    {
        InitializeComponent();
    }

    public string ResolutionText
    {
        get => (string)GetValue(ResolutionTextProperty);
        set => SetValue(ResolutionTextProperty, value);
    }

    public static readonly DependencyProperty ResolutionTextProperty =
        DependencyProperty.Register(nameof(ResolutionText), typeof(string), typeof(FrameMetadataStrip), new PropertyMetadata("--"));

    public string PixelFormatText
    {
        get => (string)GetValue(PixelFormatTextProperty);
        set => SetValue(PixelFormatTextProperty, value);
    }

    public static readonly DependencyProperty PixelFormatTextProperty =
        DependencyProperty.Register(nameof(PixelFormatText), typeof(string), typeof(FrameMetadataStrip), new PropertyMetadata("--"));

    public string ExposureText
    {
        get => (string)GetValue(ExposureTextProperty);
        set => SetValue(ExposureTextProperty, value);
    }

    public static readonly DependencyProperty ExposureTextProperty =
        DependencyProperty.Register(nameof(ExposureText), typeof(string), typeof(FrameMetadataStrip), new PropertyMetadata("--"));

    public string FrameRateText
    {
        get => (string)GetValue(FrameRateTextProperty);
        set => SetValue(FrameRateTextProperty, value);
    }

    public static readonly DependencyProperty FrameRateTextProperty =
        DependencyProperty.Register(nameof(FrameRateText), typeof(string), typeof(FrameMetadataStrip), new PropertyMetadata("--"));
}
