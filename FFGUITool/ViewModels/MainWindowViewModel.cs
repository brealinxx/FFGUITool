using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFGUITool.Models;
using FFGUITool.Services;

namespace FFGUITool.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly FFmpegManager _ffmpegManager;
        private readonly VideoAnalyzer _videoAnalyzer;
        private readonly CommandBuilder _commandBuilder;
        private readonly IDialogService _dialogService;

        #region 可观察属性

        [ObservableProperty]
        private string _title = "FFmpeg 视频压缩工具";

        [ObservableProperty]
        private string _ffmpegStatusText = " - FFmpeg状态检测中...";

        [ObservableProperty]
        private string _ffmpegStatusColor = "Gray";

        [ObservableProperty]
        private VideoInfo? _currentVideoInfo;

        [ObservableProperty]
        private bool _isVideoInfoVisible;

        [ObservableProperty]
        private CompressionSettings _compressionSettings = new();

        [ObservableProperty]
        private string _commandText = "请先选择输入文件或文件夹";

        [ObservableProperty]
        private bool _canExecute;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private bool _isProgressVisible;

        [ObservableProperty]
        private string _inputPathText = "";

        [ObservableProperty]
        private string _outputPathText = "";

        [ObservableProperty]
        private int _compressionPercentage = 70;

        [ObservableProperty]
        private int _bitrate = 2000;

        [ObservableProperty]
        private string _selectedCodec = "libx264";

        [ObservableProperty]
        private string _estimatedBitrateText = "请先选择视频文件";

        [ObservableProperty]
        private string _estimatedBitrateColor = "Black";

        [ObservableProperty]
        private bool _isBitrateWarningVisible;

        [ObservableProperty]
        private string _bitrateValueText = "2000k";

        [ObservableProperty]
        private double _bitrateSliderValue = 2000;

        [ObservableProperty]
        private double _bitrateSliderMinimum = 1;

        [ObservableProperty]
        private double _bitrateSliderMaximum = 50000;

        [ObservableProperty]
        private bool _isThemeDark;

        [ObservableProperty]
        private List<CodecOption> _codecOptions = new()
        {
            new CodecOption("H.264 (libx264)", "libx264", "兼容性最好"),
            new CodecOption("H.265 (libx265)", "libx265", "压缩率更高"),
            new CodecOption("VP9 (libvpx-vp9)", "libvpx-vp9", "开源编码")
        };

        [ObservableProperty]
        private CodecOption? _selectedCodecOption;

        #endregion

        #region 命令

        [RelayCommand]
        private async Task SelectFile()
        {
            var file = await _dialogService.OpenFileDialog("选择视频文件", new[]
            {
                new FilePickerFileType("视频文件")
                {
                    Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm" }
                },
                new FilePickerFileType("所有文件")
                {
                    Patterns = new[] { "*.*" }
                }
            });

            if (file != null)
            {
                await ProcessSelectedInput(file.Path.LocalPath);
            }
        }

        [RelayCommand]
        private async Task SelectFolder()
        {
            var folder = await _dialogService.OpenFolderDialog("选择文件夹");
            if (folder != null)
            {
                await ProcessSelectedInput(folder.Path.LocalPath);
            }
        }

        [RelayCommand]
        private async Task SelectOutputFolder()
        {
            var folder = await _dialogService.OpenFolderDialog("选择输出文件夹");
            if (folder != null)
            {
                CompressionSettings.OutputPath = folder.Path.LocalPath;
                OutputPathText = folder.Path.LocalPath;
                UpdateCommand();
            }
        }

        [RelayCommand]
        private async Task Execute()
        {
            if (IsProcessing || !_ffmpegManager.IsFFmpegAvailable) return;

            IsProcessing = true;
            CanExecute = false;
            IsProgressVisible = true;

            try
            {
                await ExecuteFFmpegCommand();
                await _dialogService.ShowMessage("完成", "视频处理完成！");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage("错误", $"执行FFmpeg命令时出错:\n{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                CanExecute = true;
                IsProgressVisible = false;
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            IsThemeDark = !IsThemeDark;
            UpdateTheme();
        }

        [RelayCommand]
        private async Task ShowFFmpegSettings()
        {
            var setupViewModel = new SetupWindowViewModel(_ffmpegManager);
            var setupWindow = new Views.SetupWindow
            {
                DataContext = setupViewModel
            };

            var mainWindow = _dialogService.GetMainWindow();
            if (mainWindow != null)
            {
                await setupWindow.ShowDialog(mainWindow);

                if (setupViewModel.SetupCompleted)
                {
                    await _ffmpegManager.InitializeAsync();
                    UpdateFFmpegStatus();
                    await _dialogService.ShowMessage("成功", "FFmpeg配置已更新！");
                }
            }
        }

        [RelayCommand]
        private async Task RedetectFFmpeg()
        {
            FfmpegStatusText = " - 重新检测中...";
            FfmpegStatusColor = "Gray";

            await _ffmpegManager.InitializeAsync();
            UpdateFFmpegStatus();

            var message = _ffmpegManager.IsFFmpegAvailable
                ? "FFmpeg检测成功！"
                : "未找到FFmpeg，请通过菜单手动配置。";

            await _dialogService.ShowMessage("检测完成", message);
        }

        [RelayCommand]
        private async Task ShowAbout()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            var ffmpegVersion = await _ffmpegManager.GetFFmpegVersion();

            var message = $"FFGUITool v{version}\n" +
                         $"FFmpeg视频压缩工具\n\n" +
                         $"FFmpeg版本: {ffmpegVersion}\n\n" +
                         $"© 2025 FFGUITool\n" +
                         $"Powered by FFmpeg and Avalonia\n" +
                         $"Assembled by brealin";

            await _dialogService.ShowMessage("关于 FFGUITool", message);
        }

        #endregion

        #region 构造函数和初始化

        public MainWindowViewModel() : this(
            new FFmpegManager(),
            new DialogService())
        {
        }

        public MainWindowViewModel(
            FFmpegManager ffmpegManager,
            IDialogService dialogService)
        {
            _ffmpegManager = ffmpegManager;
            _dialogService = dialogService;
            _videoAnalyzer = new VideoAnalyzer(_ffmpegManager);
            _commandBuilder = new CommandBuilder();

            // 设置默认编码器选项
            SelectedCodecOption = CodecOptions[0];

            // 监听属性变化
            PropertyChanged += OnPropertyChanged;

            // 初始化主题
            IsThemeDark = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;
            UpdateTheme();
        }

        protected override async Task OnInitializeAsync()
        {
            await InitializeFFmpeg();
        }

        private async Task InitializeFFmpeg()
        {
            FfmpegStatusText = " - 检测FFmpeg中...";

            await _ffmpegManager.InitializeAsync();

            if (!_ffmpegManager.IsFFmpegAvailable)
            {
                var setupViewModel = new SetupWindowViewModel(_ffmpegManager);
                var setupWindow = new Views.SetupWindow
                {
                    DataContext = setupViewModel
                };

                var mainWindow = _dialogService.GetMainWindow();
                if (mainWindow != null)
                {
                    await setupWindow.ShowDialog(mainWindow);

                    if (setupViewModel.SetupCompleted)
                    {
                        await _ffmpegManager.InitializeAsync();
                    }

                    if (!_ffmpegManager.IsFFmpegAvailable)
                    {
                        await _dialogService.ShowMessage("警告", 
                            "FFmpeg未正确配置，某些功能可能无法使用。\n您可以通过菜单重新配置。");
                    }
                }
            }

            UpdateFFmpegStatus();
        }

        #endregion

        #region 私有方法

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CompressionPercentage):
                    OnCompressionPercentageChanged();
                    break;
                case nameof(Bitrate):
                    OnBitrateChanged();
                    break;
                case nameof(BitrateSliderValue):
                    OnBitrateSliderChanged();
                    break;
                case nameof(SelectedCodecOption):
                    OnCodecChanged();
                    break;
            }
        }

        private async void OnCompressionPercentageChanged()
        {
            CompressionSettings.CompressionPercentage = CompressionPercentage;
            await CalculateOptimalBitrate();
        }

        private void OnBitrateChanged()
        {
            CompressionSettings.Bitrate = Bitrate;
            BitrateSliderValue = Bitrate;
            BitrateValueText = $"{Bitrate}k";
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnBitrateSliderChanged()
        {
            Bitrate = (int)BitrateSliderValue;
        }

        private async void OnCodecChanged()
        {
            if (SelectedCodecOption != null)
            {
                SelectedCodec = SelectedCodecOption.Value;
                CompressionSettings.Codec = SelectedCodec;
                await CalculateOptimalBitrate();
                UpdateCommand();
            }
        }

        private async Task ProcessSelectedInput(string path)
        {
            CompressionSettings.InputPath = path;
            InputPathText = path;

            // 分析视频文件
            if (Path.GetExtension(path).ToLower() is ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm")
            {
                EstimatedBitrateText = "分析视频中...";
                EstimatedBitrateColor = "Blue";

                CurrentVideoInfo = await _videoAnalyzer.AnalyzeVideo(path);

                if (CurrentVideoInfo != null)
                {
                    IsVideoInfoVisible = true;
                    await CalculateOptimalBitrate();
                }
            }
            else
            {
                CurrentVideoInfo = null;
                IsVideoInfoVisible = false;
                EstimatedBitrateText = "非视频文件";
                EstimatedBitrateColor = "Gray";
            }

            UpdateCommand();
        }

        private async Task CalculateOptimalBitrate()
        {
            if (CurrentVideoInfo == null)
            {
                EstimatedBitrateText = "请先选择视频文件";
                return;
            }

            EstimatedBitrateText = "计算中...";

            var targetBitrate = _commandBuilder.CalculateRecommendedBitrate(
                CurrentVideoInfo, 
                CompressionPercentage, 
                SelectedCodec);

            // 动态调整滑动条范围
            UpdateBitrateControlsRange(CurrentVideoInfo.Bitrate);

            // 更新比特率
            Bitrate = targetBitrate;
            BitrateSliderValue = targetBitrate;

            UpdateBitrateWarningAndEstimation();
        }

        private void UpdateBitrateControlsRange(int originalBitrate)
        {
            BitrateSliderMaximum = Math.Max(originalBitrate * 3 / 2, 50000);
            BitrateSliderMinimum = 1;
        }

        private void UpdateBitrateWarningAndEstimation()
        {
            if (CurrentVideoInfo == null) return;

            IsBitrateWarningVisible = Bitrate > CurrentVideoInfo.Bitrate;

            var estimatedSize = _commandBuilder.CalculateEstimatedFileSize(Bitrate, CurrentVideoInfo.Duration);
            var originalSizeMB = CurrentVideoInfo.FileSize / 1024.0 / 1024.0;
            var estimatedSizeMB = estimatedSize / 1024.0 / 1024.0;

            if (estimatedSizeMB > originalSizeMB)
            {
                var increaseRatio = (estimatedSizeMB / originalSizeMB - 1) * 100;
                EstimatedBitrateText = $"{Bitrate}k (预估: {estimatedSizeMB:F1}MB, 增大: {increaseRatio:F1}%)";
                EstimatedBitrateColor = "Orange";
            }
            else
            {
                var compressionRatio = (1 - estimatedSizeMB / originalSizeMB) * 100;
                EstimatedBitrateText = $"{Bitrate}k (预估: {estimatedSizeMB:F1}MB, 压缩: {compressionRatio:F1}%)";
                EstimatedBitrateColor = "Green";
            }
        }

        private void UpdateCommand()
        {
            CompressionSettings.Bitrate = Bitrate;
            var command = _commandBuilder.BuildCommand(CompressionSettings, CurrentVideoInfo);
            CommandText = command.BuildCommand();
            
            CanExecute = CompressionSettings.IsValid && _ffmpegManager.IsFFmpegAvailable;
        }

        private void UpdateFFmpegStatus()
        {
            if (_ffmpegManager.IsFFmpegAvailable)
            {
                Title = "FFmpeg 视频压缩工具 - FFmpeg已就绪";
                FfmpegStatusText = " - FFmpeg已就绪";
                FfmpegStatusColor = "Green";
            }
            else
            {
                Title = "FFmpeg 视频压缩工具 - FFmpeg未配置";
                FfmpegStatusText = " - FFmpeg未配置";
                FfmpegStatusColor = "Red";
            }
        }

        private async Task ExecuteFFmpegCommand()
        {
            if (!_ffmpegManager.IsFFmpegAvailable)
            {
                throw new Exception("FFmpeg未配置或不可用，请先配置FFmpeg路径");
            }

            var command = _commandBuilder.BuildCommand(CompressionSettings);
            var arguments = command.BuildCommand().Replace("ffmpeg ", "");

            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegManager.FFmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg执行失败，退出代码: {process.ExitCode}\n错误信息: {output}");
            }
        }

        private void UpdateTheme()
        {
            if (Application.Current == null) return;

            // Update the application theme variant
            Application.Current.RequestedThemeVariant = IsThemeDark ? ThemeVariant.Dark : ThemeVariant.Light;

            // Update dynamic resources based on theme
            var resources = Application.Current.Resources;
            if (IsThemeDark)
            {
                resources["CardBackground"] = resources["CardBackgroundDark"];
                resources["CardBorderBrush"] = resources["CardBorderBrushDark"];
                resources["HelpButtonBackground"] = resources["HelpButtonBackgroundDark"];
                resources["HelpButtonHoverBackground"] = resources["HelpButtonHoverBackgroundDark"];
                resources["CommandBoxBackground"] = resources["CommandBoxBackgroundDark"];
                resources["CommandBoxForeground"] = resources["CommandBoxForegroundDark"];
                resources["VideoInfoBackground"] = resources["VideoInfoBackgroundDark"];
                resources["VideoInfoBorder"] = resources["VideoInfoBorderDark"];
                resources["TooltipBackground"] = resources["TooltipBackgroundDark"];
                resources["TooltipBorder"] = resources["TooltipBorderDark"];
            }
            else
            {
                resources["CardBackground"] = resources["CardBackgroundLight"];
                resources["CardBorderBrush"] = resources["CardBorderBrushLight"];
                resources["HelpButtonBackground"] = resources["HelpButtonBackgroundLight"];
                resources["HelpButtonHoverBackground"] = resources["HelpButtonHoverBackgroundLight"];
                resources["CommandBoxBackground"] = resources["CommandBoxBackgroundLight"];
                resources["CommandBoxForeground"] = resources["CommandBoxForegroundLight"];
                resources["VideoInfoBackground"] = resources["VideoInfoBackgroundLight"];
                resources["VideoInfoBorder"] = resources["VideoInfoBorderLight"];
                resources["TooltipBackground"] = resources["TooltipBackgroundLight"];
                resources["TooltipBorder"] = resources["TooltipBorderLight"];
            }
        }

        #endregion
    }
}