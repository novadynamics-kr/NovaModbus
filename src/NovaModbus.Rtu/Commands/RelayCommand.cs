using System.Windows.Input;

namespace NovaModbus.Rtu.Commands;

/// <summary>Minimal <see cref="ICommand"/> implementation (no external MVVM toolkit).</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;

    /// <summary>Initializes a command (captures the current sync context for event marshal).</summary>
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

    private void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
