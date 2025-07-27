namespace LED_Controller.Models
{
    /// <summary>
    /// Zuordnung eines LED-Strips zur Ambilight-Mapping-Konfiguration.
    /// </summary>
    public class AmbilightStripConfig
    {
        public LedStrip Strip { get; set; } = null!;

        public AmbilightMapping Mapping { get; set; } = new();
    }
}
