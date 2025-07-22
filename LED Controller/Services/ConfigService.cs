using LED_Controller.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace LED_Controller.Services
{
    public static class ConfigService
    {
        public static void SaveConfiguration(string path, ObservableCollection<LedController> controllers)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(controllers, options));
        }

        public static ObservableCollection<LedController> LoadConfiguration(string path)
        {
            if (!File.Exists(path)) return new ObservableCollection<LedController>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ObservableCollection<LedController>>(File.ReadAllText(path), options)
                ?? new ObservableCollection<LedController>();
        }
    }
}
