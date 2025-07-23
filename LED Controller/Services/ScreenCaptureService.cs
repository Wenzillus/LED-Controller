using System.Drawing; // Für Bitmap, Color, Rectangle
using System.Windows.Forms; // Für Screen

namespace LED_Controller.Services
{
    public static class ScreenCaptureService
    {
        // Screenshot des angegebenen Monitors als Bitmap
        public static Bitmap CaptureScreen(Screen screen)
        {
            var bounds = screen.Bounds;
            var bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            return bmp;
        }

        // Durchschnittsfarbe für ein Rechteck im Bitmap
        public static Color AverageColor(Bitmap bmp, Rectangle rect)
        {
            Rectangle r = Rectangle.Intersect(rect, new Rectangle(0, 0, bmp.Width, bmp.Height));
            if (r.Width == 0 || r.Height == 0) return Color.Black;

            long sumR = 0, sumG = 0, sumB = 0, count = 0;
            for (int y = r.Y; y < r.Bottom; y += 2)
                for (int x = r.X; x < r.Right; x += 2)
                {
                    var c = bmp.GetPixel(x, y);
                    sumR += c.R; sumG += c.G; sumB += c.B; count++;
                }
            if (count == 0) return Color.Black;
            return Color.FromArgb((int)(sumR / count), (int)(sumG / count), (int)(sumB / count));
        }
    }
}
