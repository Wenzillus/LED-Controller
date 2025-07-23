namespace LED_Controller.Models
{
    /// <summary>
    /// Verschiedene Mapping-Modi für Ambilight.
    /// </summary>
    public enum AmbilightMode
    {
        Ring,      // LEDs um den Rand (Kreis)
        Line,      // LEDs auf einer Linie (horizontal oder vertikal)
        Free       // Frei definierte Bereiche für jede LED
    }
}
