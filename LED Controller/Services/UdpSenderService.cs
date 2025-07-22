using System.Net.Sockets;
using System.Net;

namespace LED_Controller.Services
{
    public static class UdpSenderService
    {
        public static bool SendColor(string ip, int port, byte r, byte g, byte b, out string? error)
        {
            error = null;
            try
            {
                using var client = new UdpClient();
                byte[] data = new byte[] { r, g, b };
                client.Send(data, data.Length, ip, port);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
