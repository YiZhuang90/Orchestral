using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ExperimentalControlPlatform.App;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _executeAsync;
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<Exception>? _onException;
    private int _isExecuting;

    public AsyncRelayCommand(
        Func<object?, Task> executeAsync,
        Predicate<object?>? canExecute = null,
        Action<Exception>? onException = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onException = onException;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return Volatile.Read(ref _isExecuting) == 0 && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
        {
            return;
        }

        if (_canExecute?.Invoke(parameter) == false)
        {
            Interlocked.Exchange(ref _isExecuting, 0);
            return;
        }

        try
        {
            NotifyCanExecuteChanged();
            await _executeAsync(parameter);
        }
        catch (Exception ex)
        {
            if (_onException is not null)
            {
                _onException.Invoke(ex);
            }
            else
            {
                Debug.WriteLine($"[AsyncRelayCommand] Unhandled exception: {ex}");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isExecuting, 0);
            NotifyCanExecuteChanged();
        }
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
