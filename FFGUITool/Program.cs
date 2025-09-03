using System;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using FFGUITool.Services;
using FFGUITool.Services.Interfaces;
using FFGUITool.ViewModels;
using FFGUITool.Views;

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
                
                // Build and configure services before creating Avalonia app
                ConfigureServices();
                
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
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void ConfigureServices()
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Configure services
            var services = new ServiceCollection();
            
            // Configuration
            services.AddSingleton<IConfiguration>(configuration);

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

            // Windows (if needed)
            services.AddTransient<MainWindow>();
            services.AddTransient<SetupWindow>();

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
            });

            // Build service provider
            ServiceProvider = services.BuildServiceProvider();
        }

        // 辅助方法来获取服务，提供类型安全的访问
        public static T? GetService<T>() where T : class
        {
            return ServiceProvider?.GetService<T>();
        }

        // 非泛型版本，用于某些特殊情况
        public static object? GetService(Type serviceType)
        {
            return ServiceProvider?.GetService(serviceType);
        }
    }
}