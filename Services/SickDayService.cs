using System;
using System.Collections.Generic;
using System.Linq;
using DochazkaTracker.Models;

namespace DochazkaTracker.Services
{
    public class SickDayService
    {
        private readonly AppConfig _config;
        private readonly DochazkaService _dochazkaService;

        public SickDayService(AppConfig config, DochazkaService dochazkaService)
        {
            _config = config;
            _dochazkaService = dochazkaService;
        }

        public List<SickDay> GetSickDays()
        {
            return _config.SickDays?.ToList() ?? new List<SickDay>();
        }

        public bool PridejSickDay(DateTime datum, string duvod, bool navstevneLekar, string poznamka)
        {
            if (_config.SickDays == null)
            {
                _config.SickDays = new List<SickDay>();
            }

            if (_config.SickDays.Any(s => s.Datum.Date == datum.Date))
            {
                return false;
            }

            var sickDay = new SickDay
            {
                Datum = datum,
                Duvod = duvod ?? "",
                NavstevneLekar = navstevneLekar,
                Poznamka = poznamka ?? "",
                DatumVytvoreni = DateTime.Now
            };

            _config.SickDays.Add(sickDay);

            if (!_dochazkaService.ExistujeDochazkaProDatum(datum))
            {
                var dochazka = new Dochazka
                {
                    Prichod = datum.Date.AddHours(8),
                    Odchod = datum.Date.AddHours(16),
                    Rezim = "Nemocenská"
                };
                dochazka.VypocetRozdilu();
                _dochazkaService.Add(dochazka);
            }

            _config.Save();
            return true;
        }

        public void OdstranSickDay(SickDay sickDay)
        {
            if (_config.SickDays != null && sickDay != null)
            {
                _config.SickDays.Remove(sickDay);
                _config.Save();
            }
        }

        public void OdstranSickDayByDatum(DateTime datum)
        {
            if (_config.SickDays != null)
            {
                var sickDay = _config.SickDays.FirstOrDefault(s => s.Datum.Date == datum.Date);
                if (sickDay != null)
                {
                    _config.SickDays.Remove(sickDay);
                    _config.Save();
                }
            }
        }

        public int GetPocetSickDays(int rok)
        {
            return _config.SickDays?.Count(s => s.Datum.Year == rok) ?? 0;
        }

        public int GetPocetSickDaysThisMonth()
        {
            return _config.SickDays?.Count(s => s.Datum.Year == DateTime.Now.Year &&
                                              s.Datum.Month == DateTime.Now.Month) ?? 0;
        }

        public int GetPocetSickDaysThisYear()
        {
            return GetPocetSickDays(DateTime.Now.Year);
        }

        public List<SickDay> GetSickDaysByMonth(int rok, int mesic)
        {
            if (_config.SickDays == null)
            {
                return new List<SickDay>();
            }

            return _config.SickDays
                .Where(s => s.Datum.Year == rok && s.Datum.Month == mesic)
                .OrderBy(s => s.Datum)
                .ToList();
        }

        public List<SickDay> GetSickDaysByYear(int rok)
        {
            if (_config.SickDays == null)
            {
                return new List<SickDay>();
            }

            return _config.SickDays
                .Where(s => s.Datum.Year == rok)
                .OrderBy(s => s.Datum)
                .ToList();
        }

        public Dictionary<string, int> GetSickDayStatistics(int rok)
        {
            if (_config.SickDays == null)
            {
                return new Dictionary<string, int>();
            }

            return _config.SickDays
                .Where(s => s.Datum.Year == rok)
                .GroupBy(s => s.Duvod)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Dictionary<int, int> GetSickDayStatisticsByMonth(int rok)
        {
            if (_config.SickDays == null)
            {
                return new Dictionary<int, int>();
            }

            return _config.SickDays
                .Where(s => s.Datum.Year == rok)
                .GroupBy(s => s.Datum.Month)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public SickDay GetSickDayByDatum(DateTime datum)
        {
            return _config.SickDays?.FirstOrDefault(s => s.Datum.Date == datum.Date);
        }

        public bool ExistujeSickDayProDatum(DateTime datum)
        {
            return _config.SickDays?.Any(s => s.Datum.Date == datum.Date) ?? false;
        }

        public List<SickDay> GetSickDaysWithDoctor(int rok)
        {
            if (_config.SickDays == null)
            {
                return new List<SickDay>();
            }

            return _config.SickDays
                .Where(s => s.Datum.Year == rok && s.NavstevneLekar)
                .OrderBy(s => s.Datum)
                .ToList();
        }

        public int GetPocetNavstevLekare(int rok)
        {
            return _config.SickDays?.Count(s => s.Datum.Year == rok && s.NavstevneLekar) ?? 0;
        }

        public string GetNejcastejsiDuvod(int rok)
        {
            if (_config.SickDays == null || !_config.SickDays.Any(s => s.Datum.Year == rok))
            {
                return "Žádné data";
            }

            var statistiky = GetSickDayStatistics(rok);
            return statistiky.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        public double GetPrumerSickDaysPerMonth(int rok)
        {
            int celkemDni = GetPocetSickDays(rok);
            return celkemDni / 12.0;
        }

        public List<SickDay> GetSickDaysInRange(DateTime od, DateTime doDatum)
        {
            if (_config.SickDays == null)
            {
                return new List<SickDay>();
            }

            return _config.SickDays
                .Where(s => s.Datum.Date >= od.Date && s.Datum.Date <= doDatum.Date)
                .OrderBy(s => s.Datum)
                .ToList();
        }

        public string GetSickDayReport(int rok)
        {
            var sickDays = GetSickDaysByYear(rok);
            var statistiky = GetSickDayStatistics(rok);
            int celkemDni = sickDays.Count;
            int navstevyLekare = GetPocetNavstevLekare(rok);

            if (celkemDni == 0)
            {
                return $"Žádné nemocenské v roce {rok}";
            }

            var report = $"PŘEHLED NEMOCENSKÝCH ZA ROK {rok}:\n\n";
            report += $"Celkem nemocenských dnů: {celkemDni}\n";
            report += $"Návštěvy lékaře: {navstevyLekare}\n";
            report += $"Průměr za měsíc: {GetPrumerSickDaysPerMonth(rok):F1} dní\n\n";

            report += "NEJČASTĚJŠÍ DŮVODY:\n";
            foreach (var stat in statistiky.OrderByDescending(kvp => kvp.Value))
            {
                report += $"• {stat.Key}: {stat.Value} dní\n";
            }

            return report;
        }

        public void VymazVsechnySickDays()
        {
            if (_config.SickDays != null)
            {
                _config.SickDays.Clear();
                _config.Save();
            }
        }

        public void VymazSickDaysForYear(int rok)
        {
            if (_config.SickDays != null)
            {
                _config.SickDays.RemoveAll(s => s.Datum.Year == rok);
                _config.Save();
            }
        }

        public bool UpdateSickDay(SickDay originalSickDay, string novyDuvod, bool novaNavstevneLekar, string novaPoznamka)
        {
            if (_config.SickDays != null && originalSickDay != null)
            {
                var sickDay = _config.SickDays.FirstOrDefault(s => s.Datum.Date == originalSickDay.Datum.Date);
                if (sickDay != null)
                {
                    sickDay.Duvod = novyDuvod ?? "";
                    sickDay.NavstevneLekar = novaNavstevneLekar;
                    sickDay.Poznamka = novaPoznamka ?? "";
                    _config.Save();
                    return true;
                }
            }
            return false;
        }

        public List<DateTime> GetSickDaysAsDateList(int rok)
        {
            if (_config.SickDays == null)
            {
                return new List<DateTime>();
            }

            return _config.SickDays
                .Where(s => s.Datum.Year == rok)
                .Select(s => s.Datum.Date)
                .OrderBy(d => d)
                .ToList();
        }

        public bool JeSickDay(DateTime datum)
        {
            return ExistujeSickDayProDatum(datum);
        }

        public string GetSickDayInfo(DateTime datum)
        {
            var sickDay = GetSickDayByDatum(datum);
            if (sickDay == null)
            {
                return "Žádná nemocenská pro tento den";
            }

            string info = $"Datum: {sickDay.Datum:dd.MM.yyyy}\n";
            info += $"Důvod: {sickDay.Duvod}\n";
            info += $"Návštěva lékaře: {(sickDay.NavstevneLekar ? "Ano" : "Ne")}\n";

            if (!string.IsNullOrEmpty(sickDay.Poznamka))
            {
                info += $"Poznámka: {sickDay.Poznamka}\n";
            }

            info += $"Vytvořeno: {sickDay.DatumVytvoreni:dd.MM.yyyy HH:mm}";

            return info;
        }

        public int GetPocetSickDaysInCurrentQuarter()
        {
            var today = DateTime.Today;
            var currentQuarter = (today.Month - 1) / 3 + 1;
            var quarterStart = new DateTime(today.Year, (currentQuarter - 1) * 3 + 1, 1);
            var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

            return GetSickDaysInRange(quarterStart, quarterEnd).Count;
        }

        public Dictionary<string, object> GetSickDayDashboard(int rok)
        {
            var dashboard = new Dictionary<string, object>
            {
                ["CelkemDni"] = GetPocetSickDays(rok),
                ["NavstevyLekare"] = GetPocetNavstevLekare(rok),
                ["NejcastejsiDuvod"] = GetNejcastejsiDuvod(rok),
                ["PrumerZaMesic"] = GetPrumerSickDaysPerMonth(rok),
                ["StatistikyPoDuvodech"] = GetSickDayStatistics(rok),
                ["StatistikyPoMesicich"] = GetSickDayStatisticsByMonth(rok),
                ["AktualniTvrtleti"] = GetPocetSickDaysInCurrentQuarter()
            };

            return dashboard;
        }
    }
}
