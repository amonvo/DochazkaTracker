using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DochazkaTracker
{
    public partial class MainWindow : Window
    {
        private List<Dochazka> dochazky = new List<Dochazka>();

        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            MessageBox.Show($"Dnes je {DateTime.Now.ToShortDateString()}", "Dnešní datum", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPrichod_Click(object sender, RoutedEventArgs e)
        {
            string prichodStr = Microsoft.VisualBasic.Interaction.InputBox("V kolik jste přišli do práce? (HH:mm)", "Příchod", DateTime.Now.ToString("HH:mm"));
            if (DateTime.TryParse(prichodStr, out DateTime prichod))
            {
                dochazky.Add(new Dochazka { Prichod = prichod });
            }
            else
            {
                MessageBox.Show("Zadaný čas není platný.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOdchod_Click(object sender, RoutedEventArgs e)
        {
            string odchodStr = Microsoft.VisualBasic.Interaction.InputBox("V kolik jste odešli z práce? (HH:mm)", "Odchod", DateTime.Now.ToString("HH:mm"));
            if (DateTime.TryParse(odchodStr, out DateTime odchod))
            {
                if (dochazky.Count > 0)
                {
                    Dochazka posledni = dochazky[dochazky.Count - 1];
                    posledni.Odchod = odchod;
                    posledni.VypocetRozdilu();

                    if (posledni.Rozdil.TotalHours >= 9)
                    {
                        MessageBox.Show($"Pracovní doba: {posledni.Rozdil}", "Pracovní doba", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Pracovní doba: {posledni.Rozdil}\nChtěli byste zůstat déle?", "Pracovní doba", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Nejprve musíte zadat příchod.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Zadaný čas není platný.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportovat_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "dochazka.xlsx";
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Docházka");
                worksheet.Cells[1, 1].Value = "Datum";
                worksheet.Cells[1, 2].Value = "Příchod";
                worksheet.Cells[1, 3].Value = "Odchod";
                worksheet.Cells[1, 4].Value = "Rozdíl";

                int row = 2;
                foreach (Dochazka dochazka in dochazky)
                {
                    worksheet.Cells[row, 1].Value = dochazka.Prichod.ToShortDateString();
                    worksheet.Cells[row, 2].Value = dochazka.Prichod.ToString("HH:mm");
                    worksheet.Cells[row, 3].Value = dochazka.Odchod?.ToString("HH:mm");
                    worksheet.Cells[row, 4].Value = dochazka.Rozdil.ToString();
                    row++;
                }

                File.WriteAllBytes(filePath, package.GetAsByteArray());
            }
            MessageBox.Show($"Docházka byla exportována do souboru {filePath}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class Dochazka
    {
        public DateTime Prichod { get; set; }
        public DateTime? Odchod { get; set; }
        public TimeSpan Rozdil { get; private set; }

        public void VypocetRozdilu()
        {
            if (Odchod.HasValue)
            {
                Rozdil = Odchod.Value - Prichod;
            }
        }
    }
}
