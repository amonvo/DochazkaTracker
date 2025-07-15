using System.Text.Json;

namespace DochazkaTracker.Models
{
    public class AppConfig
    {
        public double StandardniPracovniDen { get; set; } = 8.5;
        public string DataFilePath { get; set; } = "dochazka.json";
        public List<string> Rezimy { get; set; } = new List<string>
        {
            "Práce z domova", "Kancelář", "Dovolená", "Služební cesta"
        };

        public static AppConfig Load()
        {
            const string configPath = "config.json";
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    return new AppConfig();
                }
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("config.json", json);
            }
            catch
            {
                // Ignorujeme chyby při ukládání konfigurace
            }
        }
    }
}
