using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using FFGUITool.Services.Interfaces;
using FFGUITool.Models;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace FFGUITool.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IFFmpegService _ffmpegService;
        private readonly IMediaAnalyzer _mediaAnalyzer;
        private readonly IVideoProcessor _videoProcessor;

        // FFmpeg状态相关
        private string _ffmpegStatusText = "正在检测FFmpeg...";
        private bool _isFFmpegAvailable;
        private bool _isDarkTheme;
        private bool _isSystemTheme = true;
        private bool _isLightTheme;

        // 文件相关
        private string _inputPath = "";
        private string _outputPath = "";
        private bool _hasVideoInfo;
        private bool _isFolder;

        // 视频信息
        private string _originalFileSize = "-";
        private string _videoDuration = "-";
        private string _originalBitrate = "-";
        private string _videoResolution = "-";
        private string _videoFramerate = "-";

        // 压缩设置
        private int _compressionRatio = 70;
        private double _targetBitrate = 2000;
        private int _selectedCodecIndex = 0;
        private string _bitrateDisplayText = "2000k";
        private bool _showBitrateWarning = false;
        private string _estimatedResultText = "请先选择视频文件";

        // 命令和进度
        private string _generatedCommand = "";
        private bool _isProcessing = false;
        private double _processingProgress = 0;
        private bool _canExecute = false;

        public MainWindowViewModel(
            IFFmpegService ffmpegService,
            IMediaAnalyzer mediaAnalyzer,
            IVideoProcessor videoProcessor)
        {
            _ffmpegService = ffmpegService;
            _mediaAnalyzer = mediaAnalyzer;
            _videoProcessor = videoProcessor;

            // 初始化所有命令
            InitializeCommand = new RelayCommand(async _ => await InitializeAsync());
            SelectInputFileCommand = new RelayCommand(async _ => await SelectInputFileAsync());
            SelectInputFolderCommand = new RelayCommand(async _ => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async _ => await SelectOutputFolderAsync());
            ExecuteCompressionCommand = new RelayCommand(async _ => await ExecuteCompressionAsync(), _ => CanExecute);
            UpdateCodecCommand = new RelayCommand(codec => UpdateCodec(codec?.ToString()));
            UpdateBitrateCommand = new RelayCommand(bitrate => UpdateBitrate(Convert.ToDouble(bitrate)));
            SetThemeCommand = new RelayCommand(theme => SetTheme(theme?.ToString()));
            ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
            OpenFFmpegSettingsCommand = new RelayCommand(_ => OpenFFmpegSettings());
            RedetectFFmpegCommand = new RelayCommand(async _ => await RedetectFFmpegAsync());
            ShowAboutCommand = new RelayCommand(_ => ShowAbout());
            ExitCommand = new RelayCommand(_ => Exit());
        }

        #region Properties

        // FFmpeg状态
        public string FFmpegStatusText
        {
            get => _ffmpegStatusText;
            set => SetProperty(ref _ffmpegStatusText, value);
        }

        public bool IsFFmpegAvailable
        {
            get => _isFFmpegAvailable;
            set => SetProperty(ref _isFFmpegAvailable, value);
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        public bool IsSystemTheme
        {
            get => _isSystemTheme;
            set => SetProperty(ref _isSystemTheme, value);
        }

        public bool IsLightTheme
        {
            get => _isLightTheme;
            set => SetProperty(ref _isLightTheme, value);
        }

        // 文件路径
        public string InputPath
        {
            get => _inputPath;
            set => SetProperty(ref _inputPath, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        public bool HasVideoInfo
        {
            get => _hasVideoInfo;
            set => SetProperty(ref _hasVideoInfo, value);
        }

        // 视频信息
        public string OriginalFileSize
        {
            get => _originalFileSize;
            set => SetProperty(ref _originalFileSize, value);
        }

        public string VideoDuration
        {
            get => _videoDuration;
            set => SetProperty(ref _videoDuration, value);
        }

        public string OriginalBitrate
        {
            get => _originalBitrate;
            set => SetProperty(ref _originalBitrate, value);
        }

        public string VideoResolution
        {
            get => _videoResolution;
            set => SetProperty(ref _videoResolution, value);
        }

        public string VideoFramerate
        {
            get => _videoFramerate;
            set => SetProperty(ref _videoFramerate, value);
        }

        // 压缩设置
        public int CompressionRatio
        {
            get => _compressionRatio;
            set
            {
                if (SetProperty(ref _compressionRatio, value))
                {
                    UpdateEstimatedResult();
                    UpdateFFmpegCommand();
                }
            }
        }

        public double TargetBitrate
        {
            get => _targetBitrate;
            set
            {
                if (SetProperty(ref _targetBitrate, value))
                {
                    BitrateDisplayText = $"{value:F0}k";
                    UpdateBitrateWarning();
                    UpdateEstimatedResult();
                    UpdateFFmpegCommand();
                }
            }
        }

        public int SelectedCodecIndex
        {
            get => _selectedCodecIndex;
            set
            {
                if (SetProperty(ref _selectedCodecIndex, value))
                {
                    UpdateFFmpegCommand();
                }
            }
        }

        public string BitrateDisplayText
        {
            get => _bitrateDisplayText;
            set => SetProperty(ref _bitrateDisplayText, value);
        }

        public bool ShowBitrateWarning
        {
            get => _showBitrateWarning;
            set => SetProperty(ref _showBitrateWarning, value);
        }

        public string EstimatedResultText
        {
            get => _estimatedResultText;
            set => SetProperty(ref _estimatedResultText, value);
        }

        // 命令和进度
        public string GeneratedCommand
        {
            get => _generatedCommand;
            set => SetProperty(ref _generatedCommand, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public double ProcessingProgress
        {
            get => _processingProgress;
            set => SetProperty(ref _processingProgress, value);
        }

        public bool CanExecute
        {
            get => _canExecute;
            set => SetProperty(ref _canExecute, value);
        }

        #endregion

        #region Commands

        public ICommand InitializeCommand { get; }
        public ICommand SelectInputFileCommand { get; }
        public ICommand SelectInputFolderCommand { get; }
        public ICommand SelectOutputFolderCommand { get; }
        public ICommand ExecuteCompressionCommand { get; }
        public ICommand UpdateCodecCommand { get; }
        public ICommand UpdateBitrateCommand { get; }
        public ICommand SetThemeCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand OpenFFmpegSettingsCommand { get; }
        public ICommand RedetectFFmpegCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ExitCommand { get; }

        #endregion

        #region Methods

        private async Task InitializeAsync()
        {
            FFmpegStatusText = "正在检测FFmpeg...";
            
            var result = await _ffmpegService.InitializeAsync();
            IsFFmpegAvailable = result;

            if (result)
            {
                var version = await _ffmpegService.GetVersionAsync();
                FFmpegStatusText = $" - FFmpeg已就绪 ({version})";
            }
            else
            {
                FFmpegStatusText = " - FFmpeg未配置";
            }

            UpdateCanExecute();
        }

        private async Task SelectInputFileAsync()
        {
            // 这个方法将由View通过文件选择对话框调用SetInputFile
            // 保持为空实现，实际逻辑在SetInputFile中
            await Task.CompletedTask;
        }

        private async Task SelectInputFolderAsync()
        {
            // 这个方法将由View通过文件夹选择对话框调用SetInputFolder
            // 保持为空实现，实际逻辑在SetInputFolder中
            await Task.CompletedTask;
        }

        private async Task SelectOutputFolderAsync()
        {
            // 这个方法将由View通过文件夹选择对话框调用SetOutputFolder
            // 保持为空实现，实际逻辑在SetOutputFolder中
            await Task.CompletedTask;
        }

        public async Task SetInputFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            InputPath = filePath;
            _isFolder = false;
            
            try
            {
                // 分析视频文件
                var mediaInfo = await _mediaAnalyzer.AnalyzeAsync(filePath);
                
                // 更新视频信息
                var fileInfo = new FileInfo(filePath);
                OriginalFileSize = FormatFileSize(fileInfo.Length);
                VideoDuration = FormatDuration(mediaInfo.Duration);
                OriginalBitrate = $"{mediaInfo.Bitrate / 1000:F0} kbps";
                VideoResolution = $"{mediaInfo.Width}×{mediaInfo.Height}";
                VideoFramerate = $"{mediaInfo.FrameRate:F1} fps";
                
                HasVideoInfo = true;
                
                // 设置默认输出路径
                if (string.IsNullOrEmpty(OutputPath))
                {
                    OutputPath = Path.GetDirectoryName(filePath) ?? "";
                }
                
                UpdateBitrateWarning();
                UpdateEstimatedResult();
                UpdateFFmpegCommand();
                UpdateCanExecute();
            }
            catch (Exception ex)
            {
                // 处理错误
                HasVideoInfo = false;
                FFmpegStatusText = $" - 分析视频失败: {ex.Message}";
            }
        }

        public void SetInputFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            InputPath = folderPath;
            _isFolder = true;
            HasVideoInfo = false;
            
            // 统计文件夹中的视频文件
            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
            var videoFiles = Directory.GetFiles(folderPath)
                .Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();
            
            if (videoFiles.Length > 0)
            {
                EstimatedResultText = $"找到 {videoFiles.Length} 个视频文件";
                
                // 设置默认输出路径
                if (string.IsNullOrEmpty(OutputPath))
                {
                    OutputPath = folderPath;
                }
                
                UpdateFFmpegCommand();
                UpdateCanExecute();
            }
            else
            {
                EstimatedResultText = "文件夹中没有找到视频文件";
            }
        }

        public void SetOutputFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                OutputPath = folderPath;
                UpdateFFmpegCommand();
                UpdateCanExecute();
            }
        }

        private void UpdateCodec(string codecTag)
        {
            // 更新编码器选择
            UpdateFFmpegCommand();
        }

        private void UpdateBitrate(double bitrate)
        {
            TargetBitrate = bitrate;
        }

        private void UpdateBitrateWarning()
        {
            if (HasVideoInfo && !string.IsNullOrEmpty(OriginalBitrate))
            {
                // 从原始比特率字符串中提取数值
                if (double.TryParse(OriginalBitrate.Replace(" kbps", ""), out double originalKbps))
                {
                    ShowBitrateWarning = TargetBitrate > originalKbps;
                }
            }
        }

        private void UpdateEstimatedResult()
        {
            if (!HasVideoInfo && !_isFolder)
            {
                EstimatedResultText = "请先选择视频文件";
                return;
            }

            if (_isFolder)
            {
                var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
                var videoFiles = Directory.GetFiles(InputPath)
                    .Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToArray();
                EstimatedResultText = $"批量处理 {videoFiles.Length} 个文件，比特率: {TargetBitrate:F0} kbps";
            }
            else
            {
                var compressionPercent = CompressionRatio / 100.0;
                var estimatedSizeText = $"预计压缩到原文件的 {CompressionRatio}%";
                EstimatedResultText = $"{estimatedSizeText}，比特率: {TargetBitrate:F0} kbps";
            }
        }

        private void UpdateFFmpegCommand()
        {
            if (string.IsNullOrEmpty(InputPath) || string.IsNullOrEmpty(OutputPath))
            {
                GeneratedCommand = "请先选择输入文件和输出文件夹";
                return;
            }

            var codecMap = new[] { "libx264", "libx265", "libvpx-vp9" };
            var codec = codecMap[Math.Min(SelectedCodecIndex, codecMap.Length - 1)];
            
            if (_isFolder)
            {
                GeneratedCommand = $"批量处理模式: ffmpeg -i [输入文件] -c:v {codec} -b:v {TargetBitrate}k -c:a copy [输出文件]";
            }
            else
            {
                var inputFileName = Path.GetFileNameWithoutExtension(InputPath);
                var outputFileName = $"{inputFileName}_compressed.mp4";
                var outputFilePath = Path.Combine(OutputPath, outputFileName);

                GeneratedCommand = $"ffmpeg -i \"{InputPath}\" -c:v {codec} -b:v {TargetBitrate}k -c:a copy \"{outputFilePath}\"";
            }
        }

        private void UpdateCanExecute()
        {
            CanExecute = IsFFmpegAvailable && 
                        !string.IsNullOrEmpty(InputPath) && 
                        !string.IsNullOrEmpty(OutputPath) && 
                        !IsProcessing;
        }

        private async Task ExecuteCompressionAsync()
        {
            if (!CanExecute) return;

            try
            {
                IsProcessing = true;
                ProcessingProgress = 0;
                UpdateCanExecute();

                if (_isFolder)
                {
                    await ProcessFolderAsync();
                }
                else
                {
                    await ProcessSingleFileAsync();
                }
                
                FFmpegStatusText = " - 压缩完成";
            }
            catch (Exception ex)
            {
                FFmpegStatusText = $" - 压缩失败: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                ProcessingProgress = 0;
                UpdateCanExecute();
            }
        }

        private async Task ProcessSingleFileAsync()
        {
            var compressionSettings = new VideoCompressionSettings
            {
                InputPath = InputPath,
                OutputPath = OutputPath,
                TargetBitrate = (int)TargetBitrate,
                Codec = GetSelectedCodec(),
                CompressionRatio = CompressionRatio / 100.0
            };

            var progress = new Progress<double>(value => ProcessingProgress = value * 100);
            
            await _videoProcessor.CompressVideoAsync(compressionSettings, progress);
        }

        private async Task ProcessFolderAsync()
        {
            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
            var videoFiles = Directory.GetFiles(InputPath)
                .Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();

            var totalFiles = videoFiles.Length;
            var processedFiles = 0;

            foreach (var file in videoFiles)
            {
                var compressionSettings = new VideoCompressionSettings
                {
                    InputPath = file,
                    OutputPath = OutputPath,
                    TargetBitrate = (int)TargetBitrate,
                    Codec = GetSelectedCodec(),
                    CompressionRatio = CompressionRatio / 100.0
                };

                var fileProgress = new Progress<double>(value =>
                {
                    var overallProgress = (processedFiles + value) / totalFiles * 100;
                    ProcessingProgress = overallProgress;
                });

                await _videoProcessor.CompressVideoAsync(compressionSettings, fileProgress);
                processedFiles++;
            }
        }

        private string GetSelectedCodec()
        {
            return SelectedCodecIndex switch
            {
                1 => "libx265",
                2 => "libvpx-vp9",
                _ => "libx264"
            };
        }

        private void SetTheme(string theme)
        {
            IsSystemTheme = theme?.ToLower() == "system";
            IsLightTheme = theme?.ToLower() == "light";
            IsDarkTheme = theme?.ToLower() == "dark";
            
            var app = Avalonia.Application.Current;
            if (app != null)
            {
                app.RequestedThemeVariant = theme?.ToLower() switch
                {
                    "light" => Avalonia.Styling.ThemeVariant.Light,
                    "dark" => Avalonia.Styling.ThemeVariant.Dark,
                    _ => Avalonia.Styling.ThemeVariant.Default
                };
            }
        }

        private void ToggleTheme()
        {
            if (IsDarkTheme)
            {
                SetTheme("light");
            }
            else
            {
                SetTheme("dark");
            }
        }

        private void OpenFFmpegSettings()
        {
            // TODO: 打开 FFmpeg 设置对话框
            FFmpegStatusText = " - FFmpeg设置功能开发中...";
        }

        private async Task RedetectFFmpegAsync()
        {
            await InitializeAsync();
        }

        private void ShowAbout()
        {
            // TODO: 显示关于对话框
            FFmpegStatusText = " - FFGUITool v1.0 - FFmpeg视频压缩工具";
        }

        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            else
                return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        #endregion
    }

    // 辅助类
    public class VideoCompressionSettings
    {
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public int TargetBitrate { get; set; }
        public string Codec { get; set; } = "";
        public double CompressionRatio { get; set; }
    }
}