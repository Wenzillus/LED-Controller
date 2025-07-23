using System.Net.Sockets;
using LED_Controller.Models;
using System.Drawing;
using System.Collections.Generic;

namespace LED_Controller.Services
{
    public static class AmbilightUdpService
    {
        // Sendet eine komplette LED-Farbliste an den Controller
        public static void SendFrame(LedController controller, LedStrip strip, IList<Color> ledColors)
        {
            using var client = new UdpClient();
            int bytesPerLed = strip.Type == LedType.RGBW ? 4 : 3;
            var data = new byte[ledColors.Count * bytesPerLed];
            for (int i = 0; i < ledColors.Count; i++)
            {
                data[i * bytesPerLed + 0] = ledColors[i].R;
                data[i * bytesPerLed + 1] = ledColors[i].G;
                data[i * bytesPerLed + 2] = ledColors[i].B;
                if (bytesPerLed == 4)
                    data[i * bytesPerLed + 3] = 0; // Weiß-Kanal, ggf. clever berechnen!
            }
            client.Send(data, data.Length, controller.IpAddress, controller.Port);
        }
    }
}
