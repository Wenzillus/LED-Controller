namespace LED_Controller.Models
{
    public class LedStrip
    {
        public string Name { get; set; } = "";
        public int LedCount { get; set; }
        public LedType Type { get; set; }
    }
}