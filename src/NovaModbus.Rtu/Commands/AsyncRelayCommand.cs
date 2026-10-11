using System.Windows.Input;

namespace NovaModbus.Rtu.Commands;

/// <summary>Async <see cref="ICommand"/> with reentrancy guard, error routing, and context marshal.</summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;
    private int _executing;

    /// <summary>Initializes a command (captures the current sync context for event/error marshal).</summary>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
        : this(_ => execute(), canExecute, onError)
    {
    }

    /// <summary>Initializes a cancellable command.</summary>
    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
        _onError = onError;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) =>
        Interlocked.CompareExchange(ref _executing, 0, 0) == 0 && (_canExecute?.Invoke() ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (Interlocked.CompareExchange(ref _executing, 1, 0) != 0)
        {
            return;
        }

        RaiseCanExecuteChanged();
        try
        {
            await _execute(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _executing, 0);
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>Re-query <see cref="CanExecute"/> on the captured context.</summary>
    public void RaiseCanExecuteChanged()
    {
        if (_context is null)
        {
            Raise();
        }
        else
        {
            _context.Post(_ => Raise(), null);
        }
    }

    private void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private void ReportError(Exception ex)
    {
        if (_onError is null)
        {
            return;
        }

        if (_context is null)
        {
            _onError(ex);
        }
        else
        {
            _context.Post(_ => _onError(ex), null);
        }
    }
}
