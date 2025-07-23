using LED_Controller.Models;
using LED_Controller.Relay;
using LED_Controller.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing; // Für Color, Bitmap
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // Für Screen
using System.Windows.Input;
using System.Windows.Media;

namespace LED_Controller.ViewModels
{
    public class AmbilightViewModel : INotifyPropertyChanged
    {
        public static Array AmbilightModeValues => Enum.GetValues(typeof(LED_Controller.Models.AmbilightMode));
        // === Monitor-Auswahl ===
        public ObservableCollection<Screen> Screens { get; }
        private Screen? _selectedScreen;
        public Screen? SelectedScreen
        {
            get => _selectedScreen;
            set { _selectedScreen = value; OnPropertyChanged(); }
        }

        // === Strip-Auswahl ===
        public ObservableCollection<AmbilightStripConfig> Strips { get; set; }
        private AmbilightStripConfig? _selectedStrip;
        public AmbilightStripConfig? SelectedStrip
        {
            get => _selectedStrip;
            set { _selectedStrip = value; OnPropertyChanged(); }
        }

        // === Vorschau: Farben je LED (für das UI) ===
        public ObservableCollection<SolidColorBrush> PreviewColors { get; } = new();

        // === FPS-Anzeige ===
        private double _currentFps;
        public double CurrentFps
        {
            get => _currentFps;
            set { _currentFps = value; OnPropertyChanged(); }
        }

        // === Commands ===
        public ICommand StartTestCommand { get; }
        public ICommand StopTestCommand { get; }

        // === Intern ===
        private CancellationTokenSource? _ambilightCts;

        // === Konstruktor ===
        public AmbilightViewModel()
        {
            Screens = new ObservableCollection<Screen>(Screen.AllScreens);
            SelectedScreen = Screens.FirstOrDefault();

            // Die Strips-Liste musst du vorab mit deinen Strips befüllen!
            Strips = new ObservableCollection<AmbilightStripConfig>();

            StartTestCommand = new RelayCommand(StartTest);
            StopTestCommand = new RelayCommand(StopTest);
        }

        private async void StartTest()
        {
            _ambilightCts?.Cancel();
            _ambilightCts = new CancellationTokenSource();
            var token = _ambilightCts.Token;
            var stripConfig = SelectedStrip;
            var screen = SelectedScreen;
            if (stripConfig == null || screen == null) return;

            var mapping = stripConfig.Mapping;
            var strip = stripConfig.Strip;
            int ledCount = strip.LedCount;

            Stopwatch sw = new Stopwatch();
            int frames = 0;
            sw.Start();

            await Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var bmp = ScreenCaptureService.CaptureScreen(screen);

                    // Für jede LED: Farbe bestimmen
                    var ledColors = new System.Collections.Generic.List<System.Drawing.Color>();
                    foreach (var zone in mapping.Zones)
                        ledColors.Add(ScreenCaptureService.AverageColor(bmp, new System.Drawing.Rectangle(zone.Zone.X, zone.Zone.Y, zone.Zone.Width, zone.Zone.Height)));

                    // Per UDP senden
                    AmbilightUdpService.SendFrame(strip.Controller, strip, ledColors);

                    // Vorschau (UI-Thread!)
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        PreviewColors.Clear();
                        foreach (var c in ledColors)
                            PreviewColors.Add(new SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(c.R, c.G, c.B)));
                    });

                    // FPS berechnen
                    frames++;
                    if (sw.Elapsed.TotalSeconds >= 1)
                    {
                        double fps = frames / sw.Elapsed.TotalSeconds;
                        App.Current.Dispatcher.Invoke(() => CurrentFps = Math.Round(fps, 1));
                        sw.Restart();
                        frames = 0;
                    }

                    Thread.Sleep(1000 / 30); // Ziel: 30 FPS
                }
            }, token);
        }

        private void StopTest()
        {
            _ambilightCts?.Cancel();
            CurrentFps = 0;
        }

        // ==== PropertyChanged ==== 
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
