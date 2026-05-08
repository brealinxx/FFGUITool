using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private bool _isSyncingCompressionValues;
        private bool _isSyncingConversionToggles;
        private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
        private static readonly string[] AudioExtensions = { ".mp3", ".aac", ".m4a", ".wav", ".flac", ".ogg", ".wma" };

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
        private double _targetSizeMB;

        [ObservableProperty]
        private double _targetSizeSliderValue;

        [ObservableProperty]
        private double _targetSizeSliderMinimum = 1;

        [ObservableProperty]
        private double _targetSizeSliderMaximum = 1;

        [ObservableProperty]
        private string _targetSizeValueText = "0 MB";

        [ObservableProperty]
        private string _targetSizeSelectionText = "";

        [ObservableProperty]
        private bool _isAdvancedMode;

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

        [ObservableProperty]
        private List<CompressionPresetOption> _compressionPresetOptions = new()
        {
            new CompressionPresetOption("无", "none", 0, "手动设置目标大小"),
            new CompressionPresetOption("微信/QQ发送", "chat", 100, "能发出去，兼顾自动播放"),
            new CompressionPresetOption("邮箱附件", "email", 25, "适合邮件附件"),
            new CompressionPresetOption("网页上传", "web", 0, "适合上传平台，保持清晰度"),
            new CompressionPresetOption("极限压缩", "extreme", 0, "最大限度压缩体积，画质较低")
        };

        [ObservableProperty]
        private CompressionPresetOption? _selectedCompressionPresetOption;

        [ObservableProperty]
        private bool _hasSelectedInput;

        [ObservableProperty]
        private bool _isSelectedAudioInput;

        [ObservableProperty]
        private bool _isBatchMode;

        [ObservableProperty]
        private int _batchFileCount;

        [ObservableProperty]
        private string _batchModeText = "";

        [ObservableProperty]
        private bool _canUseVideoConversionTools;

        [ObservableProperty]
        private bool _enableFormatConversion;

        [ObservableProperty]
        private bool _enableAudioConversion;

        [ObservableProperty]
        private bool _enableResolutionConversion;

        [ObservableProperty]
        private bool _isFormatConversionOptionsVisible;

        [ObservableProperty]
        private bool _isAudioConversionOptionsVisible;

        [ObservableProperty]
        private bool _isResolutionConversionOptionsVisible;

        [ObservableProperty]
        private string _conversionModeHint = "";

        [ObservableProperty]
        private List<CodecOption> _videoFormatOptions = new()
        {
            new CodecOption("MP4", "mp4", "兼容性最好"),
            new CodecOption("MKV", "mkv", "多轨封装"),
            new CodecOption("WebM", "webm", "网页友好"),
            new CodecOption("MOV", "mov", "Apple/剪辑软件"),
            new CodecOption("AVI", "avi", "旧设备兼容"),
            new CodecOption("GIF", "gif", "视频转动图")
        };

        [ObservableProperty]
        private CodecOption? _selectedVideoFormatOption;

        [ObservableProperty]
        private List<CodecOption> _audioFormatOptions = new()
        {
            new CodecOption("MP3", "mp3", "通用音频"),
            new CodecOption("AAC", "aac", "体积小"),
            new CodecOption("M4A", "m4a", "Apple/移动设备"),
            new CodecOption("WAV", "wav", "无压缩"),
            new CodecOption("FLAC", "flac", "无损压缩"),
            new CodecOption("OGG", "ogg", "开源音频")
        };

        [ObservableProperty]
        private CodecOption? _selectedAudioFormatOption;

        [ObservableProperty]
        private List<CodecOption> _resolutionOptions = new()
        {
            new CodecOption("2160p", "2160", "4K"),
            new CodecOption("1080p", "1080", "全高清"),
            new CodecOption("720p", "720", "高清"),
            new CodecOption("480p", "480", "小体积"),
            new CodecOption("360p", "360", "极小体积")
        };

        [ObservableProperty]
        private CodecOption? _selectedResolutionOption;

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
                new FilePickerFileType(LocalizationService.T("Picker.AudioFiles"))
                {
                    Patterns = new[] { "*.mp3", "*.aac", "*.m4a", "*.wav", "*.flac", "*.ogg", "*.wma" }
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
                var summary = await ExecuteFFmpegCommand();
                var message = BuildCompletionMessage(summary);
                SystemNotificationService.Show("转换完成", message);
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Done"), message);
            }
            catch (FFmpegExecutionException ex)
            {
                var message = BuildFailureMessage(ex);
                SystemNotificationService.Show("转换失败", message, isError: true);
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), message);
            }
            catch (Exception ex)
            {
                var message = LocalizationService.Format("Dialog.ExecuteError", ex.Message);
                SystemNotificationService.Show("转换失败", message, isError: true);
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), message);
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

        [RelayCommand]
        private async Task ShowCompressionPresetHelp()
        {
            await _dialogService.ShowMessage(
                LocalizationService.T("Preset.HelpTitle"),
                LocalizationService.T("Preset.HelpMessage"));
        }

        [RelayCommand]
        private async Task ShowConversionHelp()
        {
            await _dialogService.ShowMessage(
                LocalizationService.T("Conversion.HelpTitle"),
                LocalizationService.T("Conversion.HelpMessage"));
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
            CompressionPresetOptions = CreateCompressionPresetOptions();
            SelectedCompressionPresetOption = CompressionPresetOptions[0];
            SelectedVideoFormatOption = VideoFormatOptions[0];
            SelectedAudioFormatOption = AudioFormatOptions[0];
            SelectedResolutionOption = ResolutionOptions[2];
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
                case nameof(TargetSizeMB):
                    OnTargetSizeChanged();
                    break;
                case nameof(TargetSizeSliderValue):
                    OnTargetSizeSliderChanged();
                    break;
                case nameof(SelectedCompressionPresetOption):
                    OnCompressionPresetChanged();
                    break;
                case nameof(EnableFormatConversion):
                case nameof(EnableAudioConversion):
                case nameof(EnableResolutionConversion):
                    OnConversionToggleChanged(e.PropertyName);
                    break;
                case nameof(SelectedVideoFormatOption):
                    OnVideoFormatChanged();
                    break;
                case nameof(SelectedAudioFormatOption):
                    OnAudioFormatChanged();
                    break;
                case nameof(SelectedResolutionOption):
                    OnResolutionChanged();
                    break;
                case nameof(IsAdvancedMode):
                    UpdateConversionOptionVisibility();
                    break;
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

        private void OnCompressionPercentageChanged()
        {
            CompressionSettings.CompressionPercentage = CompressionPercentage;
            CalculateOptimalBitrate();
        }

        private void OnTargetSizeChanged()
        {
            if (_isSyncingCompressionValues)
            {
                return;
            }

            if (CurrentVideoInfo != null)
            {
                TargetSizeMB = ClampTargetSize(TargetSizeMB);
            }

            CompressionSettings.TargetSizeMB = TargetSizeMB;
            TargetSizeSliderValue = TargetSizeMB;
            CalculateBitrateFromTargetSize();
            UpdateTargetSizeTexts();
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        private void OnTargetSizeSliderChanged()
        {
            TargetSizeMB = TargetSizeSliderValue;
        }

        private void OnCompressionPresetChanged()
        {
            if (SelectedCompressionPresetOption == null)
            {
                return;
            }

            ApplyCompressionPreset(SelectedCompressionPresetOption);

            if (CurrentVideoInfo != null && SelectedCompressionPresetOption.TargetSizeMB > 0)
            {
                TargetSizeMB = ClampTargetSize(SelectedCompressionPresetOption.TargetSizeMB);
            }
            else if (CurrentVideoInfo != null && SelectedCompressionPresetOption.Value == "extreme")
            {
                TargetSizeMB = TargetSizeSliderMinimum;
            }

            CalculateBitrateFromTargetSize();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnConversionToggleChanged(string? changedPropertyName)
        {
            if (_isSyncingConversionToggles)
            {
                return;
            }

            _isSyncingConversionToggles = true;
            if (changedPropertyName == nameof(EnableAudioConversion) && EnableAudioConversion)
            {
                EnableFormatConversion = false;
                EnableResolutionConversion = false;
            }
            else if ((changedPropertyName == nameof(EnableFormatConversion) || changedPropertyName == nameof(EnableResolutionConversion))
                     && (EnableFormatConversion || EnableResolutionConversion))
            {
                EnableAudioConversion = false;
            }

            if (!CanUseVideoConversionTools)
            {
                EnableFormatConversion = false;
                EnableResolutionConversion = false;
            }

            _isSyncingConversionToggles = false;
            CompressionSettings.EnableFormatConversion = EnableFormatConversion;
            CompressionSettings.EnableAudioConversion = EnableAudioConversion;
            CompressionSettings.EnableResolutionConversion = EnableResolutionConversion;
            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            ApplySelectedConversionOptions();
            RefreshBatchModeSummary();
            UpdateCommand();
        }

        private void OnVideoFormatChanged()
        {
            if (SelectedVideoFormatOption != null)
            {
                if (SelectedVideoFormatOption.Value == "gif")
                {
                    _isSyncingConversionToggles = true;
                    EnableAudioConversion = false;
                    _isSyncingConversionToggles = false;
                }

                CompressionSettings.OutputFormat = SelectedVideoFormatOption.Value;
                UpdateConversionHint();
                UpdateCommand();
            }
        }

        private void OnAudioFormatChanged()
        {
            if (SelectedAudioFormatOption != null)
            {
                CompressionSettings.AudioOutputFormat = SelectedAudioFormatOption.Value;
                UpdateCommand();
            }
        }

        private void OnResolutionChanged()
        {
            if (SelectedResolutionOption != null && int.TryParse(SelectedResolutionOption.Value, out var height))
            {
                CompressionSettings.ResolutionHeight = height;
                UpdateCommand();
            }
        }

        private void OnBitrateChanged()
        {
            CompressionSettings.Bitrate = Bitrate;
            BitrateSliderValue = Bitrate;
            UpdateTargetSizeFromBitrate();
            UpdateBitrateTexts();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnBitrateSliderChanged()
        {
            Bitrate = (int)BitrateSliderValue;
        }

        private void OnCodecChanged()
        {
            if (SelectedCodecOption != null)
            {
                SelectedCodec = SelectedCodecOption.Value;
                CompressionSettings.Codec = SelectedCodec;
                CalculateOptimalBitrate();
                UpdateCommand();
            }
        }

        public async Task ProcessSelectedInput(string path)
        {
            CompressionSettings.InputPath = path;
            InputPathText = path;
            HasSelectedInput = true;
            IsBatchMode = Directory.Exists(path);

            if (IsBatchMode)
            {
                await ProcessSelectedFolder(path);
                return;
            }

            // 分析视频文件
            var extension = Path.GetExtension(path).ToLower();
            if (IsVideoExtension(extension))
            {
                IsSelectedAudioInput = false;
                CanUseVideoConversionTools = true;
                BatchModeText = "";
                EstimatedBitrateText = LocalizationService.T("Estimate.Analyzing");
                EstimatedBitrateColor = "Blue";

                CurrentVideoInfo = await _videoAnalyzer.AnalyzeVideo(path);

                if (CurrentVideoInfo != null)
                {
                    IsVideoInfoVisible = true;
                    InitializeTargetSizeFromVideo();
                    CalculateBitrateFromTargetSize();
                    UpdateBitrateWarningAndEstimation();
                }
            }
            else if (IsAudioExtension(extension))
            {
                IsSelectedAudioInput = true;
                CanUseVideoConversionTools = false;
                BatchModeText = "";
                CurrentVideoInfo = null;
                IsVideoInfoVisible = false;
                EstimatedBitrateText = LocalizationService.T("Estimate.AudioFile");
                EstimatedBitrateColor = "Gray";
                EnableAudioConversion = true;
            }
            else
            {
                IsSelectedAudioInput = false;
                CanUseVideoConversionTools = false;
                BatchModeText = "";
                CurrentVideoInfo = null;
                IsVideoInfoVisible = false;
                EstimatedBitrateText = LocalizationService.T("Estimate.NonVideo");
                EstimatedBitrateColor = "Gray";
            }

            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        private Task ProcessSelectedFolder(string path)
        {
            var files = GetBatchInputFiles().ToList();
            BatchFileCount = files.Count;
            CurrentVideoInfo = null;
            IsVideoInfoVisible = false;
            IsSelectedAudioInput = false;
            CanUseVideoConversionTools = files.Any(file => IsVideoExtension(Path.GetExtension(file)));

            if (BatchFileCount == 0)
            {
                EstimatedBitrateText = LocalizationService.T("Estimate.NonVideo");
                EstimatedBitrateColor = "Gray";
                BatchModeText = LocalizationService.T("Batch.Empty");
            }
            else
            {
                EstimatedBitrateText = LocalizationService.Format("Batch.Found", BatchFileCount);
                EstimatedBitrateColor = "Green";
                BatchModeText = LocalizationService.Format("Batch.Mode", BatchFileCount);
            }

            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateCommand();
            return Task.CompletedTask;
        }

        private void CalculateOptimalBitrate()
        {
            if (CurrentVideoInfo == null)
            {
                EstimatedBitrateText = LocalizationService.T("Estimate.SelectVideo");
                return;
            }

            EstimatedBitrateText = LocalizationService.T("Estimate.Calculating");

            CalculateBitrateFromTargetSize();

            UpdateBitrateWarningAndEstimation();
        }

        private void InitializeTargetSizeFromVideo()
        {
            if (CurrentVideoInfo == null)
            {
                return;
            }

            var originalSizeMB = CurrentVideoInfo.FileSize / 1024.0 / 1024.0;
            TargetSizeSliderMaximum = originalSizeMB > 0 ? originalSizeMB : 1;
            var minimumTargetSize = originalSizeMB >= 1
                ? Math.Max(1, originalSizeMB * 0.03)
                : Math.Max(0.1, originalSizeMB * 0.03);
            TargetSizeSliderMinimum = Math.Min(TargetSizeSliderMaximum, minimumTargetSize);
            TargetSizeMB = TargetSizeSliderMaximum;
            TargetSizeSliderValue = TargetSizeMB;
            CompressionSettings.TargetSizeMB = TargetSizeMB;
            UpdateTargetSizeTexts();
            UpdateBitrateControlsRange(CurrentVideoInfo.Bitrate);
        }

        private void CalculateBitrateFromTargetSize()
        {
            if (CurrentVideoInfo == null)
            {
                return;
            }

            var targetBitrate = _commandBuilder.CalculateBitrateForTargetSize(CurrentVideoInfo, TargetSizeMB);
            if (SelectedCompressionPresetOption != null && SelectedCompressionPresetOption.Value != "none")
            {
                if (SelectedCompressionPresetOption.MinVideoBitrateKbps > 0)
                {
                    targetBitrate = Math.Max(targetBitrate, SelectedCompressionPresetOption.MinVideoBitrateKbps);
                }

                if (SelectedCompressionPresetOption.MaxVideoBitrateKbps > 0)
                {
                    targetBitrate = Math.Min(targetBitrate, SelectedCompressionPresetOption.MaxVideoBitrateKbps);
                }
            }

            _isSyncingCompressionValues = true;
            Bitrate = targetBitrate;
            BitrateSliderValue = targetBitrate;
            _isSyncingCompressionValues = false;
            CompressionSettings.Bitrate = Bitrate;
            UpdateBitrateTexts();
        }

        private void UpdateTargetSizeFromBitrate()
        {
            if (_isSyncingCompressionValues || CurrentVideoInfo == null)
            {
                return;
            }

            var estimatedSize = _commandBuilder.CalculateEstimatedFileSize(Bitrate, CurrentVideoInfo.Duration);
            var estimatedSizeMB = estimatedSize / 1024.0 / 1024.0;

            _isSyncingCompressionValues = true;
            TargetSizeMB = ClampTargetSize(estimatedSizeMB);
            TargetSizeSliderValue = TargetSizeMB;
            _isSyncingCompressionValues = false;
            CompressionSettings.TargetSizeMB = TargetSizeMB;
            UpdateTargetSizeTexts();
        }

        private double ClampTargetSize(double targetSizeMB)
        {
            if (TargetSizeSliderMaximum <= 0)
            {
                return targetSizeMB;
            }

            return Math.Max(TargetSizeSliderMinimum, Math.Min(targetSizeMB, TargetSizeSliderMaximum));
        }

        private void ApplyCompressionPreset(CompressionPresetOption preset)
        {
            if (preset.Value == "none")
            {
                CompressionSettings.UseCrf = false;
                CompressionSettings.Crf = 23;
                CompressionSettings.AudioBitrate = 96;
                CompressionSettings.MaxHeight = 0;
                CompressionSettings.MaxFramerate = 0;
                CompressionSettings.Codec = SelectedCodec;
                return;
            }

            CompressionSettings.Codec = preset.Codec;
            CompressionSettings.UseCrf = preset.UseCrf;
            CompressionSettings.Crf = preset.Crf;
            CompressionSettings.AudioBitrate = preset.AudioBitrateKbps;
            CompressionSettings.MaxHeight = preset.MaxHeight;
            CompressionSettings.MaxFramerate = preset.MaxFramerate;

            SelectedCodec = preset.Codec;
            SelectedCodecOption = CodecOptions.Find(option => option.Value == preset.Codec) ?? SelectedCodecOption;
        }

        private void UpdateBitrateControlsRange(int originalBitrate)
        {
            BitrateSliderMaximum = Math.Max(originalBitrate * 3 / 2, 50000);
            BitrateSliderMinimum = 1;
        }

        private void UpdateBitrateWarningAndEstimation()
        {
            if (CurrentVideoInfo == null) return;

            if (CompressionSettings.UseCrf)
            {
                IsBitrateWarningVisible = false;
                EstimatedBitrateText = LocalizationService.Format("Estimate.Crf", CompressionSettings.Crf, CompressionSettings.AudioBitrate);
                EstimatedBitrateColor = "Green";
                return;
            }

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
            ApplySelectedConversionOptions();
            CompressionSettings.Bitrate = Bitrate;

            if (IsBatchMode)
            {
                RefreshBatchModeSummary();
                var firstFile = GetBatchInputFiles().FirstOrDefault();
                var firstCommand = firstFile == null
                    ? LocalizationService.T("Batch.Empty")
                    : _commandBuilder.BuildCommand(CreateSettingsForInput(firstFile)).BuildCommand();
                CommandText = BuildBatchCommandPreview(firstCommand);
            }
            else
            {
                var command = _commandBuilder.BuildCommand(CompressionSettings, CurrentVideoInfo);
                CommandText = command.BuildCommand();
            }
            
            CanExecute = CompressionSettings.IsValid && _ffmpegManager.IsFFmpegAvailable && (!IsBatchMode || BatchFileCount > 0);
        }

        private void UpdateConversionOptionVisibility()
        {
            IsFormatConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && !IsSelectedAudioInput && EnableFormatConversion;
            IsAudioConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableAudioConversion;
            IsResolutionConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && !IsSelectedAudioInput && EnableResolutionConversion;
        }

        private void ApplySelectedConversionOptions()
        {
            CompressionSettings.EnableFormatConversion = EnableFormatConversion;
            CompressionSettings.EnableAudioConversion = EnableAudioConversion;
            CompressionSettings.EnableResolutionConversion = EnableResolutionConversion;

            if (SelectedVideoFormatOption != null)
            {
                CompressionSettings.OutputFormat = SelectedVideoFormatOption.Value;
            }

            if (SelectedAudioFormatOption != null)
            {
                CompressionSettings.AudioOutputFormat = SelectedAudioFormatOption.Value;
            }

            if (SelectedResolutionOption != null && int.TryParse(SelectedResolutionOption.Value, out var height))
            {
                CompressionSettings.ResolutionHeight = height;
            }
        }

        private string BuildBatchCommandPreview(string firstCommand)
        {
            if (BatchFileCount <= 0)
            {
                return LocalizationService.T("Batch.Empty");
            }

            return $"{LocalizationService.Format("Batch.Preview", BatchFileCount)}\n{firstCommand}";
        }

        private IEnumerable<string> GetBatchInputFiles()
        {
            if (!Directory.Exists(CompressionSettings.InputPath))
            {
                return Array.Empty<string>();
            }

            return Directory.EnumerateFiles(CompressionSettings.InputPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file =>
                {
                    var extension = Path.GetExtension(file);
                    return EnableAudioConversion
                        ? IsVideoExtension(extension) || IsAudioExtension(extension)
                        : IsVideoExtension(extension);
                })
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
        }

        private void RefreshBatchModeSummary()
        {
            if (!IsBatchMode)
            {
                return;
            }

            BatchFileCount = GetBatchInputFiles().Count();
            BatchModeText = BatchFileCount == 0
                ? LocalizationService.T("Batch.Empty")
                : LocalizationService.Format("Batch.Mode", BatchFileCount);
            EstimatedBitrateText = BatchFileCount == 0
                ? LocalizationService.T("Estimate.NonVideo")
                : LocalizationService.Format("Batch.Found", BatchFileCount);
            EstimatedBitrateColor = BatchFileCount == 0 ? "Gray" : "Green";
        }

        private void UpdateConversionHint()
        {
            if (EnableAudioConversion)
            {
                ConversionModeHint = LocalizationService.T("Conversion.AudioExclusiveHint");
            }
            else if (EnableFormatConversion && SelectedVideoFormatOption?.Value == "gif")
            {
                ConversionModeHint = LocalizationService.T("Conversion.GifHint");
            }
            else if (EnableFormatConversion || EnableResolutionConversion)
            {
                ConversionModeHint = LocalizationService.T("Conversion.VideoToolsHint");
            }
            else
            {
                ConversionModeHint = "";
            }
        }

        private static bool IsVideoExtension(string? extension)
        {
            return VideoExtensions.Contains((extension ?? "").ToLowerInvariant());
        }

        private static bool IsAudioExtension(string? extension)
        {
            return AudioExtensions.Contains((extension ?? "").ToLowerInvariant());
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
            UpdateCompressionPresetOptions();
            UpdateBitrateTexts();
            UpdateTargetSizeTexts();
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

        private void UpdateCompressionPresetOptions()
        {
            var selectedValue = SelectedCompressionPresetOption?.Value ?? "none";
            CompressionPresetOptions = CreateCompressionPresetOptions();
            SelectedCompressionPresetOption = CompressionPresetOptions.Find(option => option.Value == selectedValue) ?? CompressionPresetOptions[0];
        }

        private List<CompressionPresetOption> CreateCompressionPresetOptions()
        {
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            return new List<CompressionPresetOption>
            {
                new(isEnglish ? "None" : "无", "none", 0, isEnglish ? "Manual target size" : "手动设置目标大小"),
                new(isEnglish ? "WeChat/QQ" : "微信/QQ发送", "chat", 100, isEnglish ? "Sendable and autoplay friendly" : "能发出去，兼顾自动播放")
                {
                    Codec = "libx264",
                    AudioBitrateKbps = 96,
                    MaxHeight = 720,
                    MaxFramerate = 30,
                    MinVideoBitrateKbps = 800,
                    MaxVideoBitrateKbps = 1500
                },
                new(isEnglish ? "Email attachment" : "邮箱附件", "email", 25, isEnglish ? "For 20 MB / 25 MB mail limits" : "适合 20MB / 25MB 邮件附件")
                {
                    Codec = "libx264",
                    AudioBitrateKbps = 64,
                    MaxHeight = 720
                },
                new(isEnglish ? "Web upload" : "网页上传", "web", 0, isEnglish ? "Clearer output for upload platforms" : "适合上传平台，保持清晰度")
                {
                    Codec = "libx264",
                    UseCrf = true,
                    Crf = 23,
                    AudioBitrateKbps = 128
                },
                new(isEnglish ? "Extreme" : "极限压缩", "extreme", 0, isEnglish ? "Smallest practical file, lower quality" : "最大限度压缩体积，画质较低")
                {
                    Codec = "libx265",
                    UseCrf = true,
                    Crf = 30,
                    AudioBitrateKbps = 48,
                    MaxHeight = 480,
                    MaxFramerate = 24
                }
            };
        }

        private void UpdateBitrateTexts()
        {
            BitrateValueText = $"{Bitrate}k";
            BitrateSelectionText = LocalizationService.Format("Main.CurrentSelection", BitrateValueText);
        }

        private void UpdateTargetSizeTexts()
        {
            TargetSizeValueText = $"{TargetSizeMB:F1} MB";
            TargetSizeSelectionText = LocalizationService.Format("Main.CurrentSelection", TargetSizeValueText);
        }

        private async Task<ConversionSummary> ExecuteFFmpegCommand()
        {
            if (!_ffmpegManager.IsFFmpegAvailable)
            {
                throw new Exception(LocalizationService.T("Status.NotConfigured"));
            }

            var stopwatch = Stopwatch.StartNew();
            var results = new List<ConversionResult>();

            if (IsBatchMode)
            {
                var files = GetBatchInputFiles().ToList();
                if (files.Count == 0)
                {
                    throw new Exception(LocalizationService.T("Batch.Empty"));
                }

                foreach (var file in files)
                {
                    var command = _commandBuilder.BuildCommand(CreateSettingsForInput(file));
                    results.Add(await RunFFmpegCommand(command));
                }

                stopwatch.Stop();
                return new ConversionSummary(results, stopwatch.Elapsed);
            }

            var singleCommand = _commandBuilder.BuildCommand(CompressionSettings);
            results.Add(await RunFFmpegCommand(singleCommand));
            stopwatch.Stop();
            return new ConversionSummary(results, stopwatch.Elapsed);
        }

        private CompressionSettings CreateSettingsForInput(string inputPath)
        {
            return new CompressionSettings
            {
                CompressionPercentage = CompressionSettings.CompressionPercentage,
                TargetSizeMB = CompressionSettings.TargetSizeMB,
                Bitrate = CompressionSettings.Bitrate,
                Codec = CompressionSettings.Codec,
                UseCrf = CompressionSettings.UseCrf,
                Crf = CompressionSettings.Crf,
                AudioBitrate = CompressionSettings.AudioBitrate,
                MaxHeight = CompressionSettings.MaxHeight,
                MaxFramerate = CompressionSettings.MaxFramerate,
                EnableFormatConversion = CompressionSettings.EnableFormatConversion,
                OutputFormat = CompressionSettings.OutputFormat,
                EnableAudioConversion = CompressionSettings.EnableAudioConversion,
                AudioOutputFormat = CompressionSettings.AudioOutputFormat,
                EnableResolutionConversion = CompressionSettings.EnableResolutionConversion,
                ResolutionHeight = CompressionSettings.ResolutionHeight,
                InputPath = inputPath,
                OutputPath = CompressionSettings.OutputPath
            };
        }

        private async Task<ConversionResult> RunFFmpegCommand(FFmpegCommand command)
        {
            var commandText = command.BuildCommand();
            var arguments = commandText.StartsWith("ffmpeg ", StringComparison.OrdinalIgnoreCase)
                ? commandText["ffmpeg ".Length..]
                : commandText;
            var inputInfo = CurrentVideoInfo?.FilePath == command.InputPath
                ? CurrentVideoInfo
                : await _videoAnalyzer.AnalyzeVideo(command.InputPath);

            var outputDirectory = Path.GetDirectoryName(command.OutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

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
                throw new FFmpegExecutionException(
                    $"FFmpeg执行失败，退出代码: {process.ExitCode}",
                    process.ExitCode,
                    output);
            }

            var outputInfo = await _videoAnalyzer.AnalyzeVideo(command.OutputPath);
            if (outputInfo == null && File.Exists(command.OutputPath))
            {
                outputInfo = new VideoInfo
                {
                    FilePath = command.OutputPath,
                    FileSize = new FileInfo(command.OutputPath).Length
                };
            }

            return new ConversionResult(command.InputPath, command.OutputPath, inputInfo, outputInfo);
        }

        private static string BuildCompletionMessage(ConversionSummary summary)
        {
            var lines = new List<string>
            {
                summary.Results.Count > 1
                    ? $"批量转换完成：{summary.Results.Count} 个文件"
                    : "转换完成",
                $"用时：{FormatDuration(summary.Elapsed)}"
            };

            foreach (var result in summary.Results)
            {
                lines.Add("");
                lines.Add(Path.GetFileName(result.InputPath));
                lines.Add($"输出：{result.OutputPath}");

                if (result.InputInfo != null && result.OutputInfo != null)
                {
                    lines.Add($"大小：{FormatBytes(result.InputInfo.FileSize)} -> {FormatBytes(result.OutputInfo.FileSize)} ({FormatSizeChange(result.InputInfo.FileSize, result.OutputInfo.FileSize)})");

                    if (result.InputInfo.Bitrate > 0 || result.OutputInfo.Bitrate > 0)
                    {
                        lines.Add($"比特率：{FormatBitrate(result.InputInfo.Bitrate)} -> {FormatBitrate(result.OutputInfo.Bitrate)}");
                    }
                }
                else if (result.OutputInfo != null)
                {
                    lines.Add($"输出大小：{FormatBytes(result.OutputInfo.FileSize)}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildFailureMessage(FFmpegExecutionException exception)
        {
            var lines = new List<string> { exception.Message };

            if (!string.IsNullOrWhiteSpace(exception.FFmpegOutput))
            {
                lines.Add("");
                lines.Add("FFmpeg 信息：");
                lines.Add(TrimFFmpegOutput(exception.FFmpegOutput));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string TrimFFmpegOutput(string output)
        {
            var lines = output
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .TakeLast(12);

            return string.Join(Environment.NewLine, lines);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalHours >= 1
                ? duration.ToString(@"hh\:mm\:ss")
                : duration.ToString(@"mm\:ss");
        }

        private static string FormatBytes(long bytes)
        {
            var sizeMB = bytes / 1024.0 / 1024.0;
            return sizeMB < 1024
                ? $"{sizeMB:F1} MB"
                : $"{sizeMB / 1024.0:F2} GB";
        }

        private static string FormatSizeChange(long before, long after)
        {
            if (before <= 0)
            {
                return "无法计算变化";
            }

            var percentage = (after - before) * 100.0 / before;
            return percentage <= 0
                ? $"减少 {Math.Abs(percentage):F1}%"
                : $"增加 {percentage:F1}%";
        }

        private static string FormatBitrate(int bitrate)
        {
            return bitrate > 0 ? $"{bitrate} kb/s" : "未知";
        }

        private sealed record ConversionSummary(List<ConversionResult> Results, TimeSpan Elapsed);

        private sealed record ConversionResult(
            string InputPath,
            string OutputPath,
            VideoInfo? InputInfo,
            VideoInfo? OutputInfo);

        private sealed class FFmpegExecutionException : Exception
        {
            public FFmpegExecutionException(string message, int exitCode, string ffmpegOutput)
                : base(message)
            {
                ExitCode = exitCode;
                FFmpegOutput = ffmpegOutput;
            }

            public int ExitCode { get; }
            public string FFmpegOutput { get; }
        }

        #endregion

        public override void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            base.Dispose();
        }
    }
}
