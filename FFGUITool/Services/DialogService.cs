using System.Net.Mime;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;

namespace FFGUITool.Services
{
    /// <summary>
    /// 对话框服务实现
    /// </summary>
    public class DialogService : IDialogService
    {
        public Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        public async Task<string?> ShowMessage(string title, string message)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new Button
                        {
                            Content = "确定",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)dialog.Content).Children[1];
            okButton.Click += (s, e) => dialog.Close();

            await dialog.ShowDialog(mainWindow);
            return "OK";
        }

        public async Task<bool> ShowConfirmation(string title, string message)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return false;

            var result = false;
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Spacing = 10,
                            Children =
                            {
                                new Button { Content = "是" },
                                new Button { Content = "否" }
                            }
                        }
                    }
                }
            };

            var buttons = (StackPanel)((StackPanel)dialog.Content).Children[1];
            var yesButton = (Button)buttons.Children[0];
            var noButton = (Button)buttons.Children[1];

            yesButton.Click += (s, e) => { result = true; dialog.Close(); };
            noButton.Click += (s, e) => { result = false; dialog.Close(); };

            await dialog.ShowDialog(mainWindow);
            return result;
        }

        public async Task<IStorageFile?> OpenFileDialog(string title, FilePickerFileType[]? filters = null)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.StorageProvider == null) return null;

            filters ??= new[] { new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } } };

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = filters
            });

            return files.Count > 0 ? files[0] : null;
        }

        public async Task<IStorageFolder?> OpenFolderDialog(string title)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.StorageProvider == null) return null;

            var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            return folders.Count > 0 ? folders[0] : null;
        }
    }
}