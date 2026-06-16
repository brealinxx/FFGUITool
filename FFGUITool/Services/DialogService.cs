using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace FFGUITool.Services
{
    /// <summary>
    /// Dialog service implementation.
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

            var dialog = CreateDialogWindow(mainWindow, title);
            var okButton = CreatePrimaryButton(LocalizationService.T("Dialog.Ok"));
            okButton.Click += (s, e) => dialog.Close("OK");

            dialog.Content = CreateDialogContent(mainWindow, title, message, new[] { okButton });

            await dialog.ShowDialog<string?>(mainWindow);
            return "OK";
        }

        public async Task<string?> ShowScrollableMessage(string title, string message)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            var dialog = CreateDialogWindow(mainWindow, title);
            dialog.Width = 720;
            dialog.Height = 560;
            dialog.MinHeight = 360;
            dialog.SizeToContent = SizeToContent.Manual;
            dialog.CanResize = true;

            var okButton = CreatePrimaryButton(LocalizationService.T("Dialog.Ok"));
            okButton.Click += (s, e) => dialog.Close("OK");

            dialog.Content = CreateDialogContent(mainWindow, title, message, new[] { okButton }, scrollMessage: true);

            await dialog.ShowDialog<string?>(mainWindow);
            return "OK";
        }

        public async Task<string?> ShowActionMessage(string title, string message, IReadOnlyList<(string Id, string Text)> actions)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return null;

            var dialog = CreateDialogWindow(mainWindow, title);
            dialog.Width = 560;
            var buttons = new List<Button>();
            foreach (var action in actions)
            {
                var button = action.Id == "OK" ? CreatePrimaryButton(action.Text) : CreateSecondaryButton(action.Text);
                button.Click += (s, e) => dialog.Close(action.Id);
                buttons.Add(button);
            }

            dialog.Content = CreateDialogContent(mainWindow, title, message, buttons.ToArray());
            return await dialog.ShowDialog<string?>(mainWindow);
        }

        public async Task<bool> ShowConfirmation(string title, string message)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return false;

            var result = false;
            var dialog = CreateDialogWindow(mainWindow, title);

            var cancelButton = CreateSecondaryButton(LocalizationService.T("Dialog.Cancel"));
            cancelButton.Click += (s, e) =>
            {
                result = false;
                dialog.Close(false);
            };

            var confirmButton = CreatePrimaryButton(LocalizationService.T("Dialog.Ok"));
            confirmButton.Click += (s, e) =>
            {
                result = true;
                dialog.Close(true);
            };

            dialog.Content = CreateDialogContent(mainWindow, title, message, new[] { cancelButton, confirmButton });

            await dialog.ShowDialog<bool>(mainWindow);
            return result;
        }

        public async Task<IStorageFile?> OpenFileDialog(string title, FilePickerFileType[]? filters = null)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.StorageProvider == null) return null;

            filters ??= new[] { new FilePickerFileType(LocalizationService.T("Picker.AllFiles")) { Patterns = new[] { "*.*" } } };

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = filters
            });

            return files.Count > 0 ? files[0] : null;
        }

        public async Task<IReadOnlyList<IStorageFile>> OpenFilesDialog(string title, FilePickerFileType[]? filters = null)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow?.StorageProvider == null) return [];

            filters ??= new[] { new FilePickerFileType(LocalizationService.T("Picker.AllFiles")) { Patterns = new[] { "*.*" } } };

            return await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter = filters
            });
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

        private static Window CreateDialogWindow(Window owner, string title)
        {
            var theme = owner.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
            var isDark = theme == ThemeVariant.Dark;

            return new Window
            {
                Title = title,
                Width = 460,
                MinHeight = 220,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                RequestedThemeVariant = theme,
                Background = BrushFor(isDark ? "#111827" : "#F8FAFC"),
                Icon = owner.Icon
            };
        }

        private static Control CreateDialogContent(Window owner, string title, string message, Button[] buttons, bool scrollMessage = false)
        {
            var isDark = owner.ActualThemeVariant == ThemeVariant.Dark;
            var accentBrush = BrushFor(isDark ? "#60A5FA" : "#2563EB");
            var cardBrush = BrushFor(isDark ? "#1F2937" : "#FFFFFF");
            var borderBrush = BrushFor(isDark ? "#374151" : "#E5E7EB");
            var primaryTextBrush = BrushFor(isDark ? "#F9FAFB" : "#111827");
            var secondaryTextBrush = BrushFor(isDark ? "#D1D5DB" : "#4B5563");
            var iconBrush = GetDialogIconBrush(title, accentBrush);
            var buttonPanel = new StackPanel
            {
                [Grid.RowProperty] = 2,
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            foreach (var button in buttons)
            {
                buttonPanel.Children.Add(button);
            }

            Control messageControl = new SelectableTextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = secondaryTextBrush,
                FontSize = 14,
                Margin = new Thickness(0, 22, 0, 22)
            };

            if (scrollMessage)
            {
                messageControl = new ScrollViewer
                {
                    [Grid.RowProperty] = 1,
                    Margin = new Thickness(0, 22, 0, 22),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Content = new SelectableTextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = secondaryTextBrush,
                        FontSize = 13,
                        FontFamily = FontFamily.Parse("Consolas, Cascadia Code, Monospace")
                    }
                };
            }
            else
            {
                messageControl[Grid.RowProperty] = 1;
            }

            return new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24),
                Child = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                    Children =
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 14,
                            Children =
                            {
                                new Border
                                {
                                    Width = 44,
                                    Height = 44,
                                    CornerRadius = new CornerRadius(10),
                                    Background = iconBrush,
                                    Child = new TextBlock
                                    {
                                        Text = GetDialogIconText(title),
                                        FontSize = 24,
                                        FontWeight = FontWeight.Bold,
                                        Foreground = Brushes.White,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center
                                    }
                                },
                                new StackPanel
                                {
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = title,
                                            FontSize = 19,
                                            FontWeight = FontWeight.SemiBold,
                                            Foreground = primaryTextBrush
                                        },
                                        new TextBlock
                                        {
                                            Text = "FFGUITool",
                                            FontSize = 12,
                                            Foreground = secondaryTextBrush,
                                            Margin = new Thickness(0, 4, 0, 0)
                                        }
                                    }
                                }
                            }
                        },
                        messageControl,
                        buttonPanel
                    }
                }
            };
        }

        private static Button CreatePrimaryButton(string text)
        {
            return new Button
            {
                Content = text,
                MinWidth = 92,
                Padding = new Thickness(18, 8),
                CornerRadius = new CornerRadius(7),
                Background = BrushFor("#2563EB"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
        }

        private static string GetDialogIconText(string title)
        {
            if (title.Contains("成功") || title.Contains("Success"))
            {
                return "✓";
            }

            if (title.Contains("关于") || title.Contains("About"))
            {
                return "i";
            }

            return "!";
        }

        private static IBrush GetDialogIconBrush(string title, IBrush fallbackBrush)
        {
            if (title.Contains("成功") || title.Contains("Success"))
            {
                return BrushFor("#16A34A");
            }

            return fallbackBrush;
        }

        private static Button CreateSecondaryButton(string text)
        {
            return new Button
            {
                Content = text,
                MinWidth = 92,
                Padding = new Thickness(18, 8),
                CornerRadius = new CornerRadius(7),
                Background = Brushes.Transparent,
                Foreground = BrushFor("#6B7280"),
                BorderBrush = BrushFor("#D1D5DB"),
                BorderThickness = new Thickness(1),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
        }

        private static SolidColorBrush BrushFor(string color)
        {
            return new SolidColorBrush(Color.Parse(color));
        }
    }
}
