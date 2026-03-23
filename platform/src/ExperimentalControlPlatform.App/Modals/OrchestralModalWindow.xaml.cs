using System.Windows;

namespace ExperimentalControlPlatform.App.Modals;

public partial class OrchestralModalWindow : Window
{
    public OrchestralModalWindow(string modalTitle, object modalContent, string primaryButtonText = "Close")
    {
        InitializeComponent();
        ModalTitle = modalTitle;
        ModalContent = modalContent;
        PrimaryButtonText = primaryButtonText;
        DataContext = this;
    }

    public string ModalTitle { get; }

    public object ModalContent { get; }

    public string PrimaryButtonText { get; }

    public bool PrimaryActionInvoked { get; private set; }

    private void TitleBar_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        PrimaryActionInvoked = false;
        DialogResult = true;
        Close();
    }

    private void PrimaryButton_OnClick(object sender, RoutedEventArgs e)
    {
        PrimaryActionInvoked = true;
        DialogResult = true;
        Close();
    }
}
