using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FFGUITool.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FFGUITool.Views
{
    public partial class SetupWindow : Window
    {
        private readonly IFFmpegService _ffmpegService;
        public bool SetupCompleted { get; private set; }
        
        private TextBox? _ffmpegPathTextBox;
        private TextBox? _archivePathTextBox;
        private TextBlock? _statusText;

        public SetupWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            // Get service from DI if available
            _ffmpegService = Program.ServiceProvider?.GetService(typeof(IFFmpegService)) as IFFmpegService
                             ?? new Services.FFmpegService();
            SetupCompleted = false;
        }

        public SetupWindow(IFFmpegService ffmpegService) : this()
        {
            _ffmpegService = ffmpegService;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        #region Event Handlers

        public async void BrowseFFmpegButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择 FFmpeg 可执行文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("可执行文件")
                    {
                        Patterns = new[] { "*.exe" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0 && _ffmpegPathTextBox != null)
            {
                _ffmpegPathTextBox.Text = files[0].Path.LocalPath;
            }
        }

        private async void BrowseArchiveButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择 FFmpeg 压缩包",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("压缩包")
                    {
                        Patterns = new[] { "*.zip", "*.7z", "*.rar" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0 && _archivePathTextBox != null)
            {
                _archivePathTextBox.Text = files[0].Path.LocalPath;
            }
        }

        private async void SetCustomPathButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_ffmpegPathTextBox?.Text == null || string.IsNullOrWhiteSpace(_ffmpegPathTextBox.Text))
            {
                UpdateStatus("请先选择 FFmpeg 文件路径", true);
                return;
            }

            try
            {
                UpdateStatus("正在验证 FFmpeg 路径...", false);
                
                var result = await _ffmpegService.SetCustomPathAsync(_ffmpegPathTextBox.Text);
                if (result)
                {
                    UpdateStatus("FFmpeg 路径设置成功！", false);
                    SetupCompleted = true;
                }
                else
                {
                    UpdateStatus("无效的 FFmpeg 路径，请选择正确的 ffmpeg.exe 文件", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"设置路径时出错: {ex.Message}", true);
            }
        }

        private async void InstallFromArchiveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_archivePathTextBox?.Text == null || string.IsNullOrWhiteSpace(_archivePathTextBox.Text))
            {
                UpdateStatus("请先选择压缩包文件", true);
                return;
            }

            try
            {
                UpdateStatus("正在从压缩包安装 FFmpeg...", false);
                
                var result = await _ffmpegService.InstallFromArchiveAsync(_archivePathTextBox.Text);
                if (result)
                {
                    UpdateStatus("FFmpeg 安装成功！", false);
                    SetupCompleted = true;
                }
                else
                {
                    UpdateStatus("压缩包安装失败，请检查文件是否包含有效的 FFmpeg", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"安装时出错: {ex.Message}", true);
            }
        }

        private void SkipButton_Click(object? sender, RoutedEventArgs e)
        {
            SetupCompleted = false;
            Close();
        }

        private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
        
        #region Helper Methods

        private void UpdateStatus(string message, bool isError = false)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
                _statusText.Foreground = isError ? 
                    Avalonia.Media.Brushes.Red : 
                    Avalonia.Media.Brushes.Blue;
            }
        }

        #endregion
    }
}