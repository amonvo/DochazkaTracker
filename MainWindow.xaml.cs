using DochazkaTracker.Models;
using DochazkaTracker.Services;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

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
        private readonly AdminService _adminService;
        private readonly DovolenaService _dovolenaService;
        private readonly SickDayService _sickDayService;
        private readonly AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();

            _config = AppConfig.Load();
            _dochazkaService = new DochazkaService();
            _statistikyService = new StatistikyService();
            _adminService = new AdminService();
            _dovolenaService = new DovolenaService(_config, _dochazkaService);
            _sickDayService = new SickDayService(_config, _dochazkaService);

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

                EnhanceMainButtons();
            }
            catch
            {
                // Ignorujeme chyby s animací
            }
        }

        private void EnhanceMainButtons()
        {
            try
            {
                var buttons = GetLogicalChildren<Button>(this);

                Button zobrazitDochazku = buttons.FirstOrDefault(b => b.Content?.ToString()?.Contains("Zobrazit docházku") == true);
                Button zobrazitStatistiky = buttons.FirstOrDefault(b => b.Content?.ToString()?.Contains("Zobrazit statistiky") == true);

                if (zobrazitDochazku != null && zobrazitStatistiky != null)
                {
                    UIEnhancementService.EnhanceMainButtons(zobrazitDochazku, zobrazitStatistiky);
                }
            }
            catch
            {
                // Ignoruj chyby při stylování
            }
        }

        private static IEnumerable<T> GetLogicalChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            foreach (object child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is T typedChild)
                    yield return typedChild;

                if (child is DependencyObject dependencyChild)
                {
                    foreach (var descendant in GetLogicalChildren<T>(dependencyChild))
                        yield return descendant;
                }
            }
        }

        private void BtnDoplnitDochazku_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DatePicker datePicker = new DatePicker()
                {
                    SelectedDate = DateTime.Today
                };

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
                    string vybranyRezim = rezimComboBox.SelectedItem?.ToString() ?? "Kancelář";

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
                if (!_adminService.IsAdminLoggedIn())
                {
                    ShowAdminLoginDialog(sender, e);
                    return;
                }

                if (MessageBox.Show("Opravdu chcete vymazat všechny záznamy?\n\nTato akce je nevratná!",
                    "Vymazat záznamy - Admin režim", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _dochazkaService.Clear();

                    if (File.Exists("dochazka.xlsx"))
                    {
                        File.Delete("dochazka.xlsx");
                    }

                    MessageBox.Show("Všechny záznamy byly vymazány.", "Vymazáno - Admin",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Vymazání záznamů");
            }
        }

        private void ShowAdminLoginDialog(object sender, RoutedEventArgs e)
        {
            Window loginWindow = new Window
            {
                Title = "Admin přihlášení",
                Width = 350,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(20),
                Background = Brushes.White
            };

            TextBlock header = new TextBlock
            {
                Text = "🔐 Admin přihlášení",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.DarkRed
            };
            panel.Children.Add(header);

            if (_adminService.IsBlocked())
            {
                var timeRemaining = _adminService.GetBlockTimeRemaining();
                TextBlock blockedMessage = new TextBlock
                {
                    Text = $"Příliš mnoho neúspěšných pokusů!\n\nZkuste to znovu za: {timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}",
                    FontSize = 14,
                    Foreground = Brushes.Red,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                panel.Children.Add(blockedMessage);

                Button closeButton = new Button
                {
                    Content = "Zavřít",
                    Width = 100,
                    Height = 35,
                    Background = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                closeButton.Click += (s, args) => loginWindow.Close();
                panel.Children.Add(closeButton);

                loginWindow.Content = panel;
                loginWindow.ShowDialog();
                return;
            }

            TextBlock description = new TextBlock
            {
                Text = "Pro smazání všech záznamů je vyžadováno admin heslo:",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(description);

            TextBlock passwordLabel = new TextBlock
            {
                Text = "Heslo:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };
            panel.Children.Add(passwordLabel);

            PasswordBox passwordBox = new PasswordBox
            {
                FontSize = 14,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(passwordBox);

            TextBlock attemptsLabel = new TextBlock
            {
                Text = $"Zbývající pokusy: {_adminService.GetRemainingLoginAttempts()}",
                FontSize = 12,
                Foreground = Brushes.Orange,
                Margin = new Thickness(0, 0, 0, 15)
            };
            panel.Children.Add(attemptsLabel);

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Button loginButton = new Button
            {
                Content = "Přihlásit",
                Width = 100,
                Height = 35,
                Background = Brushes.LightGreen,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0)
            };

            Button cancelButton = new Button
            {
                Content = "Zrušit",
                Width = 100,
                Height = 35,
                Background = Brushes.LightCoral,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };

            loginButton.Click += (s, args) =>
            {
                if (string.IsNullOrEmpty(passwordBox.Password))
                {
                    MessageBox.Show("Zadejte heslo.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_adminService.LoginAdmin(passwordBox.Password))
                {
                    MessageBox.Show("Úspěšně přihlášen jako admin!", "Přihlášení",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    loginWindow.Close();

                    // Znovu zavolej funkci mazání s původními parametry
                    BtnVymazatZaznamy_Click(sender, e);
                }
                else
                {
                    MessageBox.Show($"Nesprávné heslo!\nZbývající pokusy: {_adminService.GetRemainingLoginAttempts()}",
                        "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);

                    attemptsLabel.Text = $"Zbývající pokusy: {_adminService.GetRemainingLoginAttempts()}";
                    passwordBox.Clear();
                }
            };

            cancelButton.Click += (s, args) => loginWindow.Close();

            passwordBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    loginButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            };

            buttonPanel.Children.Add(loginButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);

            loginWindow.Content = panel;
            loginWindow.ShowDialog();
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
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Background = Brushes.LightBlue,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold
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
                    DateTime vychodziOdchod = vybranaDochazka.Odchod ?? vybranaDochazka.Prichod.AddHours(8.5);

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

                        if ((novyOdchod - novyPrichod).TotalHours > 16)
                        {
                            var result = MessageBox.Show(
                                $"Pracovní doba je {(novyOdchod - novyPrichod).TotalHours:F1} hodin. " +
                                "To je neobvykle dlouhé. Opravdu chcete pokračovat?",
                                "Upozornění", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                            if (result == MessageBoxResult.No) return;
                        }

                        vybranaDochazka.Prichod = novyPrichod;
                        vybranaDochazka.Odchod = novyOdchod;
                        vybranaDochazka.VypocetRozdilu();

                        _dochazkaService.Update(index, vybranaDochazka);

                        MessageBox.Show(
                            $"Záznam byl úspěšně upraven.\n\n" +
                            $"Nový příchod: {novyPrichod:HH:mm}\n" +
                            $"Nový odchod: {novyOdchod:HH:mm}\n" +
                            $"Pracovní doba: {vybranaDochazka.FormatovanyRozdil}",
                            "Editace záznamu", MessageBoxButton.OK, MessageBoxImage.Information);

                    }, vychodziOdchod, vybranaDochazka.Odchod);
                }, vybranaDochazka.Prichod, vybranaDochazka.Prichod);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Editace záznamu");
            }
        }

        private void OpenTimeInputWindow(string title, Action<DateTime> onTimeSelected, DateTime? defaultTime = null, DateTime? originalTime = null)
        {
            DateTime vychoziCas = defaultTime ?? DateTime.Now;

            Window timeWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(15),
                Background = Brushes.White
            };

            TextBlock label = new TextBlock
            {
                Text = $"Nastavte čas pro: {title}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.DarkBlue
            };
            panel.Children.Add(label);

            if (originalTime.HasValue)
            {
                TextBlock originalTimeLabel = new TextBlock
                {
                    Text = $"Původní čas: {originalTime.Value:HH:mm}",
                    FontSize = 12,
                    Foreground = Brushes.DarkOrange,
                    Margin = new Thickness(0, 0, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                panel.Children.Add(originalTimeLabel);
            }

            StackPanel timeInputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock timeLabel = new TextBlock
            {
                Text = "Čas:",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            TextBox timeTextBox = new TextBox
            {
                Text = vychoziCas.ToString("HH:mm"),
                FontSize = 18,
                TextAlignment = TextAlignment.Center,
                Width = 80,
                Height = 35,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderBrush = Brushes.DarkBlue,
                BorderThickness = new Thickness(2)
            };

            timeInputPanel.Children.Add(timeLabel);
            timeInputPanel.Children.Add(timeTextBox);
            panel.Children.Add(timeInputPanel);

            TextBlock sliderLabel = new TextBlock
            {
                Text = "Použijte posuvník pro rychlé nastavení:",
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            panel.Children.Add(sliderLabel);

            StackPanel sliderContainer = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            Slider timeSlider = new Slider
            {
                Minimum = 0,
                Maximum = 1440,
                Value = vychoziCas.Hour * 60 + vychoziCas.Minute,
                TickFrequency = 60,
                TickPlacement = TickPlacement.BottomRight,
                IsSnapToTickEnabled = false,
                Height = 25,
                Margin = new Thickness(0, 0, 0, 5)
            };

            sliderContainer.Children.Add(timeSlider);

            StackPanel timeMarkers = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            for (int hour = 0; hour <= 24; hour += 4)
            {
                TextBlock marker = new TextBlock
                {
                    Text = $"{hour:D2}:00",
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 45,
                    TextAlignment = TextAlignment.Center
                };
                timeMarkers.Children.Add(marker);
            }

            sliderContainer.Children.Add(timeMarkers);
            panel.Children.Add(sliderContainer);

            TextBlock currentTimeDisplay = new TextBlock
            {
                Text = $"Vybraný čas: {vychoziCas:HH:mm}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20),
                Foreground = Brushes.DarkBlue,
                Background = Brushes.LightCyan,
                Padding = new Thickness(10, 5, 10, 5)
            };
            panel.Children.Add(currentTimeDisplay);

            timeSlider.ValueChanged += (s, args) =>
            {
                TimeSpan selectedTime = TimeSpan.FromMinutes(timeSlider.Value);
                string timeString = $"{selectedTime.Hours:D2}:{selectedTime.Minutes:D2}";
                timeTextBox.Text = timeString;
                currentTimeDisplay.Text = $"Vybraný čas: {timeString}";
            };

            timeTextBox.TextChanged += (s, args) =>
            {
                if (TimeSpan.TryParseExact(timeTextBox.Text, @"hh\:mm", null, out TimeSpan parsedTime))
                {
                    timeSlider.Value = parsedTime.TotalMinutes;
                    currentTimeDisplay.Text = $"Vybraný čas: {timeTextBox.Text}";
                }
            };

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Button cancelButton = new Button
            {
                Content = "Zrušit",
                Width = 80,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = Brushes.LightCoral,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            cancelButton.Click += (s, args) => timeWindow.Close();

            Button okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 35,
                Background = Brushes.LightGreen,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            okButton.Click += (s, args) =>
            {
                if (DateTime.TryParseExact(timeTextBox.Text, "HH:mm", null,
                    System.Globalization.DateTimeStyles.None, out DateTime selectedTime))
                {
                    onTimeSelected(selectedTime);
                    timeWindow.Close();
                }
                else
                {
                    MessageBox.Show("Zadaný čas není platný. Použijte formát HH:mm (např. 08:30)",
                        "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            panel.Children.Add(buttonPanel);

            timeWindow.Content = panel;
            timeWindow.ShowDialog();
        }

        private void BtnPlanovaníDovolené_Click(object sender, RoutedEventArgs e)
        {
            ShowVacationPlanningDialog();
        }

        private void ShowVacationPlanningDialog()
        {
            Window vacationWindow = new Window
            {
                Title = "Plánování dovolené",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(15) };

            panel.Children.Add(new TextBlock
            {
                Text = "📅 NAPLÁNOVAT DOVOLENOU",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.DarkBlue
            });

            int zbyvajiciDny = _dovolenaService.GetZbyvajiVacationDays(DateTime.Now.Year);
            panel.Children.Add(new TextBlock
            {
                Text = $"Zbývající dovolená na {DateTime.Now.Year}: {zbyvajiciDny} dní",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = zbyvajiciDny > 0 ? Brushes.Green : Brushes.Red,
                FontWeight = FontWeights.Bold
            });

            panel.Children.Add(new TextBlock { Text = "Od:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) });
            DatePicker odDatePicker = new DatePicker
            {
                SelectedDate = DateTime.Today,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(odDatePicker);

            panel.Children.Add(new TextBlock { Text = "Do:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) });
            DatePicker doDatePicker = new DatePicker
            {
                SelectedDate = DateTime.Today,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(doDatePicker);

            panel.Children.Add(new TextBlock { Text = "Popis:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) });
            TextBox popisTextBox = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            panel.Children.Add(popisTextBox);

            Button naplanovatButton = new Button
            {
                Content = "Naplánovat dovolenou",
                Height = 35,
                Background = Brushes.LightGreen,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };

            naplanovatButton.Click += (s, args) =>
            {
                if (!odDatePicker.SelectedDate.HasValue || !doDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Vyberte datum od a do.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DateTime od = odDatePicker.SelectedDate.Value;
                DateTime doValue = doDatePicker.SelectedDate.Value;

                if (od > doValue)
                {
                    MessageBox.Show("Datum 'od' musí být dříve než 'do'.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_dovolenaService.PridejPlanovanouDovolenu(od, doValue, popisTextBox.Text))
                {
                    MessageBox.Show("Dovolená byla naplánována!", "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);
                    vacationWindow.Close();
                }
                else
                {
                    MessageBox.Show("Dovolená se překrývá s již naplánovanou dovolenou.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            panel.Children.Add(naplanovatButton);
            vacationWindow.Content = panel;
            vacationWindow.ShowDialog();
        }

        private void BtnSickDay_Click(object sender, RoutedEventArgs e)
        {
            ShowSickDayDialog();
        }

        private void ShowSickDayDialog()
        {
            Window sickWindow = new Window
            {
                Title = "Nemocenská",
                Width = 500,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(15) };

            panel.Children.Add(new TextBlock
            {
                Text = "🤒 PŘIDAT NEMOCENSKOU",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.DarkRed
            });

            panel.Children.Add(new TextBlock { Text = "Datum:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) });
            DatePicker datumDatePicker = new DatePicker
            {
                SelectedDate = DateTime.Today,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(datumDatePicker);

            panel.Children.Add(new TextBlock { Text = "Důvod:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) });
            ComboBox duvodComboBox = new ComboBox
            {
                ItemsSource = new[] { "Chřipka", "Nachlazení", "Bolest hlavy", "Zažívací potíže", "Úraz", "Chronické onemocnění", "Jiné" },
                SelectedIndex = 0,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(duvodComboBox);

            CheckBox navstevneLekarCheckBox = new CheckBox
            {
                Content = "Návštěva lékaře",
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 14
            };
            panel.Children.Add(navstevneLekarCheckBox);

            panel.Children.Add(new TextBlock { Text = "Poznámka:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5) });
            TextBox poznamkaTextBox = new TextBox
            {
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            panel.Children.Add(poznamkaTextBox);

            Button pridatButton = new Button
            {
                Content = "Přidat nemocenskou",
                Height = 35,
                Background = Brushes.LightCoral,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };

            pridatButton.Click += (s, args) =>
            {
                if (!datumDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Vyberte datum.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DateTime datum = datumDatePicker.SelectedDate.Value;
                string duvod = duvodComboBox.SelectedItem?.ToString() ?? "Jiné";
                bool navstevneLekar = navstevneLekarCheckBox.IsChecked == true;
                string poznamka = poznamkaTextBox.Text;

                if (_sickDayService.PridejSickDay(datum, duvod, navstevneLekar, poznamka))
                {
                    MessageBox.Show("Nemocenská byla přidána!", "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);
                    sickWindow.Close();
                }
                else
                {
                    MessageBox.Show("Pro tento den již existuje záznam o nemocenské.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            panel.Children.Add(pridatButton);
            sickWindow.Content = panel;
            sickWindow.ShowDialog();
        }

        private void BtnNapoveda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Docházka Tracker - Rozšířená verze:\n\n" +
                            "ZÁKLADNÍ FUNKCE:\n" +
                            "• Doplnit Docházku - Přidá nový záznam\n" +
                            "• Zobrazit Docházku - Ukáže všechny záznamy\n" +
                            "• Zobrazit Statistiky - Měsíční přehledy\n" +
                            "• Editovat Záznam - Upraví existující záznam\n" +
                            "• Exportovat/Importovat - Excel podpora\n\n" +
                            "NOVÉ FUNKCE:\n" +
                            "• Admin přístup - Ochrana mazání (heslo: admin123)\n" +
                            "• Plánování dovolené - Naplánuj dovolenou dopředu\n" +
                            "• Sick Day - Eviduj nemocenské\n" +
                            "• Vylepšený časový dialog - Zachovává původní čas\n" +
                            "• Zvýrazněné tlačítka - Lepší přehlednost\n\n" +
                            "Všechny změny jsou automaticky ukládány.",
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
                        string columnName = worksheet.Cells[1, col].Value?.ToString() ?? "";
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