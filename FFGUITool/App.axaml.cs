using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using FFGUITool.Views;
using FFGUITool.Services;
using FFGUITool.ViewModels;

namespace FFGUITool
{
    public partial class App : Application
    {
        private TrayIcon? _trayIcon;
        private MainWindow? _mainWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            LocalizationService.SetLanguage(AppConfigService.Load().Language, false);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _mainWindow = new MainWindow();
                desktop.MainWindow = _mainWindow;
                ConfigureTrayIcon(desktop);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
        {
            var videoItem = new NativeMenuItem(LocalizationService.T("Mode.Video"));
            videoItem.Click += (_, _) => OpenMode(selectImageMode: false);

            var imageItem = new NativeMenuItem(LocalizationService.T("Mode.Image"));
            imageItem.Click += (_, _) => OpenMode(selectImageMode: true);

            var exitItem = new NativeMenuItem(LocalizationService.T("Menu.Exit"));
            exitItem.Click += (_, _) =>
            {
                if (_mainWindow != null)
                {
                    _mainWindow.AllowClose = true;
                }

                _trayIcon?.Dispose();
                desktop.Shutdown();
            };

            var menu = new NativeMenu();
            menu.Items.Add(videoItem);
            menu.Items.Add(imageItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exitItem);

            var iconStream = AssetLoader.Open(new System.Uri("avares://FFGUITool/Resources/icon.ico"));
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(iconStream),
                ToolTipText = LocalizationService.T("App.Title"),
                Menu = menu,
                IsVisible = true
            };
            _trayIcon.Clicked += (_, _) => ShowMainWindow();

            TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
        }

        private void OpenMode(bool selectImageMode)
        {
            ShowMainWindow();

            if (_mainWindow?.DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            var command = selectImageMode
                ? viewModel.SelectImageModeCommand
                : viewModel.SelectVideoModeCommand;

            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                return;
            }

            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Activate();
        }
    }
}
