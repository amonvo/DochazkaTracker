﻿// MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using OfficeOpenXml;
using OfficeOpenXml.Style;

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

        private void BtnDoplnitDochazku_Click(object sender, RoutedEventArgs e)
        {
            DatePicker datePicker = new DatePicker();
            ComboBox rezimComboBox = new ComboBox
            {
                ItemsSource = new List<string> { "Práce z domova", "Kancelář", "Dovolená", "Služební cesta" },
                SelectedIndex = 0,
                Margin = new Thickness(0, 10, 0, 10)
            };
            Button potvrditButton = new Button
            {
                Content = "Potvrdit",
                Width = 100,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            StackPanel panel = new StackPanel
            {
                Children = { datePicker, rezimComboBox, potvrditButton },
                Margin = new Thickness(10)
            };

            Window dateWindow = new Window
            {
                Title = "Vyberte datum a režim pro doplnění docházky",
                Width = 300,
                Height = 250,
                Content = panel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            potvrditButton.Click += (s, args) =>
            {
                if (datePicker.SelectedDate.HasValue)
                {
                    dateWindow.DialogResult = true;
                    dateWindow.Close();
                }
                else
                {
                    MessageBox.Show("Vyberte datum.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            if (dateWindow.ShowDialog() == true)
            {
                DateTime datum = datePicker.SelectedDate ?? DateTime.Now;
                string vybranyRezim = rezimComboBox.SelectedItem.ToString();
                OpenTimeInputWindow("Příchod", (prichodCas) =>
                {
                    OpenTimeInputWindow("Odchod", (odchodCas) =>
                    {
                        prichodCas = new DateTime(datum.Year, datum.Month, datum.Day, prichodCas.Hour, prichodCas.Minute, prichodCas.Second);
                        odchodCas = new DateTime(datum.Year, datum.Month, datum.Day, odchodCas.Hour, odchodCas.Minute, odchodCas.Second);

                        if (odchodCas <= prichodCas)
                        {
                            MessageBox.Show("Odchod musí být později než příchod.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (dochazky.Any(d => d.Prichod.Date == datum.Date))
                        {
                            MessageBox.Show("Docházka pro tento den již existuje.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        Dochazka novaDochazka = new Dochazka { Prichod = prichodCas, Odchod = odchodCas, Rezim = vybranyRezim };
                        novaDochazka.VypocetRozdilu();
                        dochazky.Add(novaDochazka);
                        SaveDochazkaData();

                        MessageBox.Show("Docházka byla úspěšně doplněna.", "Doplnění docházky", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
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
                SaveDochazkaData();
                File.Delete("dochazka.xlsx"); // Delete the existing Excel file to prevent showing outdated data
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
            double celkovyPrescas = dochazky.Sum(d => Math.Max(0, d.Rozdil.TotalHours - 9));

            string zprava = $"Statistiky docházky:\n" +
                           $"Průměrná pracovní doba: {prumernaPracovniDoba:F2} hodin\n" +
                           $"Počet dní splněno (9+ hodin): {pocetDniSplneno}\n" +
                           $"Počet dní nesplněno (< 9 hodin): {pocetDniNesplneno}\n" +
                           $"Celkový přesčas: {celkovyPrescas:F2} hodin";

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

        private void BtnEditovatZaznam_Click(object sender, RoutedEventArgs e)
        {
            if (dochazky.Count == 0)
            {
                MessageBox.Show("Nejsou žádné záznamy k editaci.", "Editace záznamu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ListBox zaznamyList = new ListBox
            {
                Margin = new Thickness(10),
                Height = 200
            };

            foreach (var dochazka in dochazky)
            {
                zaznamyList.Items.Add($"{dochazka.Prichod.ToShortDateString()} - Příchod: {dochazka.Prichod:HH:mm}, Odchod: {dochazka.Odchod?.ToString("HH:mm") ?? "N/A"}");
            }

            Button potvrditButton = new Button
            {
                Content = "Editovat",
                Width = 100,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            StackPanel panel = new StackPanel
            {
                Children = { zaznamyList, potvrditButton },
                Margin = new Thickness(10)
            };

            Window editWindow = new Window
            {
                Title = "Vyberte záznam k editaci",
                Width = 400,
                Height = 300,
                Content = panel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            potvrditButton.Click += (s, args) =>
            {
                if (zaznamyList.SelectedIndex == -1)
                {
                    MessageBox.Show("Vyberte záznam k editaci.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                editWindow.DialogResult = true;
                editWindow.Close();
            };

            if (editWindow.ShowDialog() == true)
            {
                int index = zaznamyList.SelectedIndex;
                EditZaznam(index);
            }
        }

        private void EditZaznam(int index)
        {
            Dochazka vybranaDochazka = dochazky[index];

            OpenTimeInputWindow("Editovat Příchod", (novyPrichod) =>
            {
                OpenTimeInputWindow("Editovat Odchod", (novyOdchod) =>
                {
                    novyPrichod = new DateTime(vybranaDochazka.Prichod.Year, vybranaDochazka.Prichod.Month, vybranaDochazka.Prichod.Day, novyPrichod.Hour, novyPrichod.Minute, novyPrichod.Second);
                    novyOdchod = new DateTime(vybranaDochazka.Prichod.Year, vybranaDochazka.Prichod.Month, vybranaDochazka.Prichod.Day, novyOdchod.Hour, novyOdchod.Minute, novyOdchod.Second);

                    if (novyOdchod <= novyPrichod)
                    {
                        MessageBox.Show("Odchod musí být později než příchod.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    vybranaDochazka.Prichod = novyPrichod;
                    vybranaDochazka.Odchod = novyOdchod;
                    vybranaDochazka.VypocetRozdilu();
                    SaveDochazkaData();

                    MessageBox.Show("Záznam byl úspěšně upraven.", "Editace záznamu", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            });
        }




        private void OpenTimeInputWindow(string title, Action<DateTime> onTimeSelected)
        {
            Window timeWindow = new Window
            {
                Title = title,
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            TextBlock label = new TextBlock
            {
                Text = $"V kolik jste {title.ToLower()}? (HH:mm)",
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(label);

            TextBox timeTextBox = new TextBox
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(timeTextBox);

            Slider timeSlider = new Slider
            {
                Minimum = 0,
                Maximum = 1440,
                Value = DateTime.Now.Hour * 60 + DateTime.Now.Minute,
                TickFrequency = 30,
                Width = 250,
                Margin = new Thickness(0, 0, 0, 10)
            };
            timeSlider.ValueChanged += (s, args) =>
            {
                TimeSpan selectedTime = TimeSpan.FromMinutes(timeSlider.Value);
                timeTextBox.Text = $"{selectedTime:hh\\:mm}";
            };
            panel.Children.Add(timeSlider);

            Button okButton = new Button
            {
                Content = "OK",
                Width = 100,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            okButton.Click += (s, args) =>
            {
                if (DateTime.TryParse(timeTextBox.Text, out DateTime selectedTime))
                {
                    onTimeSelected(selectedTime);
                    timeWindow.Close();
                }
                else
                {
                    MessageBox.Show("Zadaný čas není platný.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            panel.Children.Add(okButton);

            timeWindow.Content = panel;
            timeWindow.ShowDialog();
        }



        public class Dochazka
        {
            public DateTime Prichod { get; set; }
            public DateTime? Odchod { get; set; }
            public TimeSpan Rozdil { get; private set; }
            public string Rezim { get; set; } // Přidána vlastnost Rezim


            public void VypocetRozdilu()
            {
                if (Odchod.HasValue)
                {
                    Rozdil = Odchod.Value - Prichod;
                }
            }
        }
    }
}