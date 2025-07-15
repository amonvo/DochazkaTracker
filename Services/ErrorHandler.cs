namespace DochazkaTracker.Services
{
    public static class ErrorHandler
    {
        public static void HandleException(Exception ex, string context = "")
        {
            string message = $"Došlo k chybě: {ex.Message}";
            if (!string.IsNullOrEmpty(context))
            {
                message = $"{context}: {message}";
            }

            LogError(ex, context);
            MessageBox.Show(message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void LogError(Exception ex, string context)
        {
            try
            {
                string logPath = "error.log";
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}\n";
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // Pokud se nepodaří logovat, ignorujeme to
            }
        }
    }
}
