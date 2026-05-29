using System;
using System.IO;
using System.Linq;

namespace FFGUITool.Services
{
    public static class AppLogger
    {
        private static readonly object LockObject = new();

        public static string LogDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FFGUITool",
            "logs");

        public static string CurrentLogPath => Path.Combine(LogDirectory, $"{DateTime.Now:yyyyMMdd}.log");

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message, Exception? exception = null)
        {
            Write("ERROR", exception == null ? message : $"{message}{Environment.NewLine}{exception}");
        }

        public static string Summarize(string text, int maxLines = 16)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            var lines = text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .TakeLast(maxLines);

            return string.Join(Environment.NewLine, lines);
        }

        private static void Write(string level, string message)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                lock (LockObject)
                {
                    File.AppendAllText(CurrentLogPath, line);
                }
            }
            catch
            {
                // Logging must never break the app path it is trying to observe.
            }
        }
    }
}
