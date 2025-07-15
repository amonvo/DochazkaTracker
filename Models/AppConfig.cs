using System.Text.Json;

namespace DochazkaTracker.Models
{
    public class AppConfig
    {
        public double StandardniPracovniDen { get; set; } = 8.5;
        public string DataFilePath { get; set; } = "dochazka.json";
        public List<string> Rezimy { get; set; } = new List<string>
        {
            "Práce z domova", "Kancelář", "Dovolená", "Služební cesta", "Nemocenská"
        };

        // Nové vlastnosti
        public int RocniDovolena { get; set; } = 25;
        public List<PlanovanaDovolena> PlanovaneDovolene { get; set; } = new List<PlanovanaDovolena>();
        public List<SickDay> SickDays { get; set; } = new List<SickDay>();

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

    // Nové třídy
    public class PlanovanaDovolena
    {
        public DateTime Od { get; set; }
        public DateTime Do { get; set; }
        public string Popis { get; set; } = "";
        public bool Schvalena { get; set; } = false;
        public DateTime DatumVytvoreni { get; set; } = DateTime.Now;
        public int PocetDni => (int)(Do - Od).TotalDays + 1;
    }

    public class SickDay
    {
        public DateTime Datum { get; set; }
        public string Duvod { get; set; } = "";
        public bool NavstevneLekar { get; set; } = false;
        public string Poznamka { get; set; } = "";
        public DateTime DatumVytvoreni { get; set; } = DateTime.Now;
    }
}