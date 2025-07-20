using LED_Controller.Models;
using LED_Controller.Relay;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LED_Controller.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<LedController> Controllers { get; } = new();

        public ICommand AddControllerCommand { get; }
        public ICommand RemoveControllerCommand { get; }

        public MainViewModel()
        {
            AddControllerCommand = new RelayCommand(AddController);
            RemoveControllerCommand = new RelayCommand(RemoveSelectedController, () => SelectedController != null);
        }

        private LedController? _selectedController;
        public LedController? SelectedController
        {
            get => _selectedController;
            set
            {
                _selectedController = value;
                // CommandManager.InvalidateRequerySuggested(); // Optional, zur Aktivierung von CanExecute
            }
        }

        private void AddController()
        {
            Controllers.Add(new LedController
            {
                Name = "Neuer Controller",
                IpAddress = "192.168.0.100",
                Port = 4210
            });
        }

        private void RemoveSelectedController()
        {
            if (SelectedController != null)
                Controllers.Remove(SelectedController);
        }
    }
}