using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Border = System.Windows.Controls.Border;

namespace DochazkaTracker
{
    public partial class MainWindow : Window
    {
        private List<Dochazka> dochazky = new List<Dochazka>();

        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Nastavení vzhledu okna
            this.Title = "Docházka Tracker";
            this.Width = 600;
            this.Height = 500;
            this.Background = new LinearGradientBrush(Colors.SteelBlue, Colors.LightGray, 90);

            // Vytvoření layoutu
            System.Windows.Controls.Border mainBorder = new Border
            {
                BorderBrush = Brushes.DarkSlateGray,
                BorderThickness = new Thickness(3),
                Padding = new Thickness(10),
                Margin = new Thickness(20),
                Background = Brushes.White
            };

            StackPanel mainPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            TextBlock title = new TextBlock
            {
                Text = "Docházka Tracker",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkSlateBlue,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainPanel.Children.Add(title);

            Button btnPrichod = new Button
            {
                Content = "Zaznamenat Příchod",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightGreen, Colors.Green, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.DarkGreen,
                BorderThickness = new Thickness(2),
                //CornerRadius = new CornerRadius(5)
            };
            btnPrichod.Click += BtnPrichod_Click;
            mainPanel.Children.Add(btnPrichod);

            Button btnOdchod = new Button
            {
                Content = "Zaznamenat Odchod",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightCoral, Colors.Red, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.DarkRed,
                BorderThickness = new Thickness(2),
                //CornerRadius = new CornerRadius(5)
            };
            btnOdchod.Click += BtnOdchod_Click;
            mainPanel.Children.Add(btnOdchod);

            Button btnExportovat = new Button
            {
                Content = "Exportovat do Excelu",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightSkyBlue, Colors.SteelBlue, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.DarkBlue,
                BorderThickness = new Thickness(2),
                //CornerRadius = new CornerRadius(5)
            };
            btnExportovat.Click += BtnExportovat_Click;
            mainPanel.Children.Add(btnExportovat);

            mainBorder.Child = mainPanel;
            this.Content = mainBorder;
        }

        private void BtnPrichod_Click(object sender, RoutedEventArgs e)
        {
            string datumStr = Microsoft.VisualBasic.Interaction.InputBox("Zadejte datum (dd.MM.yyyy)", "Datum", DateTime.Now.ToString("dd.MM.yyyy"));
            if (!DateTime.TryParse(datumStr, out DateTime datum))
            {
                MessageBox.Show("Zadané datum není platné.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string prichodStr = Microsoft.VisualBasic.Interaction.InputBox("V kolik jste přišli do práce? (HH:mm)", "Příchod", DateTime.Now.ToString("HH:mm"));
            if (DateTime.TryParse(prichodStr, out DateTime prichod))
            {
                prichod = new DateTime(datum.Year, datum.Month, datum.Day, prichod.Hour, prichod.Minute, prichod.Second);
                if (dochazky.Any(d => d.Prichod.Date == datum.Date))
                {
                    MessageBox.Show("Příchod již byl zaznamenán pro tento den.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                dochazky.Add(new Dochazka { Prichod = prichod });
                MessageBox.Show("Příchod byl úspěšně zaznamenán.", "Příchod", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Zadaný čas není platný.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOdchod_Click(object sender, RoutedEventArgs e)
        {
            if (dochazky.Count == 0)
            {
                MessageBox.Show("Nejprve musíte zadat příchod.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string odchodStr = Microsoft.VisualBasic.Interaction.InputBox("V kolik jste odešli z práce? (HH:mm)", "Odchod", DateTime.Now.ToString("HH:mm"));
            if (DateTime.TryParse(odchodStr, out DateTime odchod))
            {
                Dochazka? posledni = dochazky.LastOrDefault(d => d.Odchod == null);
                if (posledni == null)
                {
                    MessageBox.Show("Neexistuje žádný záznam příchodu bez odchodu.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                odchod = new DateTime(posledni.Prichod.Year, posledni.Prichod.Month, posledni.Prichod.Day, odchod.Hour, odchod.Minute, odchod.Second);
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
                MessageBox.Show("Zadaný čas není platný.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportovat_Click(object sender, RoutedEventArgs e)
        {
            if (dochazky.Count == 0)
            {
                MessageBox.Show("Nejsou žádné záznamy k exportu.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string filePath = "dochazka.xlsx";
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Docházka");
                worksheet.Cells[1, 1].Value = "Datum";
                worksheet.Cells[1, 2].Value = "Příchod";
                worksheet.Cells[1, 3].Value = "Odchod";
                worksheet.Cells[1, 4].Value = "Rozdíl";
                worksheet.Cells[1, 5].Value = "Poznámka";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (Dochazka dochazka in dochazky)
                {
                    worksheet.Cells[row, 1].Value = dochazka.Prichod.ToShortDateString();
                    worksheet.Cells[row, 2].Value = dochazka.Prichod.ToString("HH:mm");
                    worksheet.Cells[row, 3].Value = dochazka.Odchod?.ToString("HH:mm");
                    worksheet.Cells[row, 4].Value = dochazka.Rozdil.ToString();
                    worksheet.Cells[row, 5].Value = dochazka.Rozdil.TotalHours >= 9 ? "Splněno" : "Nesplněno";
                    row++;
                }

                worksheet.Cells[$"A1:E{row - 1}"].AutoFitColumns();
                worksheet.Cells[$"A1:E{row - 1}"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[$"A1:E{row - 1}"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[$"A1:E{row - 1}"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[$"A1:E{row - 1}"].Style.Border.Right.Style = ExcelBorderStyle.Thin;

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
