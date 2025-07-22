using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using LED_Controller.Models;
using LED_Controller.Services;
using LED_Controller.Relay;

namespace LED_Controller.ViewModels
{
    /// <summary>
    /// ViewModel für die Hauptoberfläche.
    /// Verwaltet Controller, Strips, Farbauswahl, Kommandos und die Speicherung.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- Datenquelle für UI-Bindings ---
        public ObservableCollection<LedController> Controllers { get; set; }

        // --- Aktuell ausgewählter Controller in der UI ---
        private LedController? _selectedController;
        public LedController? SelectedController
        {
            get => _selectedController;
            set
            {
                if (_selectedController != value)
                {
                    _selectedController = value;
                    OnPropertyChanged();
                }
            }
        }

        // --- Farbwahl für ColorPicker ---
        private Color _selectedColor = Colors.White;
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    OnPropertyChanged();
                }
            }
        }

        // --- Statusmeldung für Benutzer (Erfolg, Fehler etc.) ---
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // --- Alle für die UI relevanten Commands ---
        public ICommand AddControllerCommand { get; }
        public ICommand RemoveControllerCommand { get; }
        public ICommand AddStripCommand { get; }
        public ICommand RemoveStripCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand SendColorCommand { get; }

        /// <summary>
        /// Konstruktor: Initialisiert Commands und lädt ggf. gespeicherte Controller.
        /// </summary>
        public MainViewModel()
        {
            // Beim Start vorhandene Konfiguration laden (ansonsten neue leere Liste).
            Controllers = ConfigService.LoadConfiguration(ConfigPath);

            AddControllerCommand = new RelayCommand(AddController);
            RemoveControllerCommand = new RelayCommand(RemoveSelectedController, () => SelectedController != null);
            AddStripCommand = new RelayCommand(AddStrip);
            RemoveStripCommand = new RelayCommand(RemoveStrip);
            SaveCommand = new RelayCommand(() => ConfigService.SaveConfiguration(ConfigPath, Controllers));
            LoadCommand = new RelayCommand(() =>
            {
                var loaded = ConfigService.LoadConfiguration(ConfigPath);
                Controllers.Clear();
                foreach (var c in loaded)
                    Controllers.Add(c);
            });

            // Farbe senden an aktuellen Controller per UDP
            SendColorCommand = new RelayCommand(SendColor, () => SelectedController != null);
        }

        /// <summary>
        /// Fügt einen neuen Controller hinzu.
        /// </summary>
        private void AddController()
        {
            var newCtrl = new LedController
            {
                Name = "Neuer Controller",
                IpAddress = "192.168.0.100",
                Port = 4210
            };
            Controllers.Add(newCtrl);
            SelectedController = newCtrl; 
        }

        /// <summary>
        /// Entfernt den aktuell ausgewählten Controller.
        /// </summary>
        private void RemoveSelectedController()
        {
            if (SelectedController != null)
                Controllers.Remove(SelectedController);
        }

        /// <summary>
        /// Fügt einem Controller einen neuen LED-Strip hinzu.
        /// </summary>
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

        /// <summary>
        /// Entfernt einen Strip aus dem zugehörigen Controller.
        /// </summary>
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

        /// <summary>
        /// Sendet die aktuell ausgewählte Farbe per UDP an den ausgewählten Controller.
        /// </summary>
        private void SendColor()
        {
            if (SelectedController == null)
            {
                StatusMessage = "Kein Controller ausgewählt!";
                return;
            }

            // Beispiel: UDP-Service – diesen Service musst du anlegen (siehe vorherige Antworten)
            bool ok = Services.UdpSenderService.SendColor(
                SelectedController.IpAddress,
                SelectedController.Port,
                SelectedColor.R,
                SelectedColor.G,
                SelectedColor.B,
                out var error);

            StatusMessage = ok ? "Farbe gesendet!" : $"Fehler: {error}";
        }

        // --- PropertyChanged Event für DataBinding ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // --- Utility für AppData-Speicherpfad ---
        private static string GetConfigPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LED_Controller"
            );
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "controllers.json");
        }
        private static string ConfigPath => GetConfigPath();
    }
}
