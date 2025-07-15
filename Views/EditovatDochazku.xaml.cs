using System.Windows;
using DochazkaTracker.Models;

namespace DochazkaTracker.Views
{
    public partial class EditovatDochazku : Window
    {
        private readonly AppConfig _config;
        private readonly Dochazka _puvodniDochazka;

        // Property pro upravenou docházku
        public Dochazka UpravenaDochazka { get; private set; }

        public EditovatDochazku(Dochazka dochazka, AppConfig config)
        {
            InitializeComponent();
            _puvodniDochazka = dochazka;
            _config = config;
        }

        // Pak sem dáš logiku pro úpravu docházky, např. při kliknutí na OK
        // public void Potvrdit()
        // {
        //     UpravenaDochazka = ... // vytvoř podle změn
        //     DialogResult = true;
        // }
    }
}
