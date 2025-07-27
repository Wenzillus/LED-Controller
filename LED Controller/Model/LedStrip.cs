using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LED_Controller.Models
{
    public class LedStrip : INotifyPropertyChanged
    {
        private bool _ambilightActive;

        public string Name { get; set; } = "";
        public int LedCount { get; set; }
        public LedType Type { get; set; }

        [JsonIgnore]
        public LedController? Controller { get; set; }

        public bool AmbilightActive
        {
            get => _ambilightActive;
            set
            {
                if (_ambilightActive != value)
                {
                    _ambilightActive = value;
                    OnPropertyChanged(nameof(AmbilightActive));
                }
            }
        }

        public AmbilightMapping AmbilightMapping { get; set; } = new AmbilightMapping();
        public string AmbilightScreenName { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
