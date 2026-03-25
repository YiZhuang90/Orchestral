using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;

namespace ExperimentalControlPlatform.App.Widgets;

public partial class LifecycleActionRow : UserControl
{
    private IIntegrationPanelViewModel? _currentViewModel;

    public LifecycleActionRow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SyncFromDataContext();
    }

    public IReadOnlyList<IntegrationPanelLifecycleAction>? SupportedLifecycleActions
    {
        get => (IReadOnlyList<IntegrationPanelLifecycleAction>?)GetValue(SupportedLifecycleActionsProperty);
        set => SetValue(SupportedLifecycleActionsProperty, value);
    }

    public static readonly DependencyProperty SupportedLifecycleActionsProperty =
        DependencyProperty.Register(nameof(SupportedLifecycleActions), typeof(IReadOnlyList<IntegrationPanelLifecycleAction>), typeof(LifecycleActionRow), new PropertyMetadata(null));

    public ICommand? LifecycleActionCommand
    {
        get => (ICommand?)GetValue(LifecycleActionCommandProperty);
        set => SetValue(LifecycleActionCommandProperty, value);
    }

    public static readonly DependencyProperty LifecycleActionCommandProperty =
        DependencyProperty.Register(nameof(LifecycleActionCommand), typeof(ICommand), typeof(LifecycleActionRow), new PropertyMetadata(null));

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachFromCurrentViewModel();
        SyncFromDataContext();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachFromCurrentViewModel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SyncFromDataContext();
    }

    private void SyncFromDataContext()
    {
        if (DataContext is IIntegrationPanelViewModel panelViewModel)
        {
            AttachToCurrentViewModel(panelViewModel);
            SupportedLifecycleActions = panelViewModel.SupportedLifecycleActions;
            LifecycleActionCommand = panelViewModel.LifecycleActionCommand;
            Visibility = panelViewModel.SupportedLifecycleActions.Any() ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        SupportedLifecycleActions = null;
        LifecycleActionCommand = null;
        Visibility = Visibility.Collapsed;
    }

    private void AttachToCurrentViewModel(IIntegrationPanelViewModel panelViewModel)
    {
        if (ReferenceEquals(_currentViewModel, panelViewModel))
        {
            return;
        }

        DetachFromCurrentViewModel();
        _currentViewModel = panelViewModel;
        _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void DetachFromCurrentViewModel()
    {
        if (_currentViewModel is null)
        {
            return;
        }

        _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _currentViewModel = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == nameof(IIntegrationPanelViewModel.SupportedLifecycleActions) ||
            e.PropertyName == nameof(IIntegrationPanelViewModel.LifecycleActionCommand))
        {
            SyncFromDataContext();
        }
    }
}
