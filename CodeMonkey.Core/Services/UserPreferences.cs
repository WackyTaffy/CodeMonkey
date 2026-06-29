using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CodeMonkey.Core.Interfaces;

namespace CodeMonkey.Core.Services
{
    public class UserPreferences : IUserPreferences
    {
        private readonly string _configPath;
        public string ProjectRoot { get; set; } = string.Empty;
        public TrustProfile ActiveProfile { get; set; } = TrustProfile.Balanced;

        public UserPreferences()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preferences.json");
            Load();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                // In a real app, we'd log this to ILogManager
                Console.WriteLine($"Failed to save preferences: {ex.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var prefs = JsonSerializer.Deserialize<UserPreferences>(json);
                    if (prefs != null)
                    {
                        ProjectRoot = prefs.ProjectRoot;
                        ActiveProfile = prefs.ActiveProfile;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load preferences: {ex.Message}");
            }
        }
    }
}
