using System.Net.Sockets;
using LED_Controller.Models;
using System.Drawing;
using System.Collections.Generic;

namespace LED_Controller.Services
{
    public static class AmbilightUdpService
    {
        /// <summary>
        /// Sendet alle Farblisten mehrerer Strips in einem einzigen Paket.
        /// </summary>
        public static void SendFrame(
            LedController controller,
            IList<LedStrip> strips,
            IList<IList<Color>> stripsColors)
        {
            // Header-Byte 3
            // Gesamtgröße = 1 Byte + Summe(bytesPerLed * ledCount pro Strip)
            int totalBytes = 1;
            var bytesPerLedList = strips
                .Select(s => s.Type == LedType.RGBW ? 4 : 3)
                .ToList();
            for (int i = 0; i < strips.Count; i++)
                totalBytes += stripsColors[i].Count * bytesPerLedList[i];

            var data = new byte[totalBytes];
            data[0] = 3;

            int pos = 1;
            for (int s = 0; s < strips.Count; s++)
            {
                var colors = stripsColors[s];
                int bpl = bytesPerLedList[s];
                foreach (var c in colors)
                {
                    data[pos++] = c.R;
                    data[pos++] = c.G;
                    data[pos++] = c.B;
                    if (bpl == 4)
                        data[pos++] = 0; // Weiß-Kanal (falls RGBW)
                }
            }

            using var client = new UdpClient();
            client.Send(data, data.Length, controller.IpAddress, controller.Port);
        }
        //// Sendet eine komplette LED-Farbliste an den Controller
        //public static void SendFrame(LedController controller, LedStrip strip, IList<Color> ledColors)
        //{
        //    using var client = new UdpClient();
        //    int bytesPerLed = strip.Type == LedType.RGBW ? 4 : 3;
        //    //var data = new byte[ledColors.Count * bytesPerLed + 1];

        //    var data = new byte[136 * 4 + 60 * 3 + 23 * 3 + 3];
        //    data[0] = 3;
        //    for (int i = 0; i < ledColors.Count ; i++)
        //    {
        //        data[i * bytesPerLed + 1] = ledColors[i].R;
        //        data[i * bytesPerLed + 2] = ledColors[i].G;
        //        data[i * bytesPerLed + 3] = ledColors[i].B;
        //        if (bytesPerLed == 4)
        //            data[i * bytesPerLed + 3] = 0; // Weiß-Kanal, ggf. clever berechnen!
        //    }
        //    client.Send(data, data.Length, controller.IpAddress, controller.Port);
        //}
    }
}
