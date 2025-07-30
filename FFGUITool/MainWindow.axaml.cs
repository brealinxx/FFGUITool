using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FFGUITool.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Avalonia.Input;
using System.Threading;
using Avalonia.Controls.Primitives;

namespace FFGUITool
{
    public class VideoInfo
    {
        public int Bitrate { get; set; }
        public double Duration { get; set; }
        public string Resolution { get; set; } = "";
        public double Framerate { get; set; }
        public long FileSize { get; set; }
    }

    public partial class MainWindow : Window
    {
        private string _inputPath = "";
        private string _outputPath = "";
        private int _compressionPercentage = 70;
        private int _calculatedBitrate = 2000;
        private string _codec = "libx264";
        private bool _isProcessing = false;
        private bool _isInitialized = false;
        private readonly FFmpegManager _ffmpegManager;
        private VideoInfo? _currentVideoInfo;
        private Timer? _updateTimer;
        private bool _isTooltipVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            _ffmpegManager = new FFmpegManager();

            Loaded += MainWindow_Loaded;

            _isInitialized = true;
            UpdateCommand();

            // 初始化计时器用于延迟更新
            _updateTimer = new Timer(OnUpdateTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void InitializeEvents()
        {
            // 为NumericUpDown控件添加事件处理器
            if (CompressionNumericUpDown != null)
            {
                CompressionNumericUpDown.ValueChanged += CompressionNumericUpDown_ValueChanged;
            }
    
            // 为比特率输入框添加事件处理器
            if (BitrateNumericUpDown != null)
            {
                BitrateNumericUpDown.ValueChanged += BitrateNumericUpDown_ValueChanged;
            }
        }
        
        private void BitrateNumericUpDown_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!_isInitialized || sender is not NumericUpDown numericUpDown) return;

            var newValue = (int)(numericUpDown.Value ?? 2000);
            _calculatedBitrate = newValue;

            // 同步更新滑动条，但不触发其事件
            if (BitrateSlider != null && Math.Abs(BitrateSlider.Value - newValue) > 0.1)
            {
                BitrateSlider.Value = newValue;
            }

            // 更新显示文本
            if (BitrateValueText != null)
            {
                BitrateValueText.Text = $"{_calculatedBitrate}k";
            }

            // 检查警告和更新预估信息
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private async void MainWindow_Loaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            InitializeEvents(); // 初始化事件处理器
            await InitializeFFmpeg();
        }

        private async Task InitializeFFmpeg()
        {
            // 更新状态显示
            FFmpegStatusText.Text = " - 检测FFmpeg中...";

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
                    await ShowMessage("警告", "FFmpeg未正确配置，某些功能可能无法使用。\n您可以通过菜单重新配置。");
                }
            }

            UpdateFFmpegStatus();
        }

        private void UpdateFFmpegStatus()
        {
            if (_ffmpegManager.IsFFmpegAvailable)
            {
                Title = "FFmpeg 视频压缩工具 - FFmpeg已就绪";
                FFmpegStatusText.Text = " - FFmpeg已就绪";
                FFmpegStatusText.Foreground = Avalonia.Media.Brushes.Green;
            }
            else
            {
                Title = "FFmpeg 视频压缩工具 - FFmpeg未配置";
                FFmpegStatusText.Text = " - FFmpeg未配置";
                FFmpegStatusText.Foreground = Avalonia.Media.Brushes.Red;
            }
        }

        #region 菜单事件处理

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void FFmpegSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var setupWindow = new SetupWindow();
            await setupWindow.ShowDialog(this);

            if (setupWindow.SetupCompleted)
            {
                await _ffmpegManager.InitializeAsync();
                UpdateFFmpegStatus();

                if (_ffmpegManager.IsFFmpegAvailable)
                {
                    await ShowMessage("成功", "FFmpeg配置已更新！");
                }
            }
        }

        private async void RedetectFFmpegMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FFmpegStatusText.Text = " - 重新检测中...";
            FFmpegStatusText.Foreground = Avalonia.Media.Brushes.Gray;

            await _ffmpegManager.InitializeAsync();
            UpdateFFmpegStatus();

            var message = _ffmpegManager.IsFFmpegAvailable
                ? "FFmpeg检测成功！"
                : "未找到FFmpeg，请通过菜单手动配置。";

            await ShowMessage("检测完成", message);
        }

        private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            var ffmpegVersion = await _ffmpegManager.GetFFmpegVersion();

            // 尝试创建图标控件
            Control iconControl;
            try
            {
                iconControl = new Image
                {
                    Source = new Avalonia.Media.Imaging.Bitmap(
                        Avalonia.Platform.AssetLoader.Open(new Uri("avares://FFGUITool/Resource/icon.ico"))),
                    Width = 48,
                    Height = 48,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
            }
            catch
            {
                // 如果图标加载失败，使用文本替代
                iconControl = new TextBlock
                {
                    Text = "🎬",
                    FontSize = 32,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
            }

            var aboutDialog = new Window
            {
                Title = "关于 FFGUITool",
                Width = 480,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new ScrollViewer
                {
                    Margin = new Avalonia.Thickness(10),
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Spacing = 15,
                        Children =
                        {
                            // 应用图标和标题
                            new StackPanel
                            {
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Spacing = 10,
                                Children =
                                {
                                    iconControl,
                                    new TextBlock
                                    {
                                        Text = "FFGUITool",
                                        FontSize = 22,
                                        FontWeight = Avalonia.Media.FontWeight.Bold,
                                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                                    },
                                    new TextBlock
                                    {
                                        Text = "FFmpeg 视频压缩工具",
                                        FontSize = 14,
                                        Foreground = Avalonia.Media.Brushes.Gray,
                                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                                    }
                                }
                            },

                            // 分隔线
                            new Border
                            {
                                Height = 1,
                                Background = Avalonia.Media.Brushes.LightGray,
                                Margin = new Avalonia.Thickness(0, 5, 0, 5)
                            },

                            // 版本信息
                            new StackPanel
                            {
                                Spacing = 8,
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "版本信息",
                                        FontWeight = Avalonia.Media.FontWeight.Bold,
                                        FontSize = 14
                                    },
                                    new TextBlock
                                    {
                                        Text = $"应用版本：{version}",
                                        Margin = new Avalonia.Thickness(10, 0, 0, 0)
                                    },
                                    new TextBlock
                                    {
                                        Text = $"FFmpeg：{ffmpegVersion}",
                                        Margin = new Avalonia.Thickness(10, 0, 0, 0),
                                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                                    }
                                }
                            },

                            // 功能介绍
                            new StackPanel
                            {
                                Spacing = 8,
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "功能特性",
                                        FontWeight = Avalonia.Media.FontWeight.Bold,
                                        FontSize = 14
                                    },
                                    new TextBlock
                                    {
                                        Text = "这是一个基于FFmpeg的视频压缩工具，提供友好的图形界面来简化视频压缩操作。",
                                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                        Margin = new Avalonia.Thickness(10, 0, 0, 0),
                                        LineHeight = 1.3
                                    }
                                }
                            },

                            // 支持的编码器
                            new StackPanel
                            {
                                Spacing = 8,
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "支持的编码器",
                                        FontWeight = Avalonia.Media.FontWeight.Bold,
                                        FontSize = 14
                                    },
                                    new StackPanel
                                    {
                                        Margin = new Avalonia.Thickness(10, 0, 0, 0),
                                        Spacing = 3,
                                        Children =
                                        {
                                            new TextBlock { Text = "• H.264 (libx264) - 广泛兼容" },
                                            new TextBlock { Text = "• H.265 (libx265) - 高压缩比" },
                                            new TextBlock { Text = "• VP9 (libvpx-vp9) - 开源编码" }
                                        }
                                    }
                                }
                            },

                            // 版权信息
                            new Border
                            {
                                Height = 1,
                                Background = Avalonia.Media.Brushes.LightGray,
                                Margin = new Avalonia.Thickness(0, 10, 0, 5)
                            },

                            new TextBlock
                            {
                                Text = "© 2025 FFGUITool Powered by FFmpeg",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                FontSize = 12,
                                Foreground = Avalonia.Media.Brushes.Gray
                            },
                            new TextBlock
                            {
                                Text = "Assembled by brealin",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                FontSize = 12,
                                Foreground = Avalonia.Media.Brushes.Gray
                            },

                            // 确定按钮
                            new Button
                            {
                                Content = "确定",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Width = 100,
                                Height = 35,
                                Margin = new Avalonia.Thickness(0, 10, 0, 0)
                            }
                        }
                    }
                }
            };

            // 为确定按钮添加事件处理
            var okButton = (Button)((StackPanel)((ScrollViewer)aboutDialog.Content).Content).Children.Last();
            okButton.Click += (s, e) => aboutDialog.Close();

            await aboutDialog.ShowDialog(this);
        }

        #endregion

        #region 比特率帮助提示

        private void BitrateHelpButton_PointerEntered(object sender, PointerEventArgs e)
        {
            ShowBitrateTooltip();
        }

        private void BitrateHelpButton_PointerExited(object sender, PointerEventArgs e)
        {
            HideBitrateTooltip();
        }

        private void ShowBitrateTooltip()
        {
            if (_isTooltipVisible) return;

            _isTooltipVisible = true;

            // 设置Popup的PlacementTarget为问号按钮
            if (BitrateTooltipPopup != null && BitrateHelpButton != null)
            {
                BitrateTooltipPopup.PlacementTarget = BitrateHelpButton;
                BitrateTooltipPopup.IsOpen = true;
            }
        }

        private void HideBitrateTooltip()
        {
            _isTooltipVisible = false;
            if (BitrateTooltipPopup != null)
            {
                BitrateTooltipPopup.IsOpen = false;
            }
        }

        #endregion

        #region 视频分析和比特率计算

        private async Task<VideoInfo?> AnalyzeVideo(string videoPath)
        {
            if (!_ffmpegManager.IsFFmpegAvailable || !File.Exists(videoPath))
                return null;

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegManager.FFmpegPath,
                    Arguments = $"-i \"{videoPath}\" -hide_banner",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                return ParseVideoInfo(output, videoPath);
            }
            catch
            {
                return null;
            }
        }

        private VideoInfo? ParseVideoInfo(string ffmpegOutput, string filePath)
        {
            try
            {
                var videoInfo = new VideoInfo();

                // 解析时长
                var durationMatch = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
                if (durationMatch.Success)
                {
                    var hours = int.Parse(durationMatch.Groups[1].Value);
                    var minutes = int.Parse(durationMatch.Groups[2].Value);
                    var seconds = double.Parse(durationMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    videoInfo.Duration = hours * 3600 + minutes * 60 + seconds;
                }

                // 解析比特率
                var bitrateMatch = Regex.Match(ffmpegOutput, @"bitrate: (\d+) kb/s");
                if (bitrateMatch.Success)
                {
                    videoInfo.Bitrate = int.Parse(bitrateMatch.Groups[1].Value);
                }

                // 解析分辨率
                var resolutionMatch = Regex.Match(ffmpegOutput, @"(\d{3,4}x\d{3,4})");
                if (resolutionMatch.Success)
                {
                    videoInfo.Resolution = resolutionMatch.Groups[1].Value;
                }

                // 解析帧率
                var framerateMatch = Regex.Match(ffmpegOutput, @"(\d+(?:\.\d+)?) fps");
                if (framerateMatch.Success)
                {
                    videoInfo.Framerate = double.Parse(framerateMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }

                // 获取文件大小
                videoInfo.FileSize = new FileInfo(filePath).Length;

                return videoInfo;
            }
            catch
            {
                return null;
            }
        }

        private async Task CalculateOptimalBitrate()
        {
            if (_currentVideoInfo == null)
            {
                EstimatedBitrateText.Text = "请先选择视频文件";
                return;
            }

            try
            {
                EstimatedBitrateText.Text = "计算中...";

                // 基于压缩百分比计算目标比特率
                var originalBitrate = _currentVideoInfo.Bitrate;
                var targetBitrate = (int)(originalBitrate * _compressionPercentage / 100.0);

                // 根据编码器调整比特率
                targetBitrate = AdjustBitrateForCodec(targetBitrate, _codec);

                // 确保比特率在合理范围内，但不超过原视频比特率（除非手动设置）
                targetBitrate = Math.Max(1, Math.Min(targetBitrate, originalBitrate));

                _calculatedBitrate = targetBitrate;

                // 动态调整滑动条和输入框的范围
                UpdateBitrateControlsRange(originalBitrate);

                // 更新控件值
                if (BitrateSlider != null)
                {
                    BitrateSlider.Value = _calculatedBitrate;
                }
        
                if (BitrateNumericUpDown != null)
                {
                    BitrateNumericUpDown.Value = _calculatedBitrate;
                }

                // 检查警告和更新预估信息
                UpdateBitrateWarningAndEstimation();
            }
            catch
            {
                EstimatedBitrateText.Text = "计算失败";
                EstimatedBitrateText.Foreground = Avalonia.Media.Brushes.Red;
            }
        }


        private int AdjustBitrateForCodec(int baseBitrate, string codec)
        {
            return codec switch
            {
                "libx265" => (int)(baseBitrate * 0.7), // H.265效率更高
                "libvpx-vp9" => (int)(baseBitrate * 0.8), // VP9效率较高
                _ => baseBitrate // H.264基准
            };
        }
        
        private void UpdateBitrateControlsRange(int originalBitrate)
        {
            // 设置最大值为原视频比特率的1.5倍，最小值为1
            var maxBitrate = Math.Max(originalBitrate * 3 / 2, 50000); // 至少保持50000的上限
            var minBitrate = 1;

            if (BitrateSlider != null)
            {
                BitrateSlider.Minimum = minBitrate;
                BitrateSlider.Maximum = maxBitrate;
        
                // 根据范围调整步进值
                if (maxBitrate > 10000)
                {
                    BitrateSlider.TickFrequency = 1000;
                }
                else if (maxBitrate > 5000)
                {
                    BitrateSlider.TickFrequency = 500;
                }
                else
                {
                    BitrateSlider.TickFrequency = 100;
                }
            }

            if (BitrateNumericUpDown != null)
            {
                BitrateNumericUpDown.Minimum = minBitrate;
                BitrateNumericUpDown.Maximum = maxBitrate;
        
                // 根据范围调整增量
                if (maxBitrate > 10000)
                {
                    BitrateNumericUpDown.Increment = 500;
                }
                else if (maxBitrate > 5000)
                {
                    BitrateNumericUpDown.Increment = 100;
                }
                else
                {
                    BitrateNumericUpDown.Increment = 50;
                }
            }
        }

        private int ClampBitrateByResolution(int bitrate, string resolution)
        {
            var minMax = resolution switch
            {
                var r when r.Contains("3840x2160") => (3000, 50000), // 4K - 提高上限
                var r when r.Contains("2560x1440") => (2000, 30000), // 1440p - 提高上限
                var r when r.Contains("1920x1080") => (1000, 20000), // 1080p - 提高上限
                var r when r.Contains("1280x720") => (500, 10000),   // 720p - 提高上限
                _ => (200, 5000) // 其他分辨率 - 提高上限
            };

            return Math.Clamp(bitrate, minMax.Item1, minMax.Item2);
        }

        private long CalculateEstimatedFileSize(int bitrateKbps, double durationSeconds)
        {
            // 文件大小 = 比特率 * 时长 / 8 (转换为字节)
            // 考虑音频轨道大约占总比特率的10-15%
            var totalBitrateKbps = bitrateKbps + (bitrateKbps * 0.12); // 视频+音频
            return (long)(totalBitrateKbps * 1024 * durationSeconds / 8);
        }

        private void OnUpdateTimerElapsed(object? state)
        {
            // 在UI线程上执行更新
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await CalculateOptimalBitrate();
                UpdateCommand();
            });
        }

        private void ScheduleUpdate()
        {
            // 延迟300ms更新，避免频繁计算
            _updateTimer?.Change(300, Timeout.Infinite);
        }

        #endregion

        #region 原有的事件处理方法

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

                // 分析视频文件
                if (Path.GetExtension(_inputPath).ToLower() is ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv"
                    or ".webm")
                {
                    EstimatedBitrateText.Text = "分析视频中...";
                    EstimatedBitrateText.Foreground = Avalonia.Media.Brushes.Blue;

                    _currentVideoInfo = await AnalyzeVideo(_inputPath);

                    if (_currentVideoInfo != null)
                    {
                        // 显示视频信息面板
                        UpdateVideoInfoPanel();
                        VideoInfoPanel.IsVisible = true;
                    }

                    await CalculateOptimalBitrate();
                }
                else
                {
                    _currentVideoInfo = null;
                    VideoInfoPanel.IsVisible = false;
                    EstimatedBitrateText.Text = "非视频文件";
                    EstimatedBitrateText.Foreground = Avalonia.Media.Brushes.Gray;
                }

                UpdateCommand();
            }
        }

        private void UpdateVideoInfoPanel()
        {
            if (_currentVideoInfo == null) return;

            // 文件大小
            var sizeMB = _currentVideoInfo.FileSize / 1024.0 / 1024.0;
            OriginalSizeText.Text = sizeMB < 1024
                ? $"{sizeMB:F1} MB"
                : $"{sizeMB / 1024.0:F2} GB";

            // 时长
            var duration = TimeSpan.FromSeconds(_currentVideoInfo.Duration);
            DurationText.Text = duration.Hours > 0
                ? $"{duration:hh\\:mm\\:ss}"
                : $"{duration:mm\\:ss}";

            // 比特率
            OriginalBitrateText.Text = $"{_currentVideoInfo.Bitrate} kbps";

            // 分辨率
            ResolutionText.Text = _currentVideoInfo.Resolution;

            // 帧率
            FramerateText.Text = $"{_currentVideoInfo.Framerate:F1} fps";
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

        // 修正：NumericUpDown的事件处理方法
        private void CompressionNumericUpDown_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (!_isInitialized || sender is not NumericUpDown numericUpDown) return;

            _compressionPercentage = (int)(numericUpDown.Value ?? 70);
            ScheduleUpdate();
        }

        private void BitrateSlider_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_isInitialized) return;

            _calculatedBitrate = (int)e.NewValue;
    
            // 同步更新输入框，但不触发其事件
            if (BitrateNumericUpDown != null && Math.Abs((decimal)(BitrateNumericUpDown.Value ?? 0) - (decimal)_calculatedBitrate) > 0.1m)
            {
                BitrateNumericUpDown.Value = _calculatedBitrate;
            }

            if (BitrateValueText != null)
            {
                BitrateValueText.Text = $"{_calculatedBitrate}k";
            }

            // 检查警告和更新预估信息
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }
        
        private void UpdateBitrateWarningAndEstimation()
        {
            if (_currentVideoInfo == null) return;

            // 检查是否超过原视频比特率
            var showWarning = _calculatedBitrate > _currentVideoInfo.Bitrate;
            if (BitrateWarningText != null)
            {
                BitrateWarningText.IsVisible = showWarning;
            }

            // 更新预估信息
            var estimatedSize = CalculateEstimatedFileSize(_calculatedBitrate, _currentVideoInfo.Duration);
            var originalSizeMB = _currentVideoInfo.FileSize / 1024.0 / 1024.0;
            var estimatedSizeMB = estimatedSize / 1024.0 / 1024.0;

            if (estimatedSizeMB > originalSizeMB)
            {
                // 文件会变大的情况
                var increaseRatio = (estimatedSizeMB / originalSizeMB - 1) * 100;
                EstimatedBitrateText.Text =
                    $"{_calculatedBitrate}k (预估: {estimatedSizeMB:F1}MB, 增大: {increaseRatio:F1}%)";
                EstimatedBitrateText.Foreground = Avalonia.Media.Brushes.Orange;
            }
            else
            {
                // 正常压缩的情况
                var compressionRatio = (1 - estimatedSizeMB / originalSizeMB) * 100;
                EstimatedBitrateText.Text =
                    $"{_calculatedBitrate}k (预估: {estimatedSizeMB:F1}MB, 压缩: {compressionRatio:F1}%)";
                EstimatedBitrateText.Foreground = Avalonia.Media.Brushes.Green;
            }
        }

        private async void CodecComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || CodecComboBox?.SelectedItem == null) return;

            if (CodecComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                _codec = selectedItem.Tag.ToString() ?? "libx264";
                await CalculateOptimalBitrate();
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

            // 使用计算出的比特率
            command.Append($"-b:v {_calculatedBitrate}k ");

            // 音频编码器（保持原有或使用AAC）
            command.Append("-c:a aac ");

            // 输出文件
            if (!string.IsNullOrEmpty(_outputPath))
            {
                var inputFileName = Path.GetFileNameWithoutExtension(_inputPath);
                var outputFileName = $"{inputFileName}_compressed_{_compressionPercentage}%.mp4";
                var outputFilePath = Path.Combine(_outputPath, outputFileName);
                command.Append($"\"{outputFilePath}\"");
            }
            else
            {
                command.Append("\"[输出路径]/output.mp4\"");
            }

            CommandTextBox.Text = command.ToString();
            ExecuteButton.IsEnabled = !string.IsNullOrEmpty(_inputPath) && !string.IsNullOrEmpty(_outputPath) &&
                                      _ffmpegManager.IsFFmpegAvailable;
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
                await ShowMessage("错误", $"执行FFmpeg命令时出错:\n{ex.Message}");
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

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}