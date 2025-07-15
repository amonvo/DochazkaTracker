using System;
using System.Collections.Generic;
using System.Linq;
using DochazkaTracker.Models;

namespace DochazkaTracker.Services
{
    public class AnalyticsService
    {
        public DetailniStatistiky VypocitejDetailniStatistiky(List<Dochazka> dochazky, DateTime od, DateTime do_)
        {
            var filtrovane = dochazky.Where(d => d.Prichod.Date >= od.Date && d.Prichod.Date <= do_.Date).ToList();

            if (!filtrovane.Any())
            {
                return new DetailniStatistiky();
            }

            var statistiky = new DetailniStatistiky
            {
                CelkemPracovnichDni = filtrovane.Count(),
                CelkemOdpracovanoHodin = filtrovane.Sum(d => d.Rozdil.TotalHours),
                PrumernaDelkaPracovnihoDne = filtrovane.Average(d => d.Rozdil.TotalHours),
                NejkratsiDen = filtrovane.Min(d => d.Rozdil),
                NejdelsiDen = filtrovane.Max(d => d.Rozdil),
                PrumernyPrichod = filtrovane.Average(d => d.Prichod.Hour + d.Prichod.Minute / 60.0),
                TrendPrescasu = VypocitejTrend(filtrovane),
                Doporuceni = GenerujDoporuceni(filtrovane)
            };

            // Výpočet průměrného odchodu pouze pro záznamy s odchodem
            var sOdchodem = filtrovane.Where(d => d.Odchod.HasValue).ToList();
            if (sOdchodem.Any())
            {
                statistiky.PrumernyOdchod = sOdchodem.Average(d => d.Odchod.Value.Hour + d.Odchod.Value.Minute / 60.0);
            }

            // Výpočet statistik po dnech
            statistiky.StatistikyPoDnech = filtrovane
                .GroupBy(d => d.Prichod.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Average(d => d.Rozdil.TotalHours));

            return statistiky;
        }

        public TrendsData VypocitejTrendy(List<Dochazka> dochazky, int dniZpet = 30)
        {
            var od = DateTime.Today.AddDays(-dniZpet);
            var filtrovane = dochazky.Where(d => d.Prichod.Date >= od)
                .OrderBy(d => d.Prichod.Date)
                .ToList();

            var trendy = new TrendsData
            {
                Datumy = filtrovane.Select(d => d.Prichod.Date).ToList(),
                Hodiny = filtrovane.Select(d => d.Rozdil.TotalHours).ToList()
            };

            // Výpočet klouzavého průměru (7 dní)
            trendy.MovingAverage = VypocitejKlouzavyPrumer(trendy.Hodiny, 7);

            return trendy;
        }

        private double VypocitejTrend(List<Dochazka> dochazky)
        {
            if (dochazky.Count() < 2) return 0;

            var serazene = dochazky.OrderBy(d => d.Prichod.Date).ToList();
            var prvniPolovina = serazene.Take(serazene.Count() / 2).Average(d => d.Rozdil.TotalHours);
            var druhaPolovina = serazene.Skip(serazene.Count() / 2).Average(d => d.Rozdil.TotalHours);

            return druhaPolovina - prvniPolovina;
        }

        private List<double> VypocitejKlouzavyPrumer(List<double> hodnoty, int okno)
        {
            var vysledek = new List<double>();

            for (int i = 0; i < hodnoty.Count(); i++)
            {
                var start = Math.Max(0, i - okno + 1);
                var end = i + 1;
                var prumer = hodnoty.Skip(start).Take(end - start).Average();
                vysledek.Add(prumer);
            }

            return vysledek;
        }

        private List<string> GenerujDoporuceni(List<Dochazka> dochazky)
        {
            var doporuceni = new List<string>();

            if (!dochazky.Any()) return doporuceni;

            var prumernaDelka = dochazky.Average(d => d.Rozdil.TotalHours);
            var prumernyPrichod = dochazky.Average(d => d.Prichod.Hour + d.Prichod.Minute / 60.0);

            if (prumernaDelka > 9)
            {
                doporuceni.Add("Pracujete v průměru více než 9 hodin denně. Zvažte lepší work-life balance.");
            }

            if (prumernyPrichod > 9)
            {
                doporuceni.Add("Často přicházíte později. Zvažte úpravu pracovní doby.");
            }

            var variabilita = dochazky.Select(d => d.Rozdil.TotalHours).ToList();
            if (variabilita.Count() > 1)
            {
                var smerodatnaOdchylka = VypocitejSmerodatnouOdchylku(variabilita);

                if (smerodatnaOdchylka > 2)
                {
                    doporuceni.Add("Vaše pracovní doba je velmi nepravidelná. Snažte se o konzistentnější rozvrh.");
                }
            }

            return doporuceni;
        }

        private double VypocitejSmerodatnouOdchylku(List<double> hodnoty)
        {
            if (!hodnoty.Any()) return 0;

            var prumer = hodnoty.Average();
            var variance = hodnoty.Select(x => Math.Pow(x - prumer, 2)).Average();
            return Math.Sqrt(variance);
        }
    }

    // Datové třídy dávám mimo hlavní třídu!
    public class DetailniStatistiky
    {
        public double PrumernaDelkaPracovnihoDne { get; set; }
        public TimeSpan NejkratsiDen { get; set; }
        public TimeSpan NejdelsiDen { get; set; }
        public Dictionary<DayOfWeek, double> StatistikyPoDnech { get; set; }
        public double TrendPrescasu { get; set; }
        public int CelkemPracovnichDni { get; set; }
        public double CelkemOdpracovanoHodin { get; set; }
        public double PrumernyPrichod { get; set; }
        public double PrumernyOdchod { get; set; }
        public List<string> Doporuceni { get; set; }

        public DetailniStatistiky()
        {
            StatistikyPoDnech = new Dictionary<DayOfWeek, double>();
            Doporuceni = new List<string>();
        }
    }

    public class TrendsData
    {
        public List<DateTime> Datumy { get; set; }
        public List<double> Hodiny { get; set; }
        public List<double> MovingAverage { get; set; }

        public TrendsData()
        {
            Datumy = new List<DateTime>();
            Hodiny = new List<double>();
            MovingAverage = new List<double>();
        }
    }
}
