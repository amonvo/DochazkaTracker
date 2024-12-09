// MainWindow.xaml.cs
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
using System.Windows.Media;
using System.Windows.Media.Animation;


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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Najdeme animaci podle jejího klíče a spustíme ji
            Storyboard fadeInStoryboard = (Storyboard)FindResource("FadeInAnimation");
            fadeInStoryboard.Begin(this);
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


        private async void BtnExportovat_Click(object sender, RoutedEventArgs e)
        {
            if (dochazky.Count == 0)
            {
                MessageBox.Show("Nejsou žádné záznamy k exportu.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var startTime = DateTime.Now;

            LoadingBar.Visibility = Visibility.Visible;

            string filePath = "dochazka.xlsx";
            await Task.Run(() =>
            {
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
                    foreach (var dochazka in dochazky)
                    {
                        worksheet.Cells[row, 1].Value = dochazka.Prichod.ToShortDateString();
                        worksheet.Cells[row, 2].Value = dochazka.Prichod.ToString("HH:mm");
                        worksheet.Cells[row, 3].Value = dochazka.Odchod?.ToString("HH:mm");
                        worksheet.Cells[row, 4].Value = dochazka.Rozdil.ToString();
                        worksheet.Cells[row, 5].Value = dochazka.Rezim;
                        row++;
                    }

                    File.WriteAllBytes(filePath, package.GetAsByteArray());
                }
            });

            // Zajistit minimální dobu zobrazení ProgressBaru
            var elapsedTime = DateTime.Now - startTime;
            if (elapsedTime.TotalMilliseconds < 1000)
            {
                await Task.Delay(1000 - (int)elapsedTime.TotalMilliseconds);
            }

            LoadingBar.Visibility = Visibility.Collapsed;

            MessageBox.Show($"Docházka byla exportována do souboru {filePath}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void BtnZobrazitDochazku_Click(object sender, RoutedEventArgs e)
        {
            if (dochazky.Count == 0)
            {
                MessageBox.Show("Nejsou žádné záznamy k zobrazení.", "Docházka", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            const double prumernyPracovniDen = 8.5;

            // Seskupení záznamů podle měsíců a roků
            var mesice = dochazky.GroupBy(d => new { d.Prichod.Year, d.Prichod.Month })
                                 .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

            // Vytvoření okna pro zobrazení
            Window dochazkaWindow = new Window
            {
                Title = "Docházka",
                Width = 600,
                Height = 800,
                Content = new ScrollViewer()
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new StackPanel()
                }
            };

            StackPanel mainPanel = (StackPanel)((ScrollViewer)dochazkaWindow.Content).Content;

            foreach (var group in mesice)
            {
                // Zobrazení nadpisu pro měsíc
                TextBlock monthHeader = new TextBlock
                {
                    Text = $"{group.Key.Month}/{group.Key.Year}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                mainPanel.Children.Add(monthHeader);

                // Iterace přes jednotlivé záznamy v měsíci
                foreach (var dochazka in group)
                {
                    string status;
                    SolidColorBrush color;

                    if (dochazka.Rozdil.TotalHours >= prumernyPracovniDen)
                    {
                        status = $"Přesčas: +{dochazka.Rozdil.TotalHours - prumernyPracovniDen:F2} hodin, doporučený odchod: {dochazka.Prichod.AddHours(prumernyPracovniDen):HH:mm}";
                        color = Brushes.Green;
                    }
                    else
                    {
                        status = $"Minus: -{prumernyPracovniDen - dochazka.Rozdil.TotalHours:F2} hodin, potřebný odchod: {dochazka.Prichod.AddHours(prumernyPracovniDen):HH:mm}";
                        color = Brushes.Red;
                    }

                    TextBlock recordText = new TextBlock
                    {
                        Text = $"Datum: {dochazka.Prichod:dd.MM.yyyy} ({dochazka.Prichod:dddd}), Příchod: {dochazka.Prichod:HH:mm}, Odchod: {dochazka.Odchod?.ToString("HH:mm") ?? "N/A"}\n{status}",
                        Margin = new Thickness(0, 0, 0, 5),
                        Foreground = color
                    };
                    mainPanel.Children.Add(recordText);
                }

                // Výpočet měsíčního deficitu, odpracovaných hodin a očekávaných hodin
                int pracovnichDniVMesici = group.Count();
                double celkoveOdpracovaneHodiny = group.Sum(d => d.Rozdil.TotalHours);
                double ocekavaneHodiny = pracovnichDniVMesici * prumernyPracovniDen;
                double rozdilHodin = celkoveOdpracovaneHodiny - ocekavaneHodiny;

                // Shrnutí měsíčních statistik
                TextBlock monthSummary = new TextBlock
                {
                    Margin = new Thickness(0, 10, 0, 5),
                    FontWeight = FontWeights.Bold
                };

                if (rozdilHodin > 0)
                {
                    monthSummary.Text = $"Přesčas za {group.Key.Month}/{group.Key.Year}: +{rozdilHodin:F2} hodin\n" +
                                        $"Očekávaný počet hodin: {ocekavaneHodiny:F2} hodin\n" +
                                        $"Odpracovaný počet hodin: {celkoveOdpracovaneHodiny:F2} hodin";
                    monthSummary.Foreground = Brushes.Green;
                }
                else if (rozdilHodin < 0)
                {
                    monthSummary.Text = $"Deficit za {group.Key.Month}/{group.Key.Year}: {rozdilHodin:F2} hodin\n" +
                                        $"Očekávaný počet hodin: {ocekavaneHodiny:F2} hodin\n" +
                                        $"Odpracovaný počet hodin: {celkoveOdpracovaneHodiny:F2} hodin";
                    monthSummary.Foreground = Brushes.Red;
                }
                else
                {
                    monthSummary.Text = $"Za {group.Key.Month}/{group.Key.Year} nemáte žádný přesčas ani deficit.\n" +
                                        $"Očekávaný počet hodin: {ocekavaneHodiny:F2} hodin\n" +
                                        $"Odpracovaný počet hodin: {celkoveOdpracovaneHodiny:F2} hodin";
                    monthSummary.Foreground = Brushes.Black;
                }

                mainPanel.Children.Add(monthSummary);
            }

            dochazkaWindow.ShowDialog();
        }



        private void BtnImportovat_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Vyberte Excel soubor pro import"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            MessageBox.Show("Excel soubor neobsahuje žádné listy.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        int rowCount = worksheet.Dimension.Rows;

                        if (rowCount < 2) // Předpokládáme, že první řádek jsou záhlaví
                        {
                            MessageBox.Show("Excel soubor neobsahuje žádné záznamy.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        List<Dochazka> importedDochazky = new List<Dochazka>();

                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                // Získání textu z prvního sloupce
                                string datumText = worksheet.Cells[row, 1].Text.Trim();

                                // Kontrola prázdných řádků
                                if (string.IsNullOrWhiteSpace(datumText))
                                {
                                    continue; // Přeskočí prázdné řádky
                                }

                                // Přeskočení řádků s měsícem a rokem (např. "10/2024")
                                if (datumText.Length <= 7 && datumText.Contains("/"))
                                {
                                    continue; // Ignorujeme oddělovače měsíců
                                }

                                // Kontrola, zda text je validní datum
                                if (!DateTime.TryParse(datumText, out DateTime datum))
                                {
                                    MessageBox.Show($"Chyba při zpracování řádku {row}: '{datumText}' není validní datum.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    continue;
                                }

                                // Získání příchodu, odchodu a režimu
                                string prichodText = worksheet.Cells[row, 2].Text;
                                string odchodText = worksheet.Cells[row, 3].Text;
                                string rezim = worksheet.Cells[row, 5].Text; // Poznámka použita jako režim

                                // Sestavení datumu a času příchodu a odchodu
                                DateTime prichod = DateTime.ParseExact($"{datum:dd.MM.yyyy} {prichodText}", "dd.MM.yyyy HH:mm", null);
                                DateTime? odchod = string.IsNullOrWhiteSpace(odchodText)
                                    ? (DateTime?)null
                                    : DateTime.ParseExact($"{datum:dd.MM.yyyy} {odchodText}", "dd.MM.yyyy HH:mm", null);

                                Dochazka novaDochazka = new Dochazka
                                {
                                    Prichod = prichod,
                                    Odchod = odchod,
                                    Rezim = rezim
                                };
                                novaDochazka.VypocetRozdilu();

                                importedDochazky.Add(novaDochazka);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Chyba při zpracování řádku {row}: {ex.Message}", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        if (importedDochazky.Count > 0)
                        {
                            dochazky.AddRange(importedDochazky);
                            SaveDochazkaData();
                            MessageBox.Show($"Úspěšně naimportováno {importedDochazky.Count} záznamů.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Excel soubor neobsahuje platné záznamy.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při čtení Excel souboru: {ex.Message}", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        // Metoda pro odstranění dne v týdnu z textu
        private string RemoveDayOfWeek(string input)
        {
            string[] daysOfWeek = { "pondělí", "úterý", "středa", "čtvrtek", "pátek", "sobota", "neděle" };
            foreach (var day in daysOfWeek)
            {
                input = input.Replace(day, "").Trim();
            }
            return input;
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

            const double prumernyPracovniDen = 8.5;

            // Vytvoření ComboBoxu pro výběr měsíce a roku
            ComboBox monthFilter = new ComboBox
            {
                Width = 200,
                Margin = new Thickness(10),
                ItemsSource = dochazky.Select(d => $"{d.Prichod.Month}/{d.Prichod.Year}")
                                      .Distinct()
                                      .OrderBy(m => m)
                                      .ToList()
            };

            Button calculateButton = new Button
            {
                Content = "Vypočítat",
                Width = 100,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            StackPanel panel = new StackPanel();
            panel.Children.Add(monthFilter);
            panel.Children.Add(calculateButton);

            Window statWindow = new Window
            {
                Title = "Výběr měsíce pro statistiky",
                Width = 400,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            DataGrid statGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                Margin = new Thickness(10),
                HeadersVisibility = DataGridHeadersVisibility.Column
            };

            statGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Den",
                Binding = new System.Windows.Data.Binding("Den")
            });

            statGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Očekávané hodiny",
                Binding = new System.Windows.Data.Binding("OcekavaneHodiny")
            });

            statGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Odpracované hodiny",
                Binding = new System.Windows.Data.Binding("OdpracovaneHodiny")
            });

            statGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Rozdíl",
                Binding = new System.Windows.Data.Binding("Rozdil")
            });

            calculateButton.Click += (s, args) =>
            {
                if (monthFilter.SelectedItem == null)
                {
                    MessageBox.Show("Vyberte měsíc.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string selectedMonth = monthFilter.SelectedItem.ToString();
                int month = int.Parse(selectedMonth.Split('/')[0]);
                int year = int.Parse(selectedMonth.Split('/')[1]);

                var filteredDochazky = dochazky.Where(d => d.Prichod.Month == month && d.Prichod.Year == year).ToList();

                if (!filteredDochazky.Any())
                {
                    MessageBox.Show("Pro vybraný měsíc nejsou žádné záznamy.", "Statistiky", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int zaznamenaneDny = filteredDochazky.Count();
                double odpracovaneHodiny = filteredDochazky.Sum(d => d.Rozdil.TotalHours);
                double ocekavaneHodiny = zaznamenaneDny * prumernyPracovniDen;
                double rozdilHodin = odpracovaneHodiny - ocekavaneHodiny;

                var stats = new
                {
                    Den = $"{month}/{year}",
                    OcekavaneHodiny = ocekavaneHodiny.ToString("N1"),
                    OdpracovaneHodiny = odpracovaneHodiny.ToString("N1"),
                    Rozdil = rozdilHodin > 0 ? $"+{rozdilHodin:F1}" : $"-{Math.Abs(rozdilHodin):F1}"
                };

                statGrid.ItemsSource = new List<object> { stats };
            };

            panel.Children.Add(statGrid);

            statWindow.Content = panel;
            statWindow.ShowDialog();
        }



        private void LoadData(string filePath, DataGrid dataGrid)
        {
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
        }

        private void FilterData(string selectedMonth, DataGrid dataGrid)
        {
            if (string.IsNullOrEmpty(selectedMonth))
                return;

            int month = int.Parse(selectedMonth.Split('/')[0]);
            int year = int.Parse(selectedMonth.Split('/')[1]);

            dataGrid.ItemsSource = dochazky.Where(d => d.Prichod.Month == month && d.Prichod.Year == year)
                                           .Select(d => new DochazkaRow
                                           {
                                               Datum = d.Prichod.ToShortDateString(),
                                               Prichod = d.Prichod.ToString("HH:mm"),
                                               Odchod = d.Odchod?.ToString("HH:mm"),
                                               Rozdil = d.Rozdil.ToString(),
                                               Poznamka = d.Rozdil.TotalHours >= 9 ? "Splněno" : "Nesplněno"
                                           })
                                           .ToList();
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

        private async void SimulateLongOperation()
        {
            LoadingBar.Visibility = Visibility.Visible;
            for (int i = 0; i <= 100; i++)
            {
                LoadingBar.Value = i;
                await Task.Delay(20); // Simulace zpoždění
            }
            LoadingBar.Visibility = Visibility.Collapsed;
        }

        private void BtnNapoveda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Docházka Tracker:\n\n1. Klikněte na 'Doplnit Docházku' pro přidání nového záznamu.\n" +
                            "2. Použijte 'Exportovat do Excelu' pro uložení dat.\n" +
                            "3. Pomocí 'Zobrazit Statistiky' analyzujte data za jednotlivé měsíce.\n" +
                            "4. Všechny změny jsou automaticky ukládány.", "Nápověda", MessageBoxButton.OK, MessageBoxImage.Information);
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