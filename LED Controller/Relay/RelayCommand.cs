using System;
using System.Windows.Input;

namespace LED_Controller.Relay
{
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
