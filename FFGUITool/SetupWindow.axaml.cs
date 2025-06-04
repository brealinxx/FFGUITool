using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FFGUITool.Services;
using System;
using System.Threading.Tasks;

namespace FFGUITool
{
    public partial class SetupWindow : Window
    {
        private readonly FFmpegManager _ffmpegManager;
        private bool _isProcessing = false;

        public bool SetupCompleted { get; private set; } = false;

        public SetupWindow()
        {
            InitializeComponent();
            _ffmpegManager = new FFmpegManager();
            Loaded += SetupWindow_Loaded;
        }

        private async void SetupWindow_Loaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await RefreshStatus();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStatus();
        }

        private async Task RefreshStatus()
        {
            StatusText.Text = "检测中...";
            StatusText.Foreground = Avalonia.Media.Brushes.Orange;
            
            await _ffmpegManager.InitializeAsync();
            
            if (_ffmpegManager.IsFFmpegAvailable)
            {
                StatusText.Text = "已安装";
                StatusText.Foreground = Avalonia.Media.Brushes.Green;
                CurrentPathText.Text = _ffmpegManager.FFmpegPath;
                VersionText.Text = await _ffmpegManager.GetFFmpegVersion();
                FinishButton.IsEnabled = true;
            }
            else
            {
                StatusText.Text = "未安装";
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
                CurrentPathText.Text = "无";
                VersionText.Text = "无";
                FinishButton.IsEnabled = false;
            }
        }

        private async void BrowseArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择FFmpeg压缩包",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("压缩包文件")
                    {
                        Patterns = new[] { "*.zip", "*.7z", "*.tar.gz" }
                    }
                }
            });

            if (files.Count > 0)
            {
                ArchivePathTextBox.Text = files[0].Path.LocalPath;
            }
        }

        private async void BrowseExistingButton_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择FFmpeg可执行文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("可执行文件")
                    {
                        Patterns = new[] { "*.exe", "*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                ExistingPathTextBox.Text = files[0].Path.LocalPath;
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            _isProcessing = true;
            InstallButton.IsEnabled = false;
            ProgressBorder.IsVisible = true;

            try
            {
                bool success = false;

                if (InstallFromArchiveRadio.IsChecked == true)
                {
                    if (string.IsNullOrEmpty(ArchivePathTextBox.Text))
                    {
                        await ShowMessage("错误", "请选择FFmpeg压缩包文件");
                        return;
                    }

                    ProgressText.Text = "正在安装FFmpeg...";
                    success = await _ffmpegManager.InstallFFmpegFromArchive(ArchivePathTextBox.Text);
                }
                else if (UseExistingRadio.IsChecked == true)
                {
                    if (string.IsNullOrEmpty(ExistingPathTextBox.Text))
                    {
                        await ShowMessage("错误", "请指定FFmpeg可执行文件路径");
                        return;
                    }

                    ProgressText.Text = "正在验证FFmpeg路径...";
                    success = await _ffmpegManager.SetCustomPath(ExistingPathTextBox.Text);
                }
                else if (UseSystemRadio.IsChecked == true)
                {
                    ProgressText.Text = "正在检测系统FFmpeg...";
                    success = await _ffmpegManager.SetCustomPath("ffmpeg");
                }

                if (success)
                {
                    await ShowMessage("成功", "FFmpeg设置完成！");
                    await RefreshStatus();
                }
                else
                {
                    await ShowMessage("失败", "FFmpeg设置失败，请检查文件路径或压缩包格式");
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("错误", $"操作失败: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                InstallButton.IsEnabled = true;
                ProgressBorder.IsVisible = false;
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCompleted = true;
            Close();
        }

        private async Task ShowMessage(string title, string message)
        {
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
                        new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                    }
                }
            };

            await dialog.ShowDialog(this);
        }
    }
}