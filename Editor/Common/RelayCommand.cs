using System;
using System.Windows.Input;

namespace LumenX
{
    class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // FIXED: Changed parameter to 'object?' and added the missing logic and return paths
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T)parameter) ?? true;
        }

        // FIXED: Changed parameter to 'object?' to match ICommand definition exactly
        public void Execute(object? parameter)
        {
            _execute((T)parameter);
        }

        // Method to manually tell Avalonia UI to update button enabled/disabled states
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}