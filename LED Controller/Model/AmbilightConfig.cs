using System.Collections.Generic;
using System.Windows.Forms;

namespace LED_Controller.Models
{
    /// <summary>
    /// Gesamtkonfiguration für das Ambilight einer Szene.
    /// </summary>
    public class AmbilightConfig
    {
        public Screen? TargetScreen { get; set; }
        public List<AmbilightStripConfig> Strips { get; set; } = new();
        public int FpsLimit { get; set; } = 30;
    }
}
