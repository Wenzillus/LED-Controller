using System.Collections.Generic;

namespace LED_Controller.Models
{
    /// <summary>
    /// Mapping und Einstellungen eines Strips für Ambilight.
    /// </summary>
    public class AmbilightMapping
    {
        public AmbilightMode Mode { get; set; } = AmbilightMode.Ring;
        public List<AmbilightZone> Zones { get; set; } = new();

        /// <summary>
        /// Für alle Modi: Breite/Höhe der Sampling-Fläche je LED.
        /// </summary>
        public int ZoneWidth { get; set; } = 10;
        public int ZoneHeight { get; set; } = 10;

        /// <summary>
        /// Abstand vom Rand (für Kreis/Linie).
        /// </summary>
        public int MarginPx { get; set; } = 0;

        /// <summary>
        /// Für Ring/Linie: Start-LED-Nummer (0-basiert), z.B. "Fange mit LED 7 an".
        /// </summary>
        public int StartLedIndex { get; set; } = 0;

        /// <summary>
        /// Für Kreis: Richtung (im/gegen Uhrzeigersinn).
        /// </summary>
        public bool Clockwise { get; set; } = true;

        /// <summary>
        /// Für Linie: Horizontal (true) oder Vertikal (false).
        /// </summary>
        public bool IsHorizontal { get; set; } = true;
        /// <summary>
        /// Für Linie: Richtung (true = L→R oder T→B, false = R→L oder B→T).
        /// </summary>
        public bool Forward { get; set; } = true;
    }
}
