using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LED_Controller.Models
{
    public class LedController : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _ipAddress = "";
        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        private int _port = 4210;
        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public List<LedStrip> Strips { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}