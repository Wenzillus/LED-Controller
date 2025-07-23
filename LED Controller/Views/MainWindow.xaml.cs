using LED_Controller.ViewModels;
using System.Windows;

namespace LED_Controller.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
        private void OpenAmbilightWindow_Click(object sender, RoutedEventArgs e)
        {
            // Erzeuge das ViewModel
            var ambilightVm = new LED_Controller.ViewModels.AmbilightViewModel();
            // Erzeuge das Fenster und setze den DataContext
            var window = new LED_Controller.Views.AmbilightWindow
            {
                DataContext = ambilightVm
            };
            window.Show();
        }

    }

}