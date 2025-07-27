using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        // --- Aktuell ausgewählter Controller ---
        private LedController? _selectedController;
        public LedController? SelectedController
        {
            get => _selectedController;
            set
            {
                if (_selectedController != value)
                {
                    _selectedController = value;
                    OnPropertyChanged(nameof(SelectedController));
                }
            }
        }

        // --- Farbe und Pattern (gekürzt, falls vorhanden) ---
        // ... (other properties for color selection etc., not shown for brevity) ...

        // --- Kommandos ---
        public ICommand AddControllerCommand { get; }
        public ICommand RemoveControllerCommand { get; }
        public ICommand AddStripCommand { get; }
        public ICommand RemoveStripCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }

        // --- Intern: Konfigurationspfad ---
        private const string ConfigPath = "led_config.json";

        // --- Intern: Ambilight Tasks pro Bildschirm ---
        private readonly System.Collections.Generic.Dictionary<string, CancellationTokenSource> _ambilightTasks = new();

        // ==== Konstruktor ====
        public MainViewModel()
        {
            // Konfiguration laden
            Controllers = ConfigService.LoadConfiguration(ConfigPath);

            // Kommandos definieren
            AddControllerCommand = new RelayCommand(() =>
            {
                Controllers.Add(new LedController());
            });
            RemoveControllerCommand = new RelayCommand(() =>
            {
                if (SelectedController != null)
                {
                    Controllers.Remove(SelectedController);
                    SelectedController = null;
                }
            });
            AddStripCommand = new RelayCommand(() =>
            {
                if (SelectedController != null)
                {
                    SelectedController.Strips.Add(new LedStrip());
                }
            });
            RemoveStripCommand = new RelayCommand<LedStrip>(strip =>
            {
                if (SelectedController != null && strip != null)
                {
                    SelectedController.Strips.Remove(strip);
                }
            });
            SaveCommand = new RelayCommand(() =>
            {
                ConfigService.SaveConfiguration(ConfigPath, Controllers);
            });
            LoadCommand = new RelayCommand(() =>
            {
                var loaded = ConfigService.LoadConfiguration(ConfigPath);
                Controllers.Clear();
                foreach (var ctrl in loaded)
                    Controllers.Add(ctrl);
            });

            // Events abonnieren, um Ambilight zu steuern
            Controllers.CollectionChanged += Controllers_CollectionChanged;
            foreach (var controller in Controllers)
            {
                // Subscribe to strip collection changes
                controller.Strips.CollectionChanged += Strips_CollectionChanged;
                foreach (var strip in controller.Strips)
                {
                    // Ensure back-reference and subscribe to AmbilightActive changes
                    strip.Controller = controller;
                    if (strip is INotifyPropertyChanged npc)
                        npc.PropertyChanged += Strip_PropertyChanged;
                }
            }
        }

        // === Event-Handler ===

        private void Controllers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (LedController controller in e.NewItems)
                {
                    // Attach to new controller's strips collection
                    controller.Strips.CollectionChanged += Strips_CollectionChanged;
                    // Subscribe to existing strips in new controller
                    foreach (var strip in controller.Strips)
                    {
                        strip.Controller = controller;
                        if (strip is INotifyPropertyChanged npc)
                            npc.PropertyChanged += Strip_PropertyChanged;
                    }
                }
            }
            if (e.OldItems != null)
            {
                foreach (LedController controller in e.OldItems)
                {
                    // Detach event handlers from removed controller
                    controller.Strips.CollectionChanged -= Strips_CollectionChanged;
                    foreach (var strip in controller.Strips)
                    {
                        if (strip is INotifyPropertyChanged npc)
                            npc.PropertyChanged -= Strip_PropertyChanged;
                    }
                }
            }
        }

        private void Strips_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Identify parent controller of this strips collection
            LedController? parentController = Controllers.FirstOrDefault(c => c.Strips == sender);
            if (e.NewItems != null && parentController != null)
            {
                foreach (LedStrip strip in e.NewItems)
                {
                    strip.Controller = parentController;
                    if (strip is INotifyPropertyChanged npc)
                        npc.PropertyChanged += Strip_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (LedStrip strip in e.OldItems)
                {
                    if (strip is INotifyPropertyChanged npc)
                        npc.PropertyChanged -= Strip_PropertyChanged;
                }
            }
        }

        private void Strip_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is LedStrip strip && e.PropertyName == nameof(LedStrip.AmbilightActive))
            {
                string screenName = strip.AmbilightScreenName;
                if (strip.AmbilightActive)
                {
                    // Ambilight eingeschaltet für diesen Strip
                    if (string.IsNullOrEmpty(screenName))
                    {
                        // Falls kein Bildschirm zugeordnet, Standard auf Primärbildschirm setzen
                        Screen? primary = Screen.PrimaryScreen;
                        if (primary != null)
                        {
                            strip.AmbilightScreenName = primary.DeviceName;
                            screenName = strip.AmbilightScreenName;
                        }
                    }
                    // Falls keine Zonen definiert, automatische Standard-Bereiche berechnen
                    if (strip.AmbilightMapping.Zones.Count == 0 && strip.AmbilightMapping.Mode != AmbilightMode.Free)
                    {
                        Screen? tgtScreen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == screenName)
                                            ?? Screen.PrimaryScreen;
                        if (tgtScreen != null)
                        {
                            // Einfache Verteilung der Zonen über den Bildschirm (Fallback)
                            var bounds = tgtScreen.Bounds;
                            int ledCount = strip.LedCount;
                            int zoneW = Math.Max(1, strip.AmbilightMapping.ZoneWidth);
                            int zoneH = Math.Max(1, strip.AmbilightMapping.ZoneHeight);
                            strip.AmbilightMapping.Zones.Clear();
                            for (int i = 0; i < ledCount; i++)
                            {
                                int x = (int)((bounds.Width - zoneW) * i / Math.Max(1.0, ledCount - 1));
                                int y = (bounds.Height - zoneH) / 2;
                                strip.AmbilightMapping.Zones.Add(new AmbilightZone
                                {
                                    LedIndex = i,
                                    Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH }
                                });
                            }
                        }
                    }
                    // Starte Task für Ambilight-Capture auf diesem Bildschirm
                    if (!_ambilightTasks.ContainsKey(screenName))
                    {
                        var cts = new CancellationTokenSource();
                        _ambilightTasks[screenName] = cts;
                        var token = cts.Token;
                        Task.Run(() =>
                        {
                            while (!token.IsCancellationRequested)
                            {
                                // Alle aktiven Strips auf diesem Bildschirm ermitteln
                                var activeStrips = Controllers.SelectMany(c => c.Strips)
                                                              .Where(s => s.AmbilightActive && s.AmbilightScreenName == screenName)
                                                              .ToList();
                                if (activeStrips.Count == 0)
                                    break;
                                // Bildschirm aufnehmen
                                Screen? capScreen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == screenName);
                                if (capScreen == null)
                                    break;
                                using var bmp = ScreenCaptureService.CaptureScreen(capScreen);
                                // Farben für jeden aktiven Strip berechnen und senden
                                foreach (var activeStrip in activeStrips)
                                {
                                    var mapping = activeStrip.AmbilightMapping;
                                    var ledColors = new System.Collections.Generic.List<System.Drawing.Color>();
                                    foreach (var zone in mapping.Zones)
                                    {
                                        var rect = new System.Drawing.Rectangle(zone.Zone.X, zone.Zone.Y, zone.Zone.Width, zone.Zone.Height);
                                        ledColors.Add(ScreenCaptureService.AverageColor(bmp, rect));
                                    }
                                    AmbilightUdpService.SendFrame(activeStrip.Controller!, activeStrip, ledColors);
                                }
                                Thread.Sleep(1000 / 30);
                            }
                        }, token);
                    }
                    // Falls bereits ein Task für diesen Bildschirm läuft, wird der Strip beim nächsten Durchlauf mit verarbeitet
                }
                else
                {
                    // Ambilight ausgeschaltet für diesen Strip
                    bool anyRemaining = Controllers.SelectMany(c => c.Strips)
                                                   .Any(s => s.AmbilightActive && s.AmbilightScreenName == screenName);
                    if (!anyRemaining && _ambilightTasks.ContainsKey(screenName))
                    {
                        // Task für Bildschirm beenden, wenn kein weiterer Strip aktiv
                        _ambilightTasks[screenName].Cancel();
                        _ambilightTasks.Remove(screenName);
                    }
                }
            }
        }

        // ==== INotifyPropertyChanged ====
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
