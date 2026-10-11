using System.Windows.Input;

namespace NovaModbus.Client.Services;

/// <summary>Minimal <see cref="ICommand"/> implementation (no external MVVM toolkit).</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>Initializes a command.</summary>
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

    /// <summary>Re-query <see cref="CanExecute"/>.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
