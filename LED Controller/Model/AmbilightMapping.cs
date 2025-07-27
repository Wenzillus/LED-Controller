using System.Collections.Generic;
using System.ComponentModel;

namespace LED_Controller.Models
{
    /// <summary>
    /// Mapping und Einstellungen eines Strips für Ambilight.
    /// </summary>
    public class AmbilightMapping : INotifyPropertyChanged
    {
        private AmbilightMode _mode = AmbilightMode.Ring;
        private int _zoneWidth = 10;
        private int _zoneHeight = 10;
        private int _marginPx = 0;
        private int _startLedIndex = 0;
        private bool _clockwise = true;
        private bool _isHorizontal = true;
        private bool _forward = true;

        public AmbilightMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnPropertyChanged(nameof(Mode));
                }
            }
        }

        public List<AmbilightZone> Zones { get; set; } = new();

        public int ZoneWidth
        {
            get => _zoneWidth;
            set
            {
                if (_zoneWidth != value)
                {
                    _zoneWidth = value;
                    OnPropertyChanged(nameof(ZoneWidth));
                }
            }
        }

        public int ZoneHeight
        {
            get => _zoneHeight;
            set
            {
                if (_zoneHeight != value)
                {
                    _zoneHeight = value;
                    OnPropertyChanged(nameof(ZoneHeight));
                }
            }
        }

        public int MarginPx
        {
            get => _marginPx;
            set
            {
                if (_marginPx != value)
                {
                    _marginPx = value;
                    OnPropertyChanged(nameof(MarginPx));
                }
            }
        }

        public int StartLedIndex
        {
            get => _startLedIndex;
            set
            {
                if (_startLedIndex != value)
                {
                    _startLedIndex = value;
                    OnPropertyChanged(nameof(StartLedIndex));
                }
            }
        }

        public bool Clockwise
        {
            get => _clockwise;
            set
            {
                if (_clockwise != value)
                {
                    _clockwise = value;
                    OnPropertyChanged(nameof(Clockwise));
                }
            }
        }

        public bool IsHorizontal
        {
            get => _isHorizontal;
            set
            {
                if (_isHorizontal != value)
                {
                    _isHorizontal = value;
                    OnPropertyChanged(nameof(IsHorizontal));
                }
            }
        }

        public bool Forward
        {
            get => _forward;
            set
            {
                if (_forward != value)
                {
                    _forward = value;
                    OnPropertyChanged(nameof(Forward));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
