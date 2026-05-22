using System;
using System.IO;
using System.Text.Json;

namespace FFGUITool.Services
{
    public sealed class AppConfig
    {
        public string Theme { get; set; } = "Default";
        public string Language { get; set; } = "zh-CN";
    }

    public static class AppConfigService
    {
        public static string AppDataPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FFGUITool");

        public static string ConfigPath => Path.Combine(AppDataPath, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return new AppConfig();
                }

                var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
                return config ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(AppDataPath);
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
            }
        }
    }
}
