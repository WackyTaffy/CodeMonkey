using System.Text.Json;
using CodeMonkey.Core.Interfaces;

namespace CodeMonkey.Core.Services
{
    public class UserPreferences : IUserPreferences
    {
        private readonly string _configPath;
        public string ProjectRoot { get; set; } = string.Empty;

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
