using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LED_Controller.Views
{
    public partial class AmbilightWindow : Window
    {
        private Window? _overlayWindow;
        private Canvas? _overlayCanvas;

        public AmbilightWindow()
        {
            InitializeComponent();
            Loaded += AmbilightWindow_Loaded;
            Closing += AmbilightWindow_Closing;
        }

        private void AmbilightWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AmbilightViewModel vm && vm.SelectedScreen != null)
            {
                // Overlay-Fenster erzeugen
                var screen = vm.SelectedScreen;
                _overlayWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false
                };
                var bounds = screen.Bounds;
                _overlayWindow.Left = bounds.X;
                _overlayWindow.Top = bounds.Y;
                _overlayWindow.Width = bounds.Width;
                _overlayWindow.Height = bounds.Height;
                _overlayWindow.IsHitTestVisible = false;
                _overlayCanvas = new Canvas();
                _overlayWindow.Content = _overlayCanvas;
                _overlayWindow.Show();

                // Callback für Overlay-Updates setzen
                vm.ZonesUpdated = UpdateOverlay;
                // Initiale Zonenanzeige
                UpdateOverlay();
            }
        }

        private void AmbilightWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Close();
                _overlayWindow = null;
            }
            if (DataContext is ViewModels.AmbilightViewModel vm)
            {
                vm.ZonesUpdated = null;
            }
        }

        private void UpdateOverlay()
        {
            if (_overlayWindow == null || _overlayCanvas == null) return;
            if (!(DataContext is ViewModels.AmbilightViewModel vm) || vm.SelectedScreen == null) return;
            // Overlay-Position und Größe ggf. an neuen Bildschirm anpassen
            var screen = vm.SelectedScreen;
            var bounds = screen.Bounds;
            _overlayWindow.Left = bounds.X;
            _overlayWindow.Top = bounds.Y;
            _overlayWindow.Width = bounds.Width;
            _overlayWindow.Height = bounds.Height;
            // Alte Markierungen entfernen
            _overlayCanvas.Children.Clear();
            // Rechtecke für jede LED-Zone zeichnen
            foreach (var stripConfig in vm.Strips)
            {
                foreach (var zone in stripConfig.Mapping.Zones)
                {
                    var rectShape = new System.Windows.Shapes.Rectangle
                    {
                        Width = zone.Zone.Width,
                        Height = zone.Zone.Height,
                        Stroke = System.Windows.Media.Brushes.Red,
                        StrokeThickness = 2,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 0, 0))
                    };
                    // Position relativ zum Bildschirm (Overlay-Fenster)
                    Canvas.SetLeft(rectShape, zone.Zone.X - bounds.X);
                    Canvas.SetTop(rectShape, zone.Zone.Y - bounds.Y);
                    _overlayCanvas.Children.Add(rectShape);
                }
            }
        }
    }
}
