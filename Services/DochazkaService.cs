using System.Text.Json;

namespace DochazkaTracker.Services
{
    public class DochazkaService
    {
        private const string DataFilePath = "dochazka.json";
        private List<Dochazka> _dochazky = new List<Dochazka>();

        public List<Dochazka> GetAll() => _dochazky.ToList();

        public void Add(Dochazka dochazka)
        {
            _dochazky.Add(dochazka);
            SaveData();
        }

        public void Update(int index, Dochazka dochazka)
        {
            if (index >= 0 && index < _dochazky.Count)
            {
                _dochazky[index] = dochazka;
                SaveData();
            }
        }

        public void Clear()
        {
            _dochazky.Clear();
            SaveData();
        }

        public bool ExistujeDochazkaProDatum(DateTime datum)
        {
            return _dochazky.Any(d => d.Prichod.Date == datum.Date);
        }

        public async Task LoadDataAsync()
        {
            await Task.Run(() => LoadData());
        }

        private void LoadData()
        {
            if (File.Exists(DataFilePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(DataFilePath);
                    _dochazky = JsonSerializer.Deserialize<List<Dochazka>>(jsonData) ?? new List<Dochazka>();

                    foreach (var dochazka in _dochazky)
                    {
                        if (dochazka.Odchod.HasValue)
                        {
                            dochazka.VypocetRozdilu();
                        }
                    }
                }
                catch (Exception)
                {
                    _dochazky = new List<Dochazka>();
                }
            }
        }

        private void SaveData()
        {
            try
            {
                string jsonData = JsonSerializer.Serialize(_dochazky, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataFilePath, jsonData);
            }
            catch (Exception)
            {
                // Ignorujeme chyby při ukládání
            }
        }
    }
}
