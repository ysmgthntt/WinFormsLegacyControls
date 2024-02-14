using System.Windows.Input;

namespace WinFormsApp1;

internal sealed class SimpleCommand(Action<object?> execute, Func<object?, bool> canExecute) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute(parameter);

    public void Execute(object? parameter) => execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
