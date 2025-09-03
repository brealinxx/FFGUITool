using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using FFGUITool.Views;
using FFGUITool.ViewModels;

namespace FFGUITool
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Try to get MainWindow from DI container
                MainWindow? mainWindow = null;
                
                if (Program.ServiceProvider != null)
                {
                    try
                    {
                        // Get ViewModel from DI
                        var viewModel = Program.ServiceProvider.GetRequiredService<MainWindowViewModel>();
                        mainWindow = new MainWindow(viewModel);
                    }
                    catch
                    {
                        // Fallback to parameterless constructor
                        mainWindow = new MainWindow();
                    }
                }
                else
                {
                    // Create without DI
                    mainWindow = new MainWindow();
                }
                
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}