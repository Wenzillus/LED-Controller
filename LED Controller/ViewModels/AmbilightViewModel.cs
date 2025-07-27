using LED_Controller.Models;
using LED_Controller.Relay;
using LED_Controller.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

using DColor = System.Drawing.Color;
using MColor = System.Windows.Media.Color;

namespace LED_Controller.ViewModels
{
    /// <summary>
    /// ViewModel für Ambilight-Einstellungen.
    /// </summary>
    public class AmbilightViewModel : INotifyPropertyChanged
    {
        public static Array AmbilightModeValues => Enum.GetValues(typeof(AmbilightMode));

        // === Monitor-Auswahl ===
        public ObservableCollection<Screen> Screens { get; }
        private Screen? _selectedScreen;
        public Screen? SelectedScreen
        {
            get => _selectedScreen;
            set
            {
                _selectedScreen = value;
                OnPropertyChanged();
                if (value != null)
                {
                    // Recalculate all zones for new screen
                    CalculateAllZones(value);
                    // Update overlay if open
                    ZonesUpdated?.Invoke();
                }
            }
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

        // === Kommandos ===
        public ICommand StartTestCommand { get; }
        public ICommand StopTestCommand { get; }

        // === Intern ===
        private CancellationTokenSource? _ambilightCts;

        // Callback für Overlay-Updates
        public Action? ZonesUpdated { get; set; }

        // === Konstruktoren ===
        public AmbilightViewModel()
        {
            Screens = new ObservableCollection<Screen>(Screen.AllScreens);
            Strips = new ObservableCollection<AmbilightStripConfig>();
            // (No default selection in design mode)
            StartTestCommand = new RelayCommand(StartTest);
            StopTestCommand = new RelayCommand(StopTest);
        }

        public AmbilightViewModel(ObservableCollection<LedController> controllers)
        {
            Screens = new ObservableCollection<Screen>(Screen.AllScreens);
            Strips = new ObservableCollection<AmbilightStripConfig>();
            // Populate strips from all controllers
            foreach (var controller in controllers)
            {
                foreach (var strip in controller.Strips)
                {
                    Strips.Add(new AmbilightStripConfig { Strip = strip, Mapping = strip.AmbilightMapping });
                }
            }
            // Default selections
            if (Screens.Count > 0)
            {
                SelectedScreen = Screens[0];
            }
            if (Strips.Count > 0)
            {
                SelectedStrip = Strips[0];
            }
            // Subscribe to mapping property changes for auto-updating zones
            foreach (var stripConfig in Strips)
            {
                stripConfig.Mapping.PropertyChanged += AmbilightMappingChanged;
            }
            // Commands
            StartTestCommand = new RelayCommand(StartTest);
            StopTestCommand = new RelayCommand(StopTest);
        }

        // === Methoden ===

        private void AmbilightMappingChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is AmbilightMapping mapping)
            {
                // Find which strip config this mapping belongs to
                var stripConfig = Strips.FirstOrDefault(s => s.Mapping == mapping);
                if (stripConfig == null) return;
                // Recalculate zones for this strip (skip if Free mode)
                if (stripConfig.Mapping.Mode == AmbilightMode.Free)
                {
                    // Clear zones for free mode (manual configuration)
                    stripConfig.Mapping.Zones.Clear();
                }
                else if (SelectedScreen != null)
                {
                    CalculateZonesForStrip(stripConfig, SelectedScreen);
                }
                // Update the strip's associated screen name (current selected screen)
                if (SelectedScreen != null)
                {
                    stripConfig.Strip.AmbilightScreenName = SelectedScreen.DeviceName;
                }
                // Refresh overlay preview
                ZonesUpdated?.Invoke();
            }
        }

        private void CalculateAllZones(Screen screen)
        {
            foreach (var stripConfig in Strips)
            {
                if (stripConfig.Mapping.Mode == AmbilightMode.Free)
                {
                    stripConfig.Mapping.Zones.Clear();
                }
                else
                {
                    CalculateZonesForStrip(stripConfig, screen);
                }
                // Assign screen name for each strip's configuration
                stripConfig.Strip.AmbilightScreenName = screen.DeviceName;
            }
        }

        private void CalculateZonesForStrip(AmbilightStripConfig config, Screen screen)
        {
            var strip = config.Strip;
            var mapping = config.Mapping;
            int ledCount = strip.LedCount;
            // Clear any existing zones
            mapping.Zones.Clear();
            if (ledCount <= 0) return;

            // Get screen dimensions
            var bounds = screen.Bounds;
            int screenWidth = bounds.Width;
            int screenHeight = bounds.Height;

            // Auto-calculate zones based on mapping mode
            if (mapping.Mode == AmbilightMode.Ring)
            {
                // Ring mode: distribute around all 4 edges
                int margin = mapping.MarginPx;
                // Ensure margin not larger than half dimension
                if (margin * 2 >= screenWidth) margin = screenWidth / 2;
                if (margin * 2 >= screenHeight) margin = screenHeight / 2;
                double topLen = Math.Max(0, screenWidth - 2 * margin);
                double sideLen = Math.Max(0, screenHeight - 2 * margin);
                double perimeter = 2 * topLen + 2 * sideLen;
                if (perimeter <= 0)
                {
                    return;
                }
                // Allocate LED counts proportionally to side lengths
                int countTop = (int)Math.Round(ledCount * (topLen / perimeter));
                int countRight = (int)Math.Round(ledCount * (sideLen / perimeter));
                int countBottom = (int)Math.Round(ledCount * (topLen / perimeter));
                int countLeft = (int)Math.Round(ledCount * (sideLen / perimeter));
                // Adjust for rounding differences
                int totalAssigned = countTop + countRight + countBottom + countLeft;
                int diff = ledCount - totalAssigned;
                while (diff != 0)
                {
                    if (diff > 0)
                    {
                        // Add LED to the longest side
                        if (topLen >= sideLen && topLen >= sideLen)
                        {
                            countTop++;
                        }
                        else if (sideLen >= topLen)
                        {
                            countRight++;
                        }
                        else
                        {
                            countBottom++;
                        }
                        diff--;
                    }
                    else // diff < 0
                    {
                        if (countTop > 0 && topLen >= sideLen)
                        {
                            countTop--;
                        }
                        else if (countRight > 0 && sideLen >= topLen)
                        {
                            countRight--;
                        }
                        else if (countBottom > 0)
                        {
                            countBottom--;
                        }
                        else if (countLeft > 0)
                        {
                            countLeft--;
                        }
                        diff++;
                    }
                }
                // Use zone dimensions from mapping (cap to screen size)
                int zoneW = Math.Min(mapping.ZoneWidth, screenWidth);
                int zoneH = Math.Min(mapping.ZoneHeight, screenHeight);
                bool clockwise = mapping.Clockwise;
                // Generate zones around edges
                if (clockwise)
                {
                    // Top side (left→right)
                    for (int i = 0; i < countTop; i++)
                    {
                        int x = margin + (int)Math.Round((screenWidth - 2 * margin - zoneW) * (i / (double)Math.Max(1, countTop - 1)));
                        int y = margin;
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    // Right side (top→bottom)
                    for (int i = 0; i < countRight; i++)
                    {
                        int x = screenWidth - margin - zoneW;
                        int y = margin + (int)Math.Round((screenHeight - 2 * margin - zoneH) * (i / (double)Math.Max(1, countRight - 1)));
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    // Bottom side (right→left)
                    for (int i = 0; i < countBottom; i++)
                    {
                        int x = screenWidth - margin - zoneW - (int)Math.Round((screenWidth - 2 * margin - zoneW) * (i / (double)Math.Max(1, countBottom - 1)));
                        int y = screenHeight - margin - zoneH;
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    // Left side (bottom→top)
                    for (int i = 0; i < countLeft; i++)
                    {
                        int x = margin;
                        int y = screenHeight - margin - zoneH - (int)Math.Round((screenHeight - 2 * margin - zoneH) * (i / (double)Math.Max(1, countLeft - 1)));
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                }
                else
                {
                    // Counter-clockwise: start at top-left going left side downwards
                    for (int i = 0; i < countLeft; i++)
                    {
                        int x = margin;
                        int y = margin + (int)Math.Round((screenHeight - 2 * margin - zoneH) * (i / (double)Math.Max(1, countLeft - 1)));
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    for (int i = 0; i < countBottom; i++)
                    {
                        int x = margin + (int)Math.Round((screenWidth - 2 * margin - zoneW) * (i / (double)Math.Max(1, countBottom - 1)));
                        int y = screenHeight - margin - zoneH;
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    for (int i = 0; i < countRight; i++)
                    {
                        int x = screenWidth - margin - zoneW;
                        int y = screenHeight - margin - zoneH - (int)Math.Round((screenHeight - 2 * margin - zoneH) * (i / (double)Math.Max(1, countRight - 1)));
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    for (int i = 0; i < countTop; i++)
                    {
                        int x = screenWidth - margin - zoneW - (int)Math.Round((screenWidth - 2 * margin - zoneW) * (i / (double)Math.Max(1, countTop - 1)));
                        int y = margin;
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                }
                // Assign LED indices with offset and sort by index
                int totalZones = mapping.Zones.Count;
                for (int i = 0; i < totalZones; i++)
                {
                    int ledIndex = (i + mapping.StartLedIndex) % totalZones;
                    mapping.Zones[i].LedIndex = ledIndex;
                }
                mapping.Zones.Sort((a, b) => a.LedIndex.CompareTo(b.LedIndex));
            }
            else if (mapping.Mode == AmbilightMode.Line)
            {
                // Line mode: horizontal or vertical
                int margin = mapping.MarginPx;
                int zoneW = Math.Min(mapping.ZoneWidth, screenWidth);
                int zoneH = Math.Min(mapping.ZoneHeight, screenHeight);
                mapping.Zones.Clear();
                if (mapping.IsHorizontal)
                {
                    // Horizontal line (assume top edge if Forward = true, bottom edge if false)
                    int y = mapping.Forward ? margin : (screenHeight - margin - zoneH);
                    y = Math.Max(0, Math.Min(screenHeight - zoneH, y));
                    for (int i = 0; i < ledCount; i++)
                    {
                        int x = margin + (int)Math.Round((screenWidth - 2 * margin - zoneW) * (i / (double)Math.Max(1, ledCount - 1)));
                        x = Math.Max(margin, Math.Min(screenWidth - margin - zoneW, x));
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    // If Forward is false (bottom edge, right→left for LED order), reverse zone order for correct LED indexing
                    if (!mapping.Forward)
                    {
                        mapping.Zones.Reverse();
                    }
                }
                else
                {
                    // Vertical line (assume left edge if Forward = true, right edge if false)
                    int x = mapping.Forward ? margin : (screenWidth - margin - zoneW);
                    x = Math.Max(0, Math.Min(screenWidth - zoneW, x));
                    for (int i = 0; i < ledCount; i++)
                    {
                        int y = margin + (int)Math.Round((screenHeight - 2 * margin - zoneH) * (i / (double)Math.Max(1, ledCount - 1)));
                        y = Math.Max(margin, Math.Min(screenHeight - margin - zoneH, y));
                        mapping.Zones.Add(new AmbilightZone { LedIndex = 0, Zone = new LED_Controller.Models.Rectangle { X = bounds.X + x, Y = bounds.Y + y, Width = zoneW, Height = zoneH } });
                    }
                    if (!mapping.Forward)
                    {
                        mapping.Zones.Reverse();
                    }
                }
                // Apply StartLedIndex offset and sort
                int totalZones = mapping.Zones.Count;
                for (int i = 0; i < totalZones; i++)
                {
                    int ledIndex = (i + mapping.StartLedIndex) % totalZones;
                    mapping.Zones[i].LedIndex = ledIndex;
                }
                mapping.Zones.Sort((a, b) => a.LedIndex.CompareTo(b.LedIndex));
            }
            // For AmbilightMode.Free, zones remain as manually defined (if any)
        }

        private async void StartTest()
        {
            // Start continuous Ambilight test (capture and preview)
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
                    // Capture screen frame
                    var bmp = ScreenCaptureService.CaptureScreen(screen);
                    // Determine color for each LED zone
                    var ledColors = new System.Collections.Generic.List<DColor>();
                    foreach (var zone in mapping.Zones)
                    {
                        var rect = new System.Drawing.Rectangle(zone.Zone.X, zone.Zone.Y, zone.Zone.Width, zone.Zone.Height);
                        ledColors.Add(ScreenCaptureService.AverageColor(bmp, rect));
                    }
                    // Dispose screenshot to free resources
                    bmp.Dispose();
                    // Send colors via UDP
                    AmbilightUdpService.SendFrame(strip.Controller!, strip, ledColors);
                    // Update UI preview on UI thread
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        PreviewColors.Clear();
                        foreach (var dc in ledColors)
                        {
                            var mc = MColor.FromRgb(dc.R, dc.G, dc.B);
                            PreviewColors.Add(new SolidColorBrush(mc));
                        }
                    });
                    // Calculate FPS
                    frames++;
                    if (sw.Elapsed.TotalSeconds >= 1)
                    {
                        double fps = frames / sw.Elapsed.TotalSeconds;
                        App.Current.Dispatcher.Invoke(() => CurrentFps = Math.Round(fps, 1));
                        sw.Restart();
                        frames = 0;
                    }
                    // Limit frame rate (30 FPS target)
                    Thread.Sleep(1000 / 30);
                }
            }, token);
        }

        private void StopTest()
        {
            // Stop Ambilight test
            _ambilightCts?.Cancel();
            CurrentFps = 0;
        }

        // === INotifyPropertyChanged ===
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
