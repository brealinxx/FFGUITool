using System;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using FFGUITool.Services;
using FFGUITool.Services.Interfaces;
using FFGUITool.ViewModels;

namespace FFGUITool
{
    internal class Program
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/ffguitool-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting FFGUITool application");
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            ServiceProvider = services.BuildServiceProvider();

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .WithInterFont();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.AddSingleton(configuration);

            // Core Services
            services.AddSingleton<IFFmpegService, FFmpegService>();
            services.AddSingleton<IMediaAnalyzer, MediaAnalyzer>();
            services.AddSingleton<IVideoProcessor, VideoProcessor>();
            services.AddSingleton<IAudioProcessor, AudioProcessor>();
            services.AddSingleton<IBatchProcessor, BatchProcessor>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<VideoCompressionViewModel>();
            services.AddTransient<AudioConversionViewModel>();
            services.AddTransient<BatchProcessViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
            });
        }
    }
}