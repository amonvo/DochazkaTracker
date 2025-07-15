using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DochazkaTracker.Models;
using DochazkaTracker.Services;
using DochazkaTracker.Commands;
using DochazkaTracker.Views; // <- přidej tento using, pokud ještě není

namespace DochazkaTracker.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DochazkaService _dochazkaService;
        private readonly StatistikyService _statistikyService;
        private readonly ExportService _exportService;
        private readonly NotificationService _notificationService;
        private readonly AppConfig _config;

        private ObservableCollection<Dochazka> _dochazky;
        private bool _isLoading;
        private string _statusMessage;
        private Dochazka _selectedDochazka;

        public ObservableCollection<Dochazka> Dochazky
        {
            get => _dochazky;
            set => SetProperty(ref _dochazky, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public Dochazka SelectedDochazka
        {
            get => _selectedDochazka;
            set => SetProperty(ref _selectedDochazka, value);
        }

        // Commands
        public ICommand PridatDochazku { get; }
        public ICommand ExportovatCommand { get; }
        public ICommand ZobrazitStatistikyCommand { get; }
        public ICommand VymazatZaznamyCommand { get; }
        public ICommand EditovatZaznamCommand { get; }
        public ICommand ImportovatCommand { get; }
        public ICommand StartTrackingCommand { get; }
        public ICommand StopTrackingCommand { get; }

        public MainViewModel(
            DochazkaService dochazkaService,
            StatistikyService statistikyService,
            ExportService exportService,
            NotificationService notificationService,
            AppConfig config)
        {
            _dochazkaService = dochazkaService;
            _statistikyService = statistikyService;
            _exportService = exportService;
            _notificationService = notificationService;
            _config = config;

            Dochazky = new ObservableCollection<Dochazka>();

            // Inicializace příkazů
            PridatDochazku = new RelayCommand(async () => await PridatNovouDochazku());
            ExportovatCommand = new RelayCommand(async () => await ExportovatData(), () => Dochazky.Any());
            ZobrazitStatistikyCommand = new RelayCommand(ZobrazitStatistiky, () => Dochazky.Any());
            VymazatZaznamyCommand = new RelayCommand(VymazatVsechnyZaznamy);
            EditovatZaznamCommand = new RelayCommand(EditovatVybranyZaznam, () => SelectedDochazka != null);
            ImportovatCommand = new RelayCommand(async () => await ImportovatData());
            StartTrackingCommand = new RelayCommand(StartTracking);
            StopTrackingCommand = new RelayCommand(StopTracking);

            LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Načítání dat...";

                await _dochazkaService.LoadDataAsync();
                var data = _dochazkaService.GetAll();

                Dochazky.Clear();
                foreach (var item in data)
                {
                    Dochazky.Add(item);
                }

                StatusMessage = $"Načteno {Dochazky.Count} záznamů";
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Načítání dat");
                StatusMessage = "Chyba při načítání dat";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PridatNovouDochazku()
        {
            try
            {
                // Otevře dialog pro přidání nové docházky
                var dialog = new PridatDochazku(_config);
                if (dialog.ShowDialog() == true)
                {
                    var novaDochazka = dialog.NovaDochazka;
                    _dochazkaService.Add(novaDochazka);
                    Dochazky.Add(novaDochazka);
                    StatusMessage = "Docházka byla úspěšně přidána";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Přidání docházky");
            }
        }

        private async Task ExportovatData()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Exportování dat...";

                // Pokud je metoda statická, použij třídu, NE instanci!
                var filePath = await ExportService.ExportToExcelAsync(Dochazky.ToList());

                StatusMessage = $"Data exportována do {filePath}";
                _notificationService.ShowNotification("Export dokončen", $"Soubor uložen: {filePath}");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Export dat");
                StatusMessage = "Chyba při exportu";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ZobrazitStatistiky()
        {
            try
            {
                var statistikyWindow = new StatistikyWindow(_statistikyService, Dochazky.ToList());
                statistikyWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Zobrazení statistik");
            }
        }

        private void VymazatVsechnyZaznamy()
        {
            try
            {
                var result = MessageBox.Show(
                    "Opravdu chcete vymazat všechny záznamy?",
                    "Potvrzení",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _dochazkaService.Clear();
                    Dochazky.Clear();
                    StatusMessage = "Všechny záznamy byly vymazány";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Vymazání záznamů");
            }
        }

        private void EditovatVybranyZaznam()
        {
            if (SelectedDochazka == null) return;

            try
            {
                var dialog = new EditovatDochazku(SelectedDochazka, _config);
                if (dialog.ShowDialog() == true)
                {
                    var index = Dochazky.IndexOf(SelectedDochazka);
                    _dochazkaService.Update(index, dialog.UpravenaDochazka);

                    // Aktualizovat v ObservableCollection
                    Dochazky[index] = dialog.UpravenaDochazka;
                    StatusMessage = "Záznam byl úspěšně upraven";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Editace záznamu");
            }
        }

        private async Task ImportovatData()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xls",
                    Title = "Vyberte Excel soubor pro import"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    StatusMessage = "Importování dat...";

                    var importedData = ImportService.ImportFromExcel(openFileDialog.FileName);

                    foreach (var item in importedData)
                    {
                        _dochazkaService.Add(item);
                        Dochazky.Add(item);
                    }

                    StatusMessage = $"Importováno {importedData.Count} záznamů";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Import dat");
                StatusMessage = "Chyba při importu";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void StartTracking()
        {
            try
            {
                TimeTrackingService.Instance.StartTracking();
                StatusMessage = "Sledování času spuštěno";
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Spuštění sledování času");
            }
        }

        private void StopTracking()
        {
            try
            {
                var elapsed = TimeTrackingService.Instance.StopTracking();

                // Automaticky vytvořit záznam
                var novaDochazka = new Dochazka
                {
                    Prichod = DateTime.Now.Subtract(elapsed),
                    Odchod = DateTime.Now,
                    Rezim = "Kancelář"
                };
                novaDochazka.VypocetRozdilu();

                _dochazkaService.Add(novaDochazka);
                Dochazky.Add(novaDochazka);

                StatusMessage = $"Sledování ukončeno. Pracovní doba: {elapsed:hh\\:mm}";
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Ukončení sledování času");
            }
        }
    }
}
