using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DochazkaTracker.Services
{
    public class AdminService
    {
        private const string AdminConfigPath = "admin.json";
        private const string DefaultAdminPassword = "admin123"; // Změň si heslo!

        public class AdminConfig
        {
            public string PasswordHash { get; set; } = "";
            public bool IsAdminLoggedIn { get; set; } = false;
            public DateTime LastLoginTime { get; set; } = DateTime.MinValue;
            public int LoginAttempts { get; set; } = 0;
            public DateTime LastFailedAttempt { get; set; } = DateTime.MinValue;
        }

        private AdminConfig _config;

        public AdminService()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(AdminConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(AdminConfigPath);
                    _config = JsonSerializer.Deserialize<AdminConfig>(json) ?? new AdminConfig();
                }
                catch
                {
                    _config = new AdminConfig();
                }
            }
            else
            {
                _config = new AdminConfig();
                // Nastav výchozí heslo
                _config.PasswordHash = HashPassword(DefaultAdminPassword);
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AdminConfigPath, json);
            }
            catch
            {
                // Ignoruj chyby při ukládání
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "DochazkaTracker"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool IsAdminLoggedIn()
        {
            // Automatické odhlášení po 2 hodinách neaktivity
            if (_config.IsAdminLoggedIn && DateTime.Now.Subtract(_config.LastLoginTime).TotalHours > 2)
            {
                LogoutAdmin();
            }
            return _config.IsAdminLoggedIn;
        }

        public bool LoginAdmin(string password)
        {
            // Zablokuj po 5 neúspěšných pokusech na 30 minut
            if (_config.LoginAttempts >= 5 &&
                DateTime.Now.Subtract(_config.LastFailedAttempt).TotalMinutes < 30)
            {
                return false;
            }

            string hashedPassword = HashPassword(password);
            if (hashedPassword == _config.PasswordHash)
            {
                _config.IsAdminLoggedIn = true;
                _config.LastLoginTime = DateTime.Now;
                _config.LoginAttempts = 0;
                SaveConfig();
                return true;
            }
            else
            {
                _config.LoginAttempts++;
                _config.LastFailedAttempt = DateTime.Now;
                SaveConfig();
                return false;
            }
        }

        public void LogoutAdmin()
        {
            _config.IsAdminLoggedIn = false;
            SaveConfig();
        }

        public bool ChangePassword(string oldPassword, string newPassword)
        {
            if (HashPassword(oldPassword) == _config.PasswordHash)
            {
                _config.PasswordHash = HashPassword(newPassword);
                SaveConfig();
                return true;
            }
            return false;
        }

        public int GetRemainingLoginAttempts()
        {
            return Math.Max(0, 5 - _config.LoginAttempts);
        }

        public bool IsBlocked()
        {
            return _config.LoginAttempts >= 5 &&
                   DateTime.Now.Subtract(_config.LastFailedAttempt).TotalMinutes < 30;
        }

        public TimeSpan GetBlockTimeRemaining()
        {
            if (!IsBlocked()) return TimeSpan.Zero;

            TimeSpan elapsed = DateTime.Now.Subtract(_config.LastFailedAttempt);
            return TimeSpan.FromMinutes(30) - elapsed;
        }
    }
}