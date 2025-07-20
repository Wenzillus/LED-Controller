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
    }
}