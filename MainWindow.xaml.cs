using OfficeOpenXml;
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

        private readonly DochazkaService _dochazkaService;
        private readonly StatistikyService _statistikyService;
        private readonly AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();

            _config = AppConfig.Load();
            _dochazkaService = new DochazkaService();
            _statistikyService = new StatistikyService();

            InitializeApp();
        }

        private async void InitializeApp()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                await _dochazkaService.LoadDataAsync();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Inicializace aplikace");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FindResource("FadeInAnimation") is Storyboard fadeInStoryboard)
                {
                    fadeInStoryboard.Begin(this);
                }
            }
            catch
            {
                // Ignorujeme chyby s animací
            }
        }

        private void BtnDoplnitDochazku_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DatePicker datePicker = new DatePicker();
                ComboBox rezimComboBox = new ComboBox
                {
                    ItemsSource = _config.Rezimy,
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

                    if (_dochazkaService.ExistujeDochazkaProDatum(datum))
                    {
                        MessageBox.Show("Docházka pro tento den již existuje.", "Chyba",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    OpenTimeInputWindow("Příchod", (prichodCas) =>
                    {
                        OpenTimeInputWindow("Odchod", (odchodCas) =>
                        {
                            prichodCas = new DateTime(datum.Year, datum.Month, datum.Day,
                                prichodCas.Hour, prichodCas.Minute, prichodCas.Second);
                            odchodCas = new DateTime(datum.Year, datum.Month, datum.Day,
                                odchodCas.Hour, odchodCas.Minute, odchodCas.Second);

                            if (odchodCas <= prichodCas)
                            {
                                MessageBox.Show("Odchod musí být později než příchod.", "Chyba",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            Dochazka novaDochazka = new Dochazka
                            {
                                Prichod = prichodCas,
                                Odchod = odchodCas,
                                Rezim = vybranyRezim
                            };
                            novaDochazka.VypocetRozdilu();

                            _dochazkaService.Add(novaDochazka);

                            MessageBox.Show("Docházka byla úspěšně doplněna.", "Doplnění docházky",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Přidání docházky");
            }
        }

        private async void BtnExportovat_Click(object sender, RoutedEventArgs e)
        {
            var dochazky = _dochazkaService.GetAll();

            if (!dochazky.Any())
            {
                MessageBox.Show("Nejsou žádné záznamy k exportu.", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                BtnExportovat.IsEnabled = false;
                LoadingBar.Visibility = Visibility.Visible;

                string filePath = await ExportService.ExportToExcelAsync(dochazky);

                MessageBox.Show($"Docházka byla exportována do souboru {filePath}",
                    "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Export do Excelu");
            }
            finally
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                BtnExportovat.IsEnabled = true;
            }
        }

        private void BtnZobrazitDochazku_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dochazky = _dochazkaService.GetAll();

                if (!dochazky.Any())
                {
                    MessageBox.Show("Nejsou žádné záznamy k zobrazení.", "Docházka",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                const double prumernyPracovniDen = 8.5;

                var mesice = dochazky.GroupBy(d => new { d.Prichod.Year, d.Prichod.Month })
                                     .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

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
                    TextBlock monthHeader = new TextBlock
                    {
                        Text = $"{group.Key.Month}/{group.Key.Year}",
                        FontWeight = FontWeights.Bold,
                        FontSize = 16,
                        Margin = new Thickness(0, 10, 0, 5)
                    };
                    mainPanel.Children.Add(monthHeader);

                    foreach (var dochazka in group)
                    {
                        string status = dochazka.GetStatusText(prumernyPracovniDen);
                        SolidColorBrush color = dochazka.MaPrescas(prumernyPracovniDen) ? Brushes.Green : Brushes.Red;

                        TextBlock recordText = new TextBlock
                        {
                            Text = $"Datum: {dochazka.Prichod:dd.MM.yyyy} ({dochazka.Prichod:dddd}), " +
                                   $"Příchod: {dochazka.Prichod:HH:mm}, " +
                                   $"Odchod: {dochazka.Odchod?.ToString("HH:mm") ?? "N/A"}\n{status}",
                            Margin = new Thickness(0, 0, 0, 5),
                            Foreground = color
                        };
                        mainPanel.Children.Add(recordText);
                    }

                    var statistiky = _statistikyService.VypocitejMesicniStatistiky(dochazky.ToList(), group.Key.Month, group.Key.Year);

                    TextBlock monthSummary = new TextBlock
                    {
                        Text = $"Za {group.Key.Month}/{group.Key.Year}:\n" +
                               $"Očekávaný počet hodin: {statistiky.OcekavaneHodiny:F2} hodin\n" +
                               $"Odpracovaný počet hodin: {statistiky.OdpracovaneHodiny:F2} hodin\n" +
                               $"Rozdíl: {statistiky.FormatovanyRozdil} hodin",
                        Margin = new Thickness(0, 10, 0, 5),
                        FontWeight = FontWeights.Bold,
                        Foreground = statistiky.RozdiHodin >= 0 ? Brushes.Green : Brushes.Red
                    };

                    mainPanel.Children.Add(monthSummary);
                }

                dochazkaWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Zobrazení docházky");
            }
        }

        private void BtnImportovat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    Title = "Vyberte Excel soubor pro import"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    var importedDochazky = ImportService.ImportFromExcel(filePath);

                    if (importedDochazky.Any())
                    {
                        foreach (var dochazka in importedDochazky)
                        {
                            _dochazkaService.Add(dochazka);
                        }

                        MessageBox.Show($"Úspěšně naimportováno {importedDochazky.Count} záznamů.",
                            "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Excel soubor neobsahuje platné záznamy.", "Import",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Import ze souboru");
            }
        }

        private void BtnVymazatZaznamy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Opravdu chcete vymazat všechny záznamy?", "Vymazat záznamy",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _dochazkaService.Clear();

                    if (File.Exists("dochazka.xlsx"))
                    {
                        File.Delete("dochazka.xlsx");
                    }

                    MessageBox.Show("Všechny záznamy byly vymazány.", "Vymazáno",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Vymazání záznamů");
            }
        }

        private void BtnStatistiky_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dochazky = _dochazkaService.GetAll();

                if (!dochazky.Any())
                {
                    MessageBox.Show("Nejsou žádné záznamy pro zobrazení statistik.", "Statistiky",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dostupneMesice = _statistikyService.GetDostupneMesice(dochazky);

                ComboBox monthFilter = new ComboBox
                {
                    Width = 200,
                    Margin = new Thickness(10),
                    ItemsSource = dostupneMesice
                };

                Button calculateButton = new Button
                {
                    Content = "Vypočítat",
                    Width = 100,
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                TextBlock resultText = new TextBlock
                {
                    Margin = new Thickness(10),
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                };

                StackPanel panel = new StackPanel();
                panel.Children.Add(monthFilter);
                panel.Children.Add(calculateButton);
                panel.Children.Add(resultText);

                Window statWindow = new Window
                {
                    Title = "Statistiky docházky",
                    Width = 400,
                    Height = 300,
                    Content = panel,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

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

                    var statistiky = _statistikyService.VypocitejMesicniStatistiky(dochazky, month, year);

                    resultText.Text = $"Statistiky za {month}/{year}:\n\n" +
                                     $"Počet pracovních dnů: {statistiky.PocetDni}\n" +
                                     $"Očekávané hodiny: {statistiky.OcekavaneHodiny:F1}\n" +
                                     $"Odpracované hodiny: {statistiky.OdpracovaneHodiny:F1}\n" +
                                     $"Rozdíl: {statistiky.FormatovanyRozdil} hodin";

                    resultText.Foreground = statistiky.RozdiHodin >= 0 ? Brushes.Green : Brushes.Red;
                };

                statWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Zobrazení statistik");
            }
        }

        private void BtnEditovatZaznam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dochazky = _dochazkaService.GetAll();

                if (!dochazky.Any())
                {
                    MessageBox.Show("Nejsou žádné záznamy k editaci.", "Editace záznamu",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ListBox zaznamyList = new ListBox
                {
                    Margin = new Thickness(10),
                    Height = 200
                };

                for (int i = 0; i < dochazky.Count; i++)
                {
                    var dochazka = dochazky[i];
                    zaznamyList.Items.Add($"{dochazka.Prichod.ToShortDateString()} - " +
                                         $"Příchod: {dochazka.Prichod:HH:mm}, " +
                                         $"Odchod: {dochazka.Odchod?.ToString("HH:mm") ?? "N/A"}");
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
                        MessageBox.Show("Vyberte záznam k editaci.", "Chyba",
                            MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Editace záznamu");
            }
        }

        private void EditZaznam(int index)
        {
            try
            {
                var dochazky = _dochazkaService.GetAll();
                if (index < 0 || index >= dochazky.Count) return;

                Dochazka vybranaDochazka = dochazky[index];

                OpenTimeInputWindow("Editovat Příchod", (novyPrichod) =>
                {
                    OpenTimeInputWindow("Editovat Odchod", (novyOdchod) =>
                    {
                        novyPrichod = new DateTime(vybranaDochazka.Prichod.Year, vybranaDochazka.Prichod.Month,
                            vybranaDochazka.Prichod.Day, novyPrichod.Hour, novyPrichod.Minute, novyPrichod.Second);
                        novyOdchod = new DateTime(vybranaDochazka.Prichod.Year, vybranaDochazka.Prichod.Month,
                            vybranaDochazka.Prichod.Day, novyOdchod.Hour, novyOdchod.Minute, novyOdchod.Second);

                        if (novyOdchod <= novyPrichod)
                        {
                            MessageBox.Show("Odchod musí být později než příchod.", "Chyba",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        vybranaDochazka.Prichod = novyPrichod;
                        vybranaDochazka.Odchod = novyOdchod;
                        vybranaDochazka.VypocetRozdilu();

                        _dochazkaService.Update(index, vybranaDochazka);

                        MessageBox.Show("Záznam byl úspěšně upraven.", "Editace záznamu",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Editace záznamu");
            }
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
                    MessageBox.Show("Zadaný čas není platný.", "Chyba",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            panel.Children.Add(okButton);

            timeWindow.Content = panel;
            timeWindow.ShowDialog();
        }

        private void BtnNapoveda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Docházka Tracker:\n\n" +
                            "1. Klikněte na 'Doplnit Docházku' pro přidání nového záznamu.\n" +
                            "2. Použijte 'Exportovat do Excelu' pro uložení dat.\n" +
                            "3. Pomocí 'Zobrazit Statistiky' analyzujte data za jednotlivé měsíce.\n" +
                            "4. Všechny změny jsou automaticky ukládány.",
                            "Nápověda", MessageBoxButton.OK, MessageBoxImage.Information);
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

            var dochazky = _dochazkaService.GetAll();
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

        private string RemoveDayOfWeek(string input)
        {
            string[] daysOfWeek = { "pondělí", "úterý", "středa", "čtvrtek", "pátek", "sobota", "neděle" };
            foreach (var day in daysOfWeek)
            {
                input = input.Replace(day, "").Trim();
            }
            return input;
        }
    }
}
