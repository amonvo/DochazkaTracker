namespace DochazkaTracker.Services
{
    public class NotificationService
    {
        public void ShowNotification(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void SetReminder(TimeSpan time, string message)
        {
            // Placeholder pro budoucí implementaci
        }

        public void CheckForMissingEntries()
        {
            // Placeholder pro budoucí implementaci
        }
    }
}
