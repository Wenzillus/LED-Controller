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
        public ICommand AddStripCommand { get; }

        public MainViewModel()
        {
            AddControllerCommand = new RelayCommand(AddController);
            RemoveControllerCommand = new RelayCommand(RemoveSelectedController, () => SelectedController != null); 
            AddStripCommand = new RelayCommand(AddStrip);
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

        private void AddStrip(object? controllerObj)
        {
            if (controllerObj is LedController controller)
            {
                controller.Strips.Add(new LedStrip
                {
                    Name = "Neuer Strip",
                    LedCount = 30,
                    Type = LedType.RGB
                });
            }
        }

        private void RemoveStrip(object? stripObj)
        {
            foreach (var controller in Controllers)
            {
                if (stripObj is LedStrip strip && controller.Strips.Contains(strip))
                {
                    controller.Strips.Remove(strip);
                    break;
                }
            }
        }

        private void RemoveSelectedController()
        {
            if (SelectedController != null)
                Controllers.Remove(SelectedController);
        }
    }
}