using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FFGUITool.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FFGUITool
{
    public partial class SetupWindow : Window
    {
        private readonly FFmpegManager _ffmpegManager;
        public bool SetupCompleted { get; private set; }

        public SetupWindow()
        {
            InitializeComponent();
            _ffmpegManager = new FFmpegManager();
            SetupCompleted = false;
        }

        public SetupWindow(FFmpegManager ffmpegManager)
        {
            InitializeComponent();
            _ffmpegManager = ffmpegManager;
            SetupCompleted = false;
        }

        private async void BrowseFFmpegButton_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择FFmpeg可执行文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("可执行文件")
                    {
                        Patterns = new[] { "*.exe", "ffmpeg", "ffmpeg.exe" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                FFmpegPathTextBox.Text = files[0].Path.LocalPath;
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
                        Patterns = new[] { "*.zip", "*.7z", "*.tar.gz", "*.tar" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                ArchivePathTextBox.Text = files[0].Path.LocalPath;
            }
        }

        private async void SetCustomPathButton_Click(object sender, RoutedEventArgs e)
        {
            var path = FFmpegPathTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                await ShowMessage("错误", "请先选择FFmpeg可执行文件路径");
                return;
            }

            await SetCustomPath(path);
        }

        private async void InstallFromArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            var archivePath = ArchivePathTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(archivePath))
            {
                await ShowMessage("错误", "请先选择FFmpeg压缩包");
                return;
            }

            await InstallFromArchive(archivePath);
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否有选择FFmpeg路径
            var path = FFmpegPathTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(path))
            {
                // 如果有路径，执行设置
                await SetCustomPath(path);
                return;
            }

            // 检查是否有选择压缩包
            var archivePath = ArchivePathTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(archivePath))
            {
                // 如果有压缩包，执行安装
                await InstallFromArchive(archivePath);
                return;
            }

            // 如果都没有选择，提示用户
            await ShowMessage("提示", "请先选择FFmpeg路径或压缩包，或点击跳过继续使用程序");
        }

        private async Task SetCustomPath(string path)
        {
            if (!File.Exists(path))
            {
                await ShowMessage("错误", "指定的文件不存在");
                return;
            }

            try
            {
                StatusText.Text = "验证FFmpeg路径...";
                var success = await _ffmpegManager.SetCustomPath(path);
                
                if (success)
                {
                    SetupCompleted = true;
                    await ShowMessage("成功", "FFmpeg路径设置成功！");
                    Close();
                }
                else
                {
                    await ShowMessage("错误", "指定的文件不是有效的FFmpeg可执行文件");
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("错误", $"设置FFmpeg路径时出错: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "";
            }
        }

        private async Task InstallFromArchive(string archivePath)
        {
            if (!File.Exists(archivePath))
            {
                await ShowMessage("错误", "指定的压缩包文件不存在");
                return;
            }

            try
            {
                StatusText.Text = "正在安装FFmpeg...";
                var success = await _ffmpegManager.InstallFFmpegFromArchive(archivePath);
                
                if (success)
                {
                    SetupCompleted = true;
                    await ShowMessage("成功", "FFmpeg安装成功！");
                    Close();
                }
                else
                {
                    await ShowMessage("错误", "FFmpeg安装失败");
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("错误", $"安装FFmpeg时出错: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "";
            }
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task ShowMessage(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 350,
                Height = 150,
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

            // 为确定按钮添加事件处理
            var okButton = (Button)((StackPanel)dialog.Content).Children[1];
            okButton.Click += (s, e) => dialog.Close();

            await dialog.ShowDialog(this);
        }
    }
}