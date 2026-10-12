using System.Windows.Input;

namespace NovaModbus.Rtu.Commands;

/// <summary>Minimal <see cref="ICommand"/> implementation (no external MVVM toolkit).</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;

    /// <summary>Initializes a command (captures the current sync context for event marshal).</summary>
    /// <param name="execute">Action invoked when the command executes.</param>
    /// <param name="canExecute">Predicate determining whether the command can execute, or null to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is null.</exception>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();

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

    /// <summary>Raise <see cref="CanExecuteChanged"/> on the captured context.</summary>
    private void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
