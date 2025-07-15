namespace DochazkaTracker.Services
{
    public class StatistikyService
    {
        private const double PrumernyPracovniDen = 8.5;

        public class MesicniStatistiky
        {
            public int Mesic { get; set; }
            public int Rok { get; set; }
            public double OdpracovaneHodiny { get; set; }
            public double OcekavaneHodiny { get; set; }
            public double RozdiHodin => OdpracovaneHodiny - OcekavaneHodiny;
            public int PocetDni { get; set; }
            public string FormatovanyRozdil => RozdiHodin > 0 ? $"+{RozdiHodin:F1}" : $"{RozdiHodin:F1}";
        }

        public MesicniStatistiky VypocitejMesicniStatistiky(List<Dochazka> dochazky, int mesic, int rok)
        {
            var filtrovane = dochazky.Where(d => d.Prichod.Month == mesic && d.Prichod.Year == rok).ToList();

            return new MesicniStatistiky
            {
                Mesic = mesic,
                Rok = rok,
                PocetDni = filtrovane.Count,
                OdpracovaneHodiny = filtrovane.Sum(d => d.Rozdil.TotalHours),
                OcekavaneHodiny = filtrovane.Count * PrumernyPracovniDen
            };
        }

        public List<string> GetDostupneMesice(List<Dochazka> dochazky)
        {
            return dochazky.Select(d => $"{d.Prichod.Month}/{d.Prichod.Year}")
                          .Distinct()
                          .OrderBy(m => m)
                          .ToList();
        }
    }
}
