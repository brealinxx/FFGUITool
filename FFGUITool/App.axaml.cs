using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FFGUITool.Views;
using FFGUITool.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FFGUITool
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            ConfigureServices();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<FFmpegManager>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<VideoAnalyzer>();
            services.AddTransient<CommandBuilder>();
            services.AddTransient<ViewModels.MainWindowViewModel>();
            services.AddTransient<ViewModels.SetupWindowViewModel>();
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}