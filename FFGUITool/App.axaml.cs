using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FFGUITool.Views;
using FFGUITool.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            // 注册服务
            services.AddSingleton<FFmpegManager>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<VideoAnalyzer>();
            services.AddTransient<CommandBuilder>();

            // 注册ViewModels
            services.AddTransient<ViewModels.MainWindowViewModel>();
            services.AddTransient<ViewModels.SetupWindowViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}