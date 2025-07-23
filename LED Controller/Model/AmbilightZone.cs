namespace LED_Controller.Models
{
    /// <summary>
    /// Beschreibt einen Bereich am Bildschirm, der einer LED zugeordnet ist.
    /// </summary>
    public class AmbilightZone
    {
        public int LedIndex { get; set; }
        public Rectangle Zone { get; set; } = new();
    }

    /// <summary>
    /// Rechteckiger Bereich (kann auch durch System.Drawing.Rectangle ersetzt werden).
    /// </summary>
    public class Rectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
