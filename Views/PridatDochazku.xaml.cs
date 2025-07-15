using System.Windows;
using DochazkaTracker.Models;

namespace DochazkaTracker.Views
{
    public partial class PridatDochazku : Window
    {
        private readonly AppConfig _config;

        public Dochazka NovaDochazka { get; private set; }

        public PridatDochazku(AppConfig config)
        {
            InitializeComponent();
            _config = config;
        }
    }
}
