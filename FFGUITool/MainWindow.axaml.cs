using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FFGUITool.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace FFGUITool
{
    public partial class MainWindow : Window
    {
        private string _inputPath = "";
        private string _outputPath = "";
        private int _bitrate = 2000;
        private string _codec = "libx264";
        private bool _isProcessing = false;
        private bool _isInitialized = false;
        private readonly FFmpegManager _ffmpegManager;

        public MainWindow()
        {
            InitializeComponent();
            _ffmpegManager = new FFmpegManager();
            _isInitialized = true;
            
            Loaded += MainWindow_Loaded;
            UpdateCommand();
        }

        private async void MainWindow_Loaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await InitializeFFmpeg();
        }

        private async Task InitializeFFmpeg()
        {
            await _ffmpegManager.InitializeAsync();
            
            if (!_ffmpegManager.IsFFmpegAvailable)
            {
                var setupWindow = new SetupWindow();
                await setupWindow.ShowDialog(this);
                
                if (setupWindow.SetupCompleted)
                {
                    await _ffmpegManager.InitializeAsync();
                }
                
                if (!_ffmpegManager.IsFFmpegAvailable)
                {
                    await ShowMessage("警告", "FFmpeg未正确配置，某些功能可能无法使用。\n您可以稍后通过菜单重新配置。");
                }
            }
            
            // 更新窗口标题显示FFmpeg状态
            Title = _ffmpegManager.IsFFmpegAvailable 
                ? "FFmpeg 视频压缩工具 - FFmpeg已就绪" 
                : "FFmpeg 视频压缩工具 - FFmpeg未配置";
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择视频文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("视频文件")
                    {
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm" }
                    },
                    new FilePickerFileType("图片文件")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                _inputPath = files[0].Path.LocalPath;
                InputPathTextBox.Text = _inputPath;
                UpdateCommand();
            }
        }

        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择文件夹",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                _inputPath = folders[0].Path.LocalPath;
                InputPathTextBox.Text = _inputPath;
                UpdateCommand();
            }
        }

        private async void SelectOutputButton_Click(object sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择输出文件夹",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                _outputPath = folders[0].Path.LocalPath;
                OutputPathTextBox.Text = _outputPath;
                UpdateCommand();
            }
        }

        private void BitrateSlider_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_isInitialized) return;
            
            _bitrate = (int)e.NewValue;
            if (BitrateValueText != null)
            {
                BitrateValueText.Text = $"{_bitrate}k";
            }
            UpdateCommand();
        }

        private void CodecComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || CodecComboBox?.SelectedItem == null) return;
            
            if (CodecComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                _codec = selectedItem.Tag.ToString() ?? "libx264";
                UpdateCommand();
            }
        }

        private void UpdateCommand()
        {
            if (!_isInitialized || CommandTextBox == null || ExecuteButton == null) return;
            
            if (string.IsNullOrEmpty(_inputPath))
            {
                CommandTextBox.Text = "请先选择输入文件或文件夹";
                ExecuteButton.IsEnabled = false;
                return;
            }

            var command = new StringBuilder();
            command.Append("ffmpeg ");

            // 输入文件
            if (File.Exists(_inputPath))
            {
                // 单个文件
                command.Append($"-i \"{_inputPath}\" ");
            }
            else if (Directory.Exists(_inputPath))
            {
                // 文件夹 - 这里需要根据具体需求调整
                command.Append($"-i \"{_inputPath}/*.mp4\" ");
            }

            // 视频编码器
            command.Append($"-c:v {_codec} ");

            // 比特率
            command.Append($"-b:v {_bitrate}k ");

            // 音频编码器（保持原有或使用AAC）
            command.Append("-c:a aac ");

            // 输出文件
            if (!string.IsNullOrEmpty(_outputPath))
            {
                var inputFileName = Path.GetFileNameWithoutExtension(_inputPath);
                var outputFileName = $"{inputFileName}_compressed.mp4";
                var outputFilePath = Path.Combine(_outputPath, outputFileName);
                command.Append($"\"{outputFilePath}\"");
            }
            else
            {
                command.Append("\"[输出路径]/output.mp4\"");
            }

            CommandTextBox.Text = command.ToString();
            ExecuteButton.IsEnabled = !string.IsNullOrEmpty(_inputPath) && !string.IsNullOrEmpty(_outputPath);
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            _isProcessing = true;
            ExecuteButton.IsEnabled = false;
            ProgressBar.IsVisible = true;
            ProgressBar.IsIndeterminate = true;

            try
            {
                await ExecuteFFmpegCommand();
            }
            catch (Exception ex)
            {
                // 这里应该显示错误信息给用户
                var dialog = new Window
                {
                    Title = "错误",
                    Width = 400,
                    Height = 200,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = "执行FFmpeg命令时出错:", FontWeight = Avalonia.Media.FontWeight.Bold },
                            new TextBlock { Text = ex.Message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 10, 0, 10) },
                            new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
                        }
                    }
                };
                await dialog.ShowDialog(this);
            }
            finally
            {
                _isProcessing = false;
                ExecuteButton.IsEnabled = true;
                ProgressBar.IsVisible = false;
                ProgressBar.IsIndeterminate = false;
            }
        }

        private async Task ExecuteFFmpegCommand()
        {
            if (!_ffmpegManager.IsFFmpegAvailable)
            {
                throw new Exception("FFmpeg未配置或不可用，请先配置FFmpeg路径");
            }

            var command = CommandTextBox.Text;
            
            // 分离ffmpeg可执行文件和参数
            var arguments = command.Replace("ffmpeg ", "");

            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegManager.FFmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                
                // 读取输出（FFmpeg通常将进度信息输出到stderr）
                var output = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // 成功完成
                    await ShowMessage("完成", "视频处理完成！");
                }
                else
                {
                    throw new Exception($"FFmpeg执行失败，退出代码: {process.ExitCode}\n错误信息: {output}");
                }
            }
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