using System;
using System.IO;
using System.Text.Json;

namespace FFGUITool.Services
{
    public sealed class AppConfig
    {
        public string Theme { get; set; } = "Default";
        public string Language { get; set; } = "zh-CN";
        public bool CloseToTray { get; set; } = false;
        public bool TrayHintShown { get; set; }
        public int ImageParallelism { get; set; } = 2;
    }

    public static class AppConfigService
    {
        public static string AppDataPath { get; } = ResolveAppDataPath();

        private static string ResolveAppDataPath()
        {
            var path = Environment.GetEnvironmentVariable("FFGUITOOL_APP_DATA");
            return !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path)
                ? Path.GetFullPath(path)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FFGUITool");
        }

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
                using var transaction = new OutputTransaction(ConfigPath, true);
                File.WriteAllText(transaction.TemporaryPath, json);
                transaction.Commit();
            }
            catch
            {
            }
        }
    }
}
