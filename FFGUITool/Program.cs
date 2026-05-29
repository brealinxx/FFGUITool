using System;
using System.Threading.Tasks;
using Avalonia;
using FFGUITool.Services;

namespace FFGUITool
{
    internal class Program
    {
        // Program entry point.
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                AppLogger.Error("Unhandled app domain exception.", e.ExceptionObject as Exception);
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                AppLogger.Error("Unobserved task exception.", e.Exception);
                e.SetObserved();
            };

            try
            {
                AppLogger.Info("Application starting.");
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                AppLogger.Info("Application exited.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Application crashed during startup or shutdown.", ex);
                throw;
            }
        }

        // Avalonia configuration.
        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect();

#if DEBUG
            builder.LogToTrace();
#endif

            return builder;
        }
    }
}
