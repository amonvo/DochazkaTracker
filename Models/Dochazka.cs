using System;

namespace DochazkaTracker.Models
{
    public class Dochazka
    {
        public DateTime Prichod { get; set; }
        public DateTime? Odchod { get; set; }
        public TimeSpan Rozdil { get; private set; }
        public string Rezim { get; set; } = "Kancelář";

        // Pomocné vlastnosti
        public bool JeUplna => Odchod.HasValue;
        public string FormatovanyRozdil => $"{(int)Rozdil.TotalHours:D2}:{Rozdil.Minutes:D2}";

        /// <summary>
        /// Vypočítá rozdíl mezi příchodem a odchodem
        /// </summary>
        public void VypocetRozdilu()
        {
            if (Odchod.HasValue)
            {
                Rozdil = Odchod.Value - Prichod;
            }
        }

        /// <summary>
        /// Kontroluje, zda je záznam docházky platný
        /// </summary>
        /// <returns>True pokud je záznam platný</returns>
        public bool JePlatna()
        {
            if (!Odchod.HasValue) return false;
            if (Odchod <= Prichod) return false;
            if (Rozdil.TotalHours > 24) return false; // Rozumná kontrola
            return true;
        }

        /// <summary>
        /// Zjišťuje, zda má zaměstnanec přesčas
        /// </summary>
        /// <param name="standardniPracovniDen">Standardní pracovní doba v hodinách</param>
        /// <returns>True pokud má přesčas</returns>
        public bool MaPrescas(double standardniPracovniDen = 8.5)
        {
            return Rozdil.TotalHours > standardniPracovniDen;
        }

        /// <summary>
        /// Vrací textový popis stavu pracovní doby
        /// </summary>
        /// <param name="standardniPracovniDen">Standardní pracovní doba v hodinách</param>
        /// <returns>Textový popis s detaily o přesčasu nebo deficitu</returns>
        public string GetStatusText(double standardniPracovniDen = 8.5)
        {
            if (Rozdil.TotalHours >= standardniPracovniDen)
            {
                var prescas = Rozdil.Subtract(TimeSpan.FromHours(standardniPracovniDen));
                return $"Přesčas: +{(int)prescas.TotalHours} hodin a {prescas.Minutes} minut, " +
                       $"doporučený odchod: {Prichod.AddHours(standardniPracovniDen):HH:mm}";
            }
            else
            {
                var deficit = TimeSpan.FromHours(standardniPracovniDen).Subtract(Rozdil);
                return $"Minus: -{(int)deficit.TotalHours} hodin a {deficit.Minutes} minut, " +
                       $"potřebný odchod: {Prichod.AddHours(standardniPracovniDen):HH:mm}";
            }
        }

        /// <summary>
        /// Vrací krátký textový popis stavu
        /// </summary>
        /// <param name="standardniPracovniDen">Standardní pracovní doba v hodinách</param>
        /// <returns>Krátký popis (Přesčas/Deficit)</returns>
        public string GetKratkyStatus(double standardniPracovniDen = 8.5)
        {
            if (Rozdil.TotalHours >= standardniPracovniDen)
            {
                var prescas = Rozdil.Subtract(TimeSpan.FromHours(standardniPracovniDen));
                return $"+{(int)prescas.TotalHours}:{prescas.Minutes:D2}";
            }
            else
            {
                var deficit = TimeSpan.FromHours(standardniPracovniDen).Subtract(Rozdil);
                return $"-{(int)deficit.TotalHours}:{deficit.Minutes:D2}";
            }
        }

        /// <summary>
        /// Vypočítá doporučený čas odchodu pro splnění standardní pracovní doby
        /// </summary>
        /// <param name="standardniPracovniDen">Standardní pracovní doba v hodinách</param>
        /// <returns>Doporučený čas odchodu</returns>
        public DateTime GetDoporucenyOdchod(double standardniPracovniDen = 8.5)
        {
            return Prichod.AddHours(standardniPracovniDen);
        }

        /// <summary>
        /// Vrací barvu pro zobrazení podle stavu (přesčas/deficit)
        /// </summary>
        /// <param name="standardniPracovniDen">Standardní pracovní doba v hodinách</param>
        /// <returns>Název barvy jako string</returns>
        public string GetStatusColor(double standardniPracovniDen = 8.5)
        {
            return MaPrescas(standardniPracovniDen) ? "Green" : "Red";
        }

        /// <summary>
        /// Převede objekt na string pro ladění
        /// </summary>
        /// <returns>String reprezentace objektu</returns>
        public override string ToString()
        {
            return $"Dochazka: {Prichod:dd.MM.yyyy HH:mm} - {Odchod?.ToString("HH:mm") ?? "N/A"} " +
                   $"({FormatovanyRozdil}) [{Rezim}]";
        }

        /// <summary>
        /// Porovnává dva objekty Dochazka
        /// </summary>
        /// <param name="obj">Objekt k porovnání</param>
        /// <returns>True pokud jsou objekty stejné</returns>
        public override bool Equals(object obj)
        {
            if (obj is Dochazka other)
            {
                return Prichod == other.Prichod &&
                       Odchod == other.Odchod &&
                       Rezim == other.Rezim;
            }
            return false;
        }

        /// <summary>
        /// Vrací hash kód objektu
        /// </summary>
        /// <returns>Hash kód</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Prichod, Odchod, Rezim);
        }
    }
}
