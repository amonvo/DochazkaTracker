using System.Collections.Generic;
using System.Windows;
using DochazkaTracker.Models;
using DochazkaTracker.Services;

namespace DochazkaTracker.Views
{
    public partial class StatistikyWindow : Window
    {
        private readonly StatistikyService _statistikyService;
        private readonly List<Dochazka> _dochazky;

        public StatistikyWindow(StatistikyService statistikyService, List<Dochazka> dochazky)
        {
            InitializeComponent();
            _statistikyService = statistikyService;
            _dochazky = dochazky;
        }
    }
}
