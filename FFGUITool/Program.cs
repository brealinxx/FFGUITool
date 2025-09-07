using Avalonia;
using System;
using Avalonia.Themes.Fluent;

namespace FFGUITool
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start: {ex}");
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new FluentTheme())
                .WithInterFont()
                .LogToTrace();
    }
}