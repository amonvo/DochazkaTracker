using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Border = System.Windows.Controls.Border;
using DataGrid = System.Windows.Controls.DataGrid;

namespace DochazkaTracker
{
    public partial class MainWindow : Window
    {
        public class DochazkaRow
        {
            public string Datum { get; set; }
            public string Prichod { get; set; }
            public string Odchod { get; set; }
            public string Rozdil { get; set; }
            public string Poznamka { get; set; }
        }
        private List<Dochazka> dochazky = new List<Dochazka>();
        private const string DataFilePath = "dochazka.json";

        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Načtení záznamů při spuštění
            LoadDochazkaData();
            //tesrts
            // Nastavení vzhledu okna
            this.Title = "Docházka Tracker";
            this.Width = 800;
            this.Height = 800;
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
            //t
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
                BorderThickness = new Thickness(2)
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
                BorderThickness = new Thickness(2)
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
                BorderThickness = new Thickness(2)
            };
            btnExportovat.Click += BtnExportovat_Click;
            mainPanel.Children.Add(btnExportovat);

            Button btnZobrazitDochazku = new Button
            {
                Content = "Zobrazit Docházku",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightYellow, Colors.Orange, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.DarkOrange,
                BorderThickness = new Thickness(2)
            };
            btnZobrazitDochazku.Click += BtnZobrazitDochazku_Click;
            mainPanel.Children.Add(btnZobrazitDochazku);

            Button btnVymazatZaznamy = new Button
            {
                Content = "Vymazat Záznamy",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightPink, Colors.OrangeRed, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.DarkRed,
                BorderThickness = new Thickness(2)
            };
            btnVymazatZaznamy.Click += BtnVymazatZaznamy_Click;
            mainPanel.Children.Add(btnVymazatZaznamy);

            Button btnStatistiky = new Button
            {
                Content = "Zobrazit Statistiky",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightCyan, Colors.CadetBlue, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.DarkCyan,
                BorderThickness = new Thickness(2)
            };
            btnStatistiky.Click += BtnStatistiky_Click;
            mainPanel.Children.Add(btnStatistiky);

            Button btnZobrazitExcel = new Button
            {
                Content = "Zobrazit Excel",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightGoldenrodYellow, Colors.Goldenrod, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.DarkGoldenrod,
                BorderThickness = new Thickness(2)
            };
            btnZobrazitExcel.Click += BtnZobrazitExcel_Click;
            mainPanel.Children.Add(btnZobrazitExcel);

            Button btnZobrazitGraf = new Button
            {
                Content = "Zobrazit Graf Docházky",
                Width = 250,
                Height = 50,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new LinearGradientBrush(Colors.LightSkyBlue, Colors.MediumSlateBlue, 90),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.MediumSlateBlue,
                BorderThickness = new Thickness(2)
            };
            btnZobrazitGraf.Click += BtnZobrazitGraf_Click;
            mainPanel.Children.Add(btnZobrazitGraf);

            // Přidání kalendáře pro výběr data
            DatePicker datePicker = new DatePicker
            {
                Width = 250,
                Margin = new Thickness(0, 10, 0, 10)
            };
            mainPanel.Children.Add(datePicker);

            mainBorder.Child = mainPanel;
            this.Content = mainBorder;

            // Uložení záznamů při zavření okna
            this.Closing += MainWindow_Closing;
        }

        private void LoadDochazkaData()
        {
            if (File.Exists(DataFilePath))
            {
                string jsonData = File.ReadAllText(DataFilePath);
                dochazky = JsonSerializer.Deserialize<List<Dochazka>>(jsonData) ?? new List<Dochazka>();

                // Znovu spočítat rozdíly pro načtené záznamy
                foreach (var dochazka in dochazky)
                {
                    if (dochazka.Odchod.HasValue)
                    {
                        dochazka.VypocetRozdilu();
                    }
                }
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveDochazkaData();
        }

        private void SaveDochazkaData()
        {
            string jsonData = JsonSerializer.Serialize(dochazky);
            File.WriteAllText(DataFilePath, jsonData);
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
                SaveDochazkaData(); // Uložení po přidání nového záznamu
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
                SaveDochazkaData(); // Uložení po aktualizaci záznamu

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

        private void BtnZobrazitDochazku_Click(object sender, RoutedEventArgs e)
        {
            if (dochazky.Count == 0)
            {
                MessageBox.Show("Nejsou žádné záznamy k zobrazení.", "Docházka", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string zprava = "Záznamy docházky:\n";
            foreach (var dochazka in dochazky)
            {
                zprava += $"Datum: {dochazka.Prichod.ToShortDateString()}, Příchod: {dochazka.Prichod:HH:mm}, Odchod: {dochazka.Odchod?.ToString("HH:mm") ?? "N/A"}, Rozdíl: {dochazka.Rozdil}\n";
            }

            MessageBox.Show(zprava, "Docházka", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnVymazatZaznamy_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Opravdu chcete vymazat všechny záznamy?", "Vymazat záznamy", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                dochazky.Clear();
                SaveDochazkaData(); // Uložení po vymazání záznamů
                MessageBox.Show("Všechny záznamy byly vymazány.", "Vymazáno", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnStatistiky_Click(object sender, RoutedEventArgs e)
        {
            if (dochazky.Count == 0)
            {
                MessageBox.Show("Nejsou žádné záznamy pro zobrazení statistik.", "Statistiky", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double prumernaPracovniDoba = dochazky.Average(d => d.Rozdil.TotalHours);
            int pocetDniSplneno = dochazky.Count(d => d.Rozdil.TotalHours >= 9);
            int pocetDniNesplneno = dochazky.Count(d => d.Rozdil.TotalHours < 9);

            string zprava = $"Statistiky docházky:\n" +
                           $"Průměrná pracovní doba: {prumernaPracovniDoba:F2} hodin\n" +
                           $"Počet dní splněno (9+ hodin): {pocetDniSplneno}\n" +
                           $"Počet dní nesplněno (< 9 hodin): {pocetDniNesplneno}";

            MessageBox.Show(zprava, "Statistiky", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnZobrazitExcel_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "dochazka.xlsx";
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Excel soubor neexistuje. Nejprve proveďte export.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Window excelWindow = new Window
            {
                Title = "Zobrazení Excelu",
                Width = 800,
                Height = 600
            };

            DataGrid dataGrid = new DataGrid
            {
                AutoGenerateColumns = true,
                Margin = new Thickness(10),
                IsReadOnly = true
            };

            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                List<DochazkaRow> data = new List<DochazkaRow>();
                for (int row = 2; row <= rowCount; row++)
                {
                    var rowData = new DochazkaRow();
                    for (int col = 1; col <= colCount; col++)
                    {
                        string columnName = worksheet.Cells[1, col].Value.ToString();
                        switch (columnName)
                        {
                            case "Datum":
                                rowData.Datum = worksheet.Cells[row, col].Value?.ToString();
                                break;
                            case "Příchod":
                                rowData.Prichod = worksheet.Cells[row, col].Value?.ToString();
                                break;
                            case "Odchod":
                                rowData.Odchod = worksheet.Cells[row, col].Value?.ToString();
                                break;
                            case "Rozdíl":
                                rowData.Rozdil = worksheet.Cells[row, col].Value?.ToString();
                                break;
                            case "Poznámka":
                                rowData.Poznamka = worksheet.Cells[row, col].Value?.ToString();
                                break;
                        }
                    }
                    data.Add(rowData);
                }

                dataGrid.ItemsSource = data;
            }

            excelWindow.Content = dataGrid;
            excelWindow.Show();
        }

        private void BtnZobrazitGraf_Click(object sender, RoutedEventArgs e)
        {
            var validDochazky = dochazky.Where(d => d.Odchod.HasValue).ToList();
            if (validDochazky.Count == 0)
            {
                MessageBox.Show("Nejsou žádné záznamy k zobrazení grafu.", "Graf", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Window grafWindow = new Window
            {
                Title = "Graf Docházky",
                Width = 800,
                Height = 600
            };

            CartesianChart chart = new CartesianChart
            {
                Margin = new Thickness(10)
            };

            var values = new ChartValues<double>();
            var labels = new List<string>();

            foreach (var dochazka in validDochazky)
            {
                values.Add(dochazka.Rozdil.TotalHours);
                labels.Add(dochazka.Prichod.ToShortDateString());
            }

            LineSeries lineSeries = new LineSeries
            {
                Title = "Pracovní doba (hodiny)",
                Values = values,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10
            };

            chart.Series = new SeriesCollection { lineSeries };
            chart.AxisX.Add(new Axis
            {
                Title = "Datum",
                Labels = labels
            });

            chart.AxisY.Add(new Axis
            {
                Title = "Pracovní doba (hodiny)",
                LabelFormatter = value => value.ToString("N2")
            });

            grafWindow.Content = chart;
            grafWindow.Show();
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
