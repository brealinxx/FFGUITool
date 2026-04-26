using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
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
        private string _title = LocalizationService.T("App.Title");

        [ObservableProperty]
        private string _ffmpegStatusText = LocalizationService.T("Status.Detecting");

        [ObservableProperty]
        private string _ffmpegStatusColor = "Gray";

        [ObservableProperty]
        private VideoInfo? _currentVideoInfo;

        [ObservableProperty]
        private bool _isVideoInfoVisible;

        [ObservableProperty]
        private CompressionSettings _compressionSettings = new();

        [ObservableProperty]
        private string _commandText = LocalizationService.T("Command.SelectInput");

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
        private string _estimatedBitrateText = LocalizationService.T("Estimate.SelectVideo");

        [ObservableProperty]
        private string _estimatedBitrateColor = "Black";

        [ObservableProperty]
        private bool _isBitrateWarningVisible;

        [ObservableProperty]
        private string _bitrateValueText = "2000k";

        [ObservableProperty]
        private string _bitrateSelectionText = "";

        [ObservableProperty]
        private double _bitrateSliderValue = 2000;

        [ObservableProperty]
        private double _bitrateSliderMinimum = 1;

        [ObservableProperty]
        private double _bitrateSliderMaximum = 50000;

        [ObservableProperty]
        private ThemeVariant _currentTheme = ThemeVariant.Default;

        [ObservableProperty]
        private bool _isThemeDark;

        [ObservableProperty]
        private bool _isChineseLanguage = LocalizationService.CurrentLanguage == "zh-CN";

        [ObservableProperty]
        private bool _isEnglishLanguage = LocalizationService.CurrentLanguage == "en-US";

        [ObservableProperty]
        private List<CodecOption> _codecOptions = new()
        {
            new CodecOption("H.264 (libx264)", "libx264", LocalizationService.T("Codec.H264.Desc")),
            new CodecOption("H.265 (libx265)", "libx265", LocalizationService.T("Codec.H265.Desc")),
            new CodecOption("VP9 (libvpx-vp9)", "libvpx-vp9", LocalizationService.T("Codec.VP9.Desc"))
        };

        [ObservableProperty]
        private CodecOption? _selectedCodecOption;

        #endregion

        #region 命令

        [RelayCommand]
        private async Task SelectFile()
        {
            var file = await _dialogService.OpenFileDialog(LocalizationService.T("Picker.SelectVideo"), new[]
            {
                new FilePickerFileType(LocalizationService.T("Picker.VideoFiles"))
                {
                    Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm" }
                },
                new FilePickerFileType(LocalizationService.T("Picker.AllFiles"))
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
            var folder = await _dialogService.OpenFolderDialog(LocalizationService.T("Picker.SelectFolder"));
            if (folder != null)
            {
                await ProcessSelectedInput(folder.Path.LocalPath);
            }
        }

        [RelayCommand]
        private async Task SelectOutputFolder()
        {
            var folder = await _dialogService.OpenFolderDialog(LocalizationService.T("Picker.SelectOutput"));
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
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Done"), LocalizationService.T("Dialog.VideoComplete"));
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.Format("Dialog.ExecuteError", ex.Message));
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
            CurrentTheme = IsThemeDark ? ThemeVariant.Dark : ThemeVariant.Light;
            Application.Current!.RequestedThemeVariant = CurrentTheme;
        }

        [RelayCommand]
        private void SetLanguage(string languageCode)
        {
            LocalizationService.SetLanguage(languageCode);
        }

        [RelayCommand]
        private async Task CopyCommandText()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.Clipboard == null)
            {
                return;
            }

            await desktop.MainWindow.Clipboard.SetTextAsync(CommandText);
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
                    await _dialogService.ShowMessage(LocalizationService.T("Dialog.Success"), LocalizationService.T("Dialog.FFmpegConfigUpdated"));
                }
            }
        }

        [RelayCommand]
        private async Task RedetectFFmpeg()
        {
            FfmpegStatusText = LocalizationService.T("Status.Redetecting");
            FfmpegStatusColor = "Gray";

            await _ffmpegManager.InitializeAsync();
            UpdateFFmpegStatus();

            var message = _ffmpegManager.IsFFmpegAvailable
                ? LocalizationService.T("Dialog.FFmpegDetected")
                : LocalizationService.T("Dialog.FFmpegMissing");

            await _dialogService.ShowMessage(LocalizationService.T("Dialog.DetectComplete"), message);
        }

        [RelayCommand]
        private async Task ShowAbout()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            var ffmpegVersion = await _ffmpegManager.GetFFmpegVersion();

            var message = LocalizationService.Format("Dialog.AboutMessage", version, ffmpegVersion);

            await _dialogService.ShowMessage(LocalizationService.T("Dialog.AboutTitle"), message);
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
            UpdateBitrateTexts();

            // 监听属性变化
            PropertyChanged += OnPropertyChanged;
            LocalizationService.LanguageChanged += OnLanguageChanged;
        }

        protected override async Task OnInitializeAsync()
        {
            await InitializeFFmpeg();
        }

        private async Task InitializeFFmpeg()
        {
            FfmpegStatusText = LocalizationService.T("Status.Checking");

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
                        await _dialogService.ShowMessage(
                            LocalizationService.T("Dialog.Warning"),
                            LocalizationService.T("Dialog.FFmpegWarn"));
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
                case nameof(IsThemeDark):
                    // 当IsThemeDark改变时不需要额外处理，ToggleTheme命令会处理
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
            UpdateBitrateTexts();
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
                EstimatedBitrateText = LocalizationService.T("Estimate.Analyzing");
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
                EstimatedBitrateText = LocalizationService.T("Estimate.NonVideo");
                EstimatedBitrateColor = "Gray";
            }

            UpdateCommand();
        }

        private async Task CalculateOptimalBitrate()
        {
            if (CurrentVideoInfo == null)
            {
                EstimatedBitrateText = LocalizationService.T("Estimate.SelectVideo");
                return;
            }

            EstimatedBitrateText = LocalizationService.T("Estimate.Calculating");

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
                EstimatedBitrateText = LocalizationService.Format(
                    "Estimate.Current",
                    Bitrate,
                    estimatedSizeMB,
                    LocalizationService.T("Estimate.Increase"),
                    increaseRatio);
                EstimatedBitrateColor = "Orange";
            }
            else
            {
                var compressionRatio = (1 - estimatedSizeMB / originalSizeMB) * 100;
                EstimatedBitrateText = LocalizationService.Format(
                    "Estimate.Current",
                    Bitrate,
                    estimatedSizeMB,
                    LocalizationService.T("Estimate.Compress"),
                    compressionRatio);
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
                Title = LocalizationService.T("App.Title.Ready");
                FfmpegStatusText = LocalizationService.T("Status.Ready");
                FfmpegStatusColor = "Green";
            }
            else
            {
                Title = LocalizationService.T("App.Title.NotConfigured");
                FfmpegStatusText = LocalizationService.T("Status.NotConfigured");
                FfmpegStatusColor = "Red";
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            IsChineseLanguage = LocalizationService.CurrentLanguage == "zh-CN";
            IsEnglishLanguage = LocalizationService.CurrentLanguage == "en-US";
            UpdateCodecOptions();
            UpdateBitrateTexts();
            UpdateFFmpegStatus();

            if (CurrentVideoInfo == null && string.IsNullOrWhiteSpace(CompressionSettings.InputPath))
            {
                EstimatedBitrateText = LocalizationService.T("Estimate.SelectVideo");
                CommandText = LocalizationService.T("Command.SelectInput");
            }
            else
            {
                UpdateBitrateWarningAndEstimation();
                UpdateCommand();
            }
        }

        private void UpdateCodecOptions()
        {
            var selectedValue = SelectedCodecOption?.Value ?? SelectedCodec;
            CodecOptions = new List<CodecOption>
            {
                new("H.264 (libx264)", "libx264", LocalizationService.T("Codec.H264.Desc")),
                new("H.265 (libx265)", "libx265", LocalizationService.T("Codec.H265.Desc")),
                new("VP9 (libvpx-vp9)", "libvpx-vp9", LocalizationService.T("Codec.VP9.Desc"))
            };
            SelectedCodecOption = CodecOptions.Find(option => option.Value == selectedValue) ?? CodecOptions[0];
        }

        private void UpdateBitrateTexts()
        {
            BitrateValueText = $"{Bitrate}k";
            BitrateSelectionText = LocalizationService.Format("Main.CurrentSelection", BitrateValueText);
        }

        private async Task ExecuteFFmpegCommand()
        {
            if (!_ffmpegManager.IsFFmpegAvailable)
            {
                throw new Exception(LocalizationService.T("Status.NotConfigured"));
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

        #endregion

        public override void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            base.Dispose();
        }
    }
}
