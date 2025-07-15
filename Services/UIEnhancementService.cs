using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DochazkaTracker.Services
{
    public static class UIEnhancementService
    {
        public static void EnhanceMainButtons(Button zobrazitDochazku, Button zobrazitStatistiky)
        {
            // Zvýraznění tlačítka "Zobrazit docházku"
            EnhanceButton(zobrazitDochazku, Brushes.LightGreen, Brushes.DarkGreen, "📊");

            // Zvýraznění tlačítka "Zobrazit statistiky"
            EnhanceButton(zobrazitStatistiky, Brushes.LightBlue, Brushes.DarkBlue, "📈");
        }

        private static void EnhanceButton(Button button, Brush normalColor, Brush hoverColor, string icon)
        {
            // Základní styling
            button.Background = normalColor;
            button.Foreground = Brushes.White;
            button.FontWeight = FontWeights.Bold;
            button.FontSize = 14;
            button.Height = 40;
            button.BorderBrush = hoverColor;
            button.BorderThickness = new Thickness(2);

            // Přidej ikonu do obsahu
            string originalContent = button.Content.ToString();
            button.Content = $"{icon} {originalContent}";

            // Hover efekt
            button.MouseEnter += (s, e) =>
            {
                button.Background = hoverColor;
                ApplyGlowEffect(button);
            };

            button.MouseLeave += (s, e) =>
            {
                button.Background = normalColor;
                RemoveGlowEffect(button);
            };

            // Click animace
            button.Click += (s, e) =>
            {
                ApplyClickAnimation(button);
            };
        }

        private static void ApplyGlowEffect(Button button)
        {
            // Přidej světelný efekt
            var dropShadow = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.White,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.8
            };
            button.Effect = dropShadow;
        }

        private static void RemoveGlowEffect(Button button)
        {
            button.Effect = null;
        }

        private static void ApplyClickAnimation(Button button)
        {
            // Animace stisknutí
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            button.RenderTransform = scaleTransform;
            button.RenderTransformOrigin = new Point(0.5, 0.5);

            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }
    }
}