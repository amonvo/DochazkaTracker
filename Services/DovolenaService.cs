using DochazkaTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DochazkaTracker.Services
{
    public class DovolenaService
    {
        private readonly AppConfig _config;
        private readonly DochazkaService _dochazkaService;

        public DovolenaService(AppConfig config, DochazkaService dochazkaService)
        {
            _config = config;
            _dochazkaService = dochazkaService;
        }

        public List<PlanovanaDovolena> GetPlanovaneDovolene()
        {
            return _config.PlanovaneDovolene?.ToList() ?? new List<PlanovanaDovolena>();
        }

        public bool PridejPlanovanouDovolenu(DateTime od, DateTime doDatum, string popis)
        {
            if (_config.PlanovaneDovolene == null)
            {
                _config.PlanovaneDovolene = new List<PlanovanaDovolena>();
            }

            if (MaOverlap(od, doDatum))
            {
                return false;
            }

            var novaDovolena = new PlanovanaDovolena
            {
                Od = od,
                Do = doDatum,
                Popis = popis ?? "",
                Schvalena = false,
                DatumVytvoreni = DateTime.Now
            };

            _config.PlanovaneDovolene.Add(novaDovolena);
            _config.Save();
            return true;
        }

        public void OdstranPlanovanouDovolenu(PlanovanaDovolena dovolena)
        {
            if (_config.PlanovaneDovolene != null)
            {
                _config.PlanovaneDovolene.Remove(dovolena);
                _config.Save();
            }
        }

        public void SchvalDovolenu(PlanovanaDovolena dovolena)
        {
            if (dovolena != null)
            {
                dovolena.Schvalena = true;
                _config.Save();
            }
        }

        public bool MaOverlap(DateTime od, DateTime doDatum)
        {
            if (_config.PlanovaneDovolene == null)
            {
                return false;
            }

            return _config.PlanovaneDovolene.Any(d =>
                (od <= d.Do && doDatum >= d.Od));
        }

        public int GetPocetDniCerpaneDovolene(int rok)
        {
            var dochazky = _dochazkaService.GetAll();
            return dochazky?.Count(d => d.Prichod.Year == rok && d.Rezim == "Dovolená") ?? 0;
        }

        public int GetPocetDniPlanovaneDovolene(int rok)
        {
            if (_config.PlanovaneDovolene == null)
            {
                return 0;
            }

            return _config.PlanovaneDovolene
                .Where(d => d.Od.Year == rok && d.Schvalena)
                .Sum(d => d.PocetDni);
        }

        public int GetZbyvajiVacationDays(int rok)
        {
            int cerpane = GetPocetDniCerpaneDovolene(rok);
            int planovane = GetPocetDniPlanovaneDovolene(rok);
            return _config.RocniDovolena - cerpane - planovane;
        }

        public List<DateTime> GetPracovniDny(DateTime od, DateTime doDatum)
        {
            var dny = new List<DateTime>();

            for (var datum = od; datum <= doDatum; datum = datum.AddDays(1))
            {
                if (datum.DayOfWeek != DayOfWeek.Saturday && datum.DayOfWeek != DayOfWeek.Sunday)
                {
                    dny.Add(datum);
                }
            }

            return dny;
        }

        public List<PlanovanaDovolena> GetSchvalenouDovolenu(int rok)
        {
            if (_config.PlanovaneDovolene == null)
            {
                return new List<PlanovanaDovolena>();
            }

            return _config.PlanovaneDovolene
                .Where(d => d.Od.Year == rok && d.Schvalena)
                .OrderBy(d => d.Od)
                .ToList();
        }

        public List<PlanovanaDovolena> GetNeschvalenouDovolenu(int rok)
        {
            if (_config.PlanovaneDovolene == null)
            {
                return new List<PlanovanaDovolena>();
            }

            return _config.PlanovaneDovolene
                .Where(d => d.Od.Year == rok && !d.Schvalena)
                .OrderBy(d => d.Od)
                .ToList();
        }

        public bool JeVolnyDen(DateTime datum)
        {
            if (datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday)
            {
                return true;
            }

            if (JeStatniSvatek(datum))
            {
                return true;
            }

            if (_config.PlanovaneDovolene != null)
            {
                return _config.PlanovaneDovolene.Any(d =>
                    d.Schvalena && datum >= d.Od && datum <= d.Do);
            }

            return false;
        }

        private bool JeStatniSvatek(DateTime datum)
        {
            var svatky = new[]
            {
                new DateTime(datum.Year, 1, 1),
                new DateTime(datum.Year, 5, 1),
                new DateTime(datum.Year, 5, 8),
                new DateTime(datum.Year, 7, 5),
                new DateTime(datum.Year, 7, 6),
                new DateTime(datum.Year, 9, 28),
                new DateTime(datum.Year, 10, 28),
                new DateTime(datum.Year, 11, 17),
                new DateTime(datum.Year, 12, 24),
                new DateTime(datum.Year, 12, 25),
                new DateTime(datum.Year, 12, 26)
            };

            return svatky.Contains(datum.Date);
        }

        public int GetPocetPracovnichDni(DateTime od, DateTime doDatum)
        {
            int pocet = 0;

            for (var datum = od; datum <= doDatum; datum = datum.AddDays(1))
            {
                if (!JeVolnyDen(datum))
                {
                    pocet++;
                }
            }

            return pocet;
        }

        public string GetDovolenaStatus(int rok)
        {
            int cerpane = GetPocetDniCerpaneDovolene(rok);
            int planovane = GetPocetDniPlanovaneDovolene(rok);
            int zbyvajici = GetZbyvajiVacationDays(rok);
            int celkem = _config.RocniDovolena;

            return $"Dovolená za {rok}:\n" +
                   $"Celkem: {celkem} dní\n" +
                   $"Čerpáno: {cerpane} dní\n" +
                   $"Naplánováno: {planovane} dní\n" +
                   $"Zbývá: {zbyvajici} dní";
        }

        public bool MuzeNaplanovat(DateTime od, DateTime doDatum)
        {
            if (MaOverlap(od, doDatum))
            {
                return false;
            }

            int potrebneDny = GetPocetPracovnichDni(od, doDatum);
            int zbyvajiciDny = GetZbyvajiVacationDays(od.Year);

            return potrebneDny <= zbyvajiciDny;
        }

        public void VymazVsechnuDovolenu()
        {
            if (_config.PlanovaneDovolene != null)
            {
                _config.PlanovaneDovolene.Clear();
                _config.Save();
            }
        }

        public List<PlanovanaDovolena> GetDovolenaProMesic(int rok, int mesic)
        {
            if (_config.PlanovaneDovolene == null)
            {
                return new List<PlanovanaDovolena>();
            }

            return _config.PlanovaneDovolene
                .Where(d => (d.Od.Year == rok && d.Od.Month == mesic) ||
                           (d.Do.Year == rok && d.Do.Month == mesic))
                .OrderBy(d => d.Od)
                .ToList();
        }
    }
}
