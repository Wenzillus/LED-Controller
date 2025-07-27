using System;
using System.Windows.Input;

namespace LED_Controller.Relay
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T>? _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Schnittstelle exakt matchen:
        public bool CanExecute(object? parameter)
        {
            if (_canExecute is null) return true;
            if (parameter is T t) return _canExecute(t);
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T t)
                _execute(t);
            else
                throw new InvalidOperationException(
                    $"Expected parameter of type {typeof(T)}, but got {parameter?.GetType()}");
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _executeWithParam;
        private readonly Func<object?, bool>? _canExecuteWithParam;

        private readonly Action? _executeNoParam;
        private readonly Func<bool>? _canExecuteNoParam;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _executeNoParam = execute;
            _canExecuteNoParam = canExecute;
        }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeWithParam = execute;
            _canExecuteWithParam = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecuteWithParam != null)
                return _canExecuteWithParam(parameter);
            if (_canExecuteNoParam != null)
                return _canExecuteNoParam();
            return true;
        }

        public void Execute(object? parameter)
        {
            if (_executeWithParam != null)
                _executeWithParam(parameter);
            else
                _executeNoParam?.Invoke();
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
