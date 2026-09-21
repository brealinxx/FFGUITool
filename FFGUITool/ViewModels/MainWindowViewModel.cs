using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        private readonly ExifToolManager _exifToolManager;
        private readonly VideoAnalyzer _videoAnalyzer;
        private readonly CommandBuilder _commandBuilder;
        private readonly ProcessingExecutor _processingExecutor;
        private readonly ProcessingWorkspace _processingWorkspace = new();
        private readonly MediaInputService _mediaInputService;
        private readonly IDialogService _dialogService;
        private readonly AppConfig _appConfig;
        private const string ReleasesUrl = "https://github.com/brealinxx/FFGUITool/releases";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/brealinxx/FFGUITool/releases/latest";
        private bool _isSyncingCompressionValues;
        private bool _isSyncingConversionToggles;
        private bool _isUpdatingInputPathText;
        private bool _isRestoringSourceTab;
        private bool _isLoadingSourceTab;
        private CancellationTokenSource? _executionCancellation;
        private OutputConflictPolicy _outputConflictPolicy = OutputConflictPolicy.AutoRename;
        private VideoInfo? _batchPreviewInfo;
        private string _batchPreviewInfoPath = "";
        private IReadOnlySet<string> _availableVideoEncoders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private IReadOnlySet<string> _availableVideoDecoders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


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
        private bool _isModeSelectionVisible = true;

        [ObservableProperty]
        private bool _isWorkspaceVisible;

        [ObservableProperty]
        private bool _isImageMode;

        [ObservableProperty]
        private string _currentModeTitle = LocalizationService.T("Mode.Choose");

        [ObservableProperty]
        private string _inputSourceTitle = LocalizationService.T("Main.InputSource");

        [ObservableProperty]
        private string _compressionParamsTitle = LocalizationService.T("Main.CompressionParams");

        [ObservableProperty]
        private string _targetSizeLabel = LocalizationService.T("Main.TargetSize");

        [ObservableProperty]
        private string _targetSizeUnitText = "MB";

        [ObservableProperty]
        private bool _isImageTargetUnitSelectorVisible;

        [ObservableProperty]
        private bool _canEditTargetSize = true;

        [ObservableProperty]
        private bool _canEditAdvancedMode = true;

        [ObservableProperty]
        private List<string> _imageTargetSizeUnitOptions = new() { "KB", "MB" };

        [ObservableProperty]
        private string _selectedImageTargetSizeUnit = "KB";

        [ObservableProperty]
        private string _advancedBitrateLabel = LocalizationService.T("Main.TargetBitrate");

        [ObservableProperty]
        private bool _isAdvancedQualityControlsVisible;

        [ObservableProperty]
        private bool _canEditBitrate = true;

        [ObservableProperty]
        private string _formatOptionLabel = LocalizationService.T("Main.VideoFormat");

        [ObservableProperty]
        private string _sourceInfoTitle = LocalizationService.T("Main.SourceInfo");

        [ObservableProperty]
        private string _sourceSizeLabel = LocalizationService.T("Main.Size");

        [ObservableProperty]
        private string _sourceSecondMetricLabel = LocalizationService.T("Main.Duration");

        [ObservableProperty]
        private string _sourceSecondMetricValue = "";

        [ObservableProperty]
        private string _sourceThirdMetricLabel = LocalizationService.T("Main.OriginalBitrate");

        [ObservableProperty]
        private string _sourceThirdMetricValue = "";

        [ObservableProperty]
        private string _sourceBadgeText = "";

        [ObservableProperty]
        private bool _isSourceBadgeVisible;

        [ObservableProperty]
        private string _sourceMetadataText = "";

        [ObservableProperty]
        private bool _clearMetadata;

        [ObservableProperty]
        private bool _isMetadataClearOptionVisible;

        [ObservableProperty]
        private bool _isMetadataPreviewVisible;

        [ObservableProperty]
        private bool _canClearMetadata;

        [ObservableProperty]
        private string _metadataClearHint = "";

        [ObservableProperty]
        private CompressionSettings _compressionSettings = new();

        [ObservableProperty]
        private string _commandText = LocalizationService.T("Command.SelectInput");

        [ObservableProperty]
        private bool _isFailureActionsVisible;

        [ObservableProperty]
        private string _lastFailureDetails = "";

        [ObservableProperty]
        private string _lastFailureCommand = "";

        [ObservableProperty]
        private bool _canExecute;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private string _progressText = "";

        [ObservableProperty]
        private bool _isProgressVisible;

        [ObservableProperty]
        private bool _canCancel;

        [ObservableProperty]
        private bool _canRetryFailed;

        [ObservableProperty]
        private string _executeAllText = LocalizationService.T("Main.StartConvert");

        [ObservableProperty]
        private string _inputPathText = "";

        [ObservableProperty]
        private bool _isSourceTabsVisible;

        public ObservableCollection<ProcessingTask> SourceTabs => _processingWorkspace.IndependentTasks;

        [ObservableProperty]
        private bool _isInputStatusVisible;

        [ObservableProperty]
        private bool _isBatchTaskListVisible;

        public ObservableCollection<ProcessingTask> BatchTasks => _processingWorkspace.SharedTasks;

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
        private string _hardwareEncoderLabel = "硬件加速";

        [ObservableProperty]
        private string _hardwareEncoderHelpLabel = "?";

        [ObservableProperty]
        private string _crfQualityLabel = "CRF 清晰度";

        [ObservableProperty]
        private string _enableCrfLabel = "启用 CRF 质量模式";

        [ObservableProperty]
        private List<CodecOption> _hardwareEncoderOptions = new()
        {
            new CodecOption("关闭", "", "使用软件编码")
        };

        [ObservableProperty]
        private CodecOption? _selectedHardwareEncoderOption;

        [ObservableProperty]
        private bool _useCrf;

        [ObservableProperty]
        private int _crf = 23;

        [ObservableProperty]
        private double _crfSliderValue = 23;

        [ObservableProperty]
        private bool _isCrfControlsVisible;

        [ObservableProperty]
        private string _crfSelectionText = "";

        [ObservableProperty]
        private string _estimatedBitrateText = "";

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
        private bool _isThemeSystem;

        [ObservableProperty]
        private bool _isThemeLight;

        [ObservableProperty]
        private bool _isThemeDarkManual;

        [ObservableProperty]
        private string _themeSystemMenuText = "";

        [ObservableProperty]
        private string _themeLightMenuText = "";

        [ObservableProperty]
        private string _themeDarkMenuText = "";

        [ObservableProperty]
        private bool _isChineseLanguage = LocalizationService.CurrentLanguage == "zh-CN";

        [ObservableProperty]
        private bool _isEnglishLanguage = LocalizationService.CurrentLanguage == "en-US";

        [ObservableProperty]
        private List<CodecOption> _codecOptions = new()
        {
            new CodecOption("H.264 (libx264)", "libx264", LocalizationService.T("Codec.H264.Desc")),
            new CodecOption("H.265 (libx265)", "libx265", LocalizationService.T("Codec.H265.Desc")),
            new CodecOption("VP9 (libvpx-vp9)", "libvpx-vp9", LocalizationService.T("Codec.VP9.Desc")),
            new CodecOption("AV1 (libaom-av1)", "libaom-av1", LocalizationService.T("Codec.AV1.Desc"))
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
        private bool _includeSubfolders;

        [ObservableProperty]
        private int _batchFileCount;

        [ObservableProperty]
        private string _batchModeText = "";

        [ObservableProperty]
        private bool _canUseVideoConversionTools;

        [ObservableProperty]
        private bool _canEditVideoConversionTools;

        [ObservableProperty]
        private bool _canEditResolutionConversionTools;

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
        private bool _isVideoOutputOptionsVisible;

        [ObservableProperty]
        private bool _enableTrim;

        [ObservableProperty]
        private string _trimStartText = "";

        [ObservableProperty]
        private string _trimEndText = "";

        [ObservableProperty]
        private string _trimHintText = "未启用";

        [ObservableProperty]
        private bool _isAdvancedVideoControlsVisible;

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
        private List<CodecOption> _audioBitrateOptions = new()
        {
            new CodecOption("320 kb/s", "320", LocalizationService.T("AudioBitrate.320.Desc")),
            new CodecOption("256 kb/s", "256", LocalizationService.T("AudioBitrate.256.Desc")),
            new CodecOption("128 kb/s", "128", LocalizationService.T("AudioBitrate.128.Desc")),
            new CodecOption("96 kb/s", "96", LocalizationService.T("AudioBitrate.96.Desc")),
            new CodecOption("64 kb/s", "64", LocalizationService.T("AudioBitrate.64.Desc")),
            new CodecOption("8 kb/s", "8", LocalizationService.T("AudioBitrate.8.Desc"))
        };

        [ObservableProperty]
        private CodecOption? _selectedAudioBitrateOption;

        [ObservableProperty]
        private List<CodecOption> _audioTrackModeOptions = new()
        {
            new CodecOption("重新编码音频", "transcode", "兼容性最好"),
            new CodecOption("保留原音轨", "copy", "不重新编码音频"),
            new CodecOption("移除音轨", "remove", "输出静音视频")
        };

        [ObservableProperty]
        private CodecOption? _selectedAudioTrackModeOption;

        [ObservableProperty]
        private List<CodecOption> _resolutionOptions = new()
        {
            new CodecOption("原尺寸", "0", "不调整尺寸"),
            new CodecOption("2160p", "2160", "4K"),
            new CodecOption("1080p", "1080", "全高清"),
            new CodecOption("720p", "720", "高清"),
            new CodecOption("480p", "480", "小体积"),
            new CodecOption("512px", "512", "头像"),
            new CodecOption("360p", "360", "极小体积")
        };

        [ObservableProperty]
        private CodecOption? _selectedResolutionOption;

        [ObservableProperty]
        private List<CodecOption> _imageFormatOptions = new()
        {
            new CodecOption("JPG", "jpg", "通用照片格式"),
            new CodecOption("PNG", "png", "透明与无损场景"),
            new CodecOption("WebP", "webp", "网页体积更小")
        };

        [ObservableProperty]
        private CodecOption? _selectedImageFormatOption;

        [ObservableProperty]
        private bool _isIconOptionsVisible;

        [ObservableProperty]
        private bool _icoSize16 = true;

        [ObservableProperty]
        private bool _icoSize24;

        [ObservableProperty]
        private bool _icoSize32 = true;

        [ObservableProperty]
        private bool _icoSize48 = true;

        [ObservableProperty]
        private bool _icoSize64;

        [ObservableProperty]
        private bool _icoSize128;

        [ObservableProperty]
        private bool _icoSize256 = true;



        [RelayCommand]
        private void SelectVideoMode()
        {
            if (IsProcessing || IsScanning || IsPreviewing) return;
            IsImageMode = false;
            CompressionSettings.IsImageProcessing = false;
            IsModeSelectionVisible = false;
            IsWorkspaceVisible = true;
            IsAdvancedMode = false;
            BitrateSliderMinimum = 1;
            BitrateSliderMaximum = 50000;
            Bitrate = CompressionSettings.Bitrate > 0 ? CompressionSettings.Bitrate : 2000;
            RefreshModeText();
            ResetSelectedInput();
            ApplyPresetEditability();
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        [RelayCommand]
        private void SelectImageMode()
        {
            if (IsProcessing || IsScanning || IsPreviewing) return;
            IsImageMode = true;
            CompressionSettings.IsImageProcessing = true;
            IsModeSelectionVisible = false;
            IsWorkspaceVisible = true;
            IsAdvancedMode = false;
            EnableAudioConversion = false;
            EnableFormatConversion = true;
            EnableResolutionConversion = false;
            SelectedImageFormatOption ??= ImageFormatOptions[0];
            CompressionSettings.ImageOutputFormat = SelectedImageFormatOption.Value;
            BitrateSliderMinimum = 1;
            BitrateSliderMaximum = 100;
            Bitrate = CompressionSettings.ImageQuality;
            RefreshModeText();
            ResetSelectedInput();
            ApplyPresetEditability();
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        [RelayCommand]
        private void BackToModeSelection()
        {
            if (IsProcessing || IsScanning || IsPreviewing) return;
            IsModeSelectionVisible = true;
            IsWorkspaceVisible = false;
            ResetSelectedInput();
        }

        [RelayCommand]
        private async Task SelectFile()
        {
            var files = await _dialogService.OpenFilesDialog(IsImageMode ? LocalizationService.T("Image.SelectFile") : LocalizationService.T("Picker.SelectVideo"), IsImageMode ? new[]
            {
                new FilePickerFileType(LocalizationService.T("Image.FileType"))
                {
                    Patterns = MediaFileSupport.ImageExtensions.Select(extension => $"*{extension}").ToArray()
                },
                new FilePickerFileType(LocalizationService.T("Picker.AllFiles"))
                {
                    Patterns = new[] { "*.*" }
                }
            } : new[]
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

            if (files.Count > 0)
            {
                await ProcessSelectedInputs(files.Select(file => file.Path.LocalPath));
            }
        }

        [RelayCommand]
        private async Task SelectFolder()
        {
            var folder = await _dialogService.OpenFolderDialog(IsImageMode ? LocalizationService.T("Image.SelectFolder") : LocalizationService.T("Picker.SelectFolder"));
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
            _exifToolManager = new ExifToolManager();
            _videoAnalyzer = new VideoAnalyzer(_ffmpegManager);
            _mediaInputService = new MediaInputService(_videoAnalyzer);
            _commandBuilder = new CommandBuilder();
            _processingExecutor = new ProcessingExecutor(
                _ffmpegManager,
                _exifToolManager,
                _videoAnalyzer,
                _commandBuilder);
            _appConfig = AppConfigService.Load();
            CurrentTheme = ParseThemeVariant(_appConfig.Theme);

            // 设置默认编码器选项
            SelectedCodecOption = CodecOptions[0];
            SelectedHardwareEncoderOption = HardwareEncoderOptions[0];
            CompressionPresetOptions = CreateCompressionPresetOptions();
            UpdateConversionOptionLists();
            SelectedCompressionPresetOption = CompressionPresetOptions[0];
            SelectedVideoFormatOption = VideoFormatOptions[0];
            SelectedAudioFormatOption = AudioFormatOptions[0];
            SelectedAudioBitrateOption = AudioBitrateOptions.Find(option => option.Value == CompressionSettings.AudioBitrate.ToString()) ?? AudioBitrateOptions[^1];
            SelectedAudioTrackModeOption = AudioTrackModeOptions[0];
            SelectedResolutionOption = ResolutionOptions[3];
            SelectedImageFormatOption = ImageFormatOptions[0];
            RefreshAdvancedVideoLabels();
            UpdateThemeStateTexts();
            UpdateBitrateTexts();
            UpdateCrfText();

            // 监听属性变化
            PropertyChanged += OnPropertyChanged;
            LocalizationService.LanguageChanged += OnLanguageChanged;
            InitializeFeatures();
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
                var setupViewModel = new SetupWindowViewModel(_ffmpegManager, _exifToolManager);
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
            await InitializeExifTool();
            await RefreshHardwareEncoderOptions();
        }

        private async Task InitializeExifTool()
        {
            await _exifToolManager.InitializeAsync();
            UpdateExifToolStatus();
        }

        private void UpdateExifToolStatus()
        {
            CanClearMetadata = _exifToolManager.IsExifToolAvailable;
            MetadataClearHint = CanClearMetadata
                ? LocalizationService.T("ExifTool.Ready")
                : LocalizationService.T("ExifTool.NotConfigured");
            if (!CanClearMetadata)
            {
                ClearMetadata = false;
            }
        }



        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isRestoringSourceTab)
            {
                return;
            }

            MarkSelectedSourceTabPending(e.PropertyName);
            if (HandleFeatureChange(e.PropertyName)) return;

            switch (e.PropertyName)
            {
                case nameof(InputPathText):
                    _ = OnInputPathTextChanged();
                    break;
                case nameof(IncludeSubfolders):
                    if (IsBatchMode)
                    {
                        _ = RefreshFolderPreviewAsync();
                    }
                    break;
                case nameof(SelectedImageFormatOption):
                    OnImageFormatChanged();
                    break;
                case nameof(IcoSize16):
                case nameof(IcoSize24):
                case nameof(IcoSize32):
                case nameof(IcoSize48):
                case nameof(IcoSize64):
                case nameof(IcoSize128):
                case nameof(IcoSize256):
                    OnIconSizeChanged();
                    break;
                case nameof(SelectedImageTargetSizeUnit):
                    OnImageTargetSizeUnitChanged();
                    break;
                case nameof(TargetSizeMB):
                    OnTargetSizeChanged();
                    break;
                case nameof(TargetSizeSliderValue):
                    CoalesceSliderUpdate(OnTargetSizeSliderChanged);
                    break;
                case nameof(SelectedCompressionPresetOption):
                    OnCompressionPresetChanged();
                    break;
                case nameof(EnableFormatConversion):
                case nameof(EnableAudioConversion):
                case nameof(EnableResolutionConversion):
                    OnConversionToggleChanged(e.PropertyName);
                    break;
                case nameof(ClearMetadata):
                    OnClearMetadataChanged();
                    break;
                case nameof(SelectedVideoFormatOption):
                    OnVideoFormatChanged();
                    break;
                case nameof(SelectedAudioFormatOption):
                    OnAudioFormatChanged();
                    break;
                case nameof(SelectedAudioBitrateOption):
                    OnAudioBitrateChanged();
                    break;
                case nameof(SelectedAudioTrackModeOption):
                    OnAudioTrackModeChanged();
                    break;
                case nameof(EnableTrim):
                    OnTrimChanged();
                    break;
                case nameof(TrimStartText):
                case nameof(TrimEndText):
                    OnTrimTextChanged();
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
                    CoalesceSliderUpdate(OnBitrateSliderChanged);
                    break;
                case nameof(SelectedCodecOption):
                    OnCodecChanged();
                    break;
                case nameof(SelectedHardwareEncoderOption):
                    OnHardwareEncoderChanged();
                    break;
                case nameof(UseCrf):
                    OnUseCrfChanged();
                    break;
                case nameof(Crf):
                    OnCrfChanged();
                    break;
                case nameof(CrfSliderValue):
                    CoalesceSliderUpdate(OnCrfSliderChanged);
                    break;
                case nameof(IsThemeDark):
                    RefreshStatusBrushes();
                    break;
                case nameof(CurrentTheme):
                    if (Application.Current != null)
                    {
                        Application.Current.RequestedThemeVariant = CurrentTheme;
                        IsThemeDark = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
                    }
                    UpdateThemeStateTexts();
                    break;
            }
        }

        private void UpdateCommand()
        {
            try { UpdateCommandCore(); ValidationMessage = ""; }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
            { ValidationMessage = ex.Message; CanExecute = false; }
            UpdateMediaNotice();
            ScheduleWorkspaceSave();
        }

        private void UpdateCommandCore()
        {
            if (_featuresReady) ApplyFeatureSettings();
            ApplySelectedConversionOptions();
            CompressionSettings.ClearMetadata = ClearMetadata;
            CompressionSettings.EnableTrim = EnableTrim;
            CompressionSettings.TrimStart = TrimStartText.Trim();
            CompressionSettings.TrimEnd = TrimEndText.Trim();
            CompressionSettings.AudioTrackMode = SelectedAudioTrackModeOption?.Value ?? CompressionSettings.AudioTrackMode;
            CompressionSettings.InputVideoCodec = CurrentVideoInfo?.VideoCodec ?? "";
            CompressionSettings.PreferDav1dDecoder = string.Equals(CurrentVideoInfo?.VideoCodec, "av1", StringComparison.OrdinalIgnoreCase) &&
                                                     _availableVideoDecoders.Contains("libdav1d");
            if (IsImageMode)
            {
                CompressionSettings.ImageQuality = Bitrate;
                CompressionSettings.ImageTargetSizeKB = LimitFileSize ? ImageTargetDisplayValueToKB(TargetSizeMB) : 0;
                CompressionSettings.IconSizesCsv = string.Join(",", GetSelectedIconSizes());
            }
            else
            {
                CompressionSettings.Bitrate = Bitrate;
            }

            CompressionSettings.OutputLabel = BuildOutputLabel(CompressionSettings);

            if (string.IsNullOrWhiteSpace(CompressionSettings.InputPath))
            {
                CommandText = LocalizationService.T("Command.SelectInput");
                CanExecute = false;
                return;
            }

            if (IsBatchMode)
            {
                RefreshBatchModeSummary();
                RefreshSharedTaskPolicy();
                var firstTask = BatchTasks.FirstOrDefault(task => task.IsIncluded);
                var previewInfo = firstTask != null && string.Equals(firstTask.InputPath, _batchPreviewInfoPath, StringComparison.OrdinalIgnoreCase)
                    ? _batchPreviewInfo
                    : null;
                var previewCommand = firstTask == null
                    ? null
                    : _processingExecutor.BuildCommand(
                        firstTask,
                        new ProcessingExecutionOptions { AvailableVideoDecoders = _availableVideoDecoders },
                        previewInfo);
                var firstCommand = previewCommand?.BuildCommand() ?? LocalizationService.T("Batch.Empty");
                if (ClearMetadata && CanClearMetadata && previewCommand != null)
                {
                    firstCommand = AppendExifToolPreview(firstCommand, previewCommand.OutputPath);
                }
                CommandText = BuildBatchCommandPreview(firstCommand);
            }
            else
            {
                var command = _commandBuilder.BuildCommand(CompressionSettings, CurrentVideoInfo);
                var commandPreview = ClearMetadata && CanClearMetadata
                    ? AppendExifToolPreview(command.BuildCommand(), command.OutputPath)
                    : command.BuildCommand();
                if (!IsImageMode && command.TargetBytes > 0) { CompressionSettings.Bitrate = command.Bitrate; }
                CommandText = SourceTabs.Count > 0
                    ? $"{LocalizationService.Format("SourceTabs.Preview", SourceTabs.Count, Path.GetFileName(CompressionSettings.InputPath))}\n{commandPreview}"
                    : commandPreview;
            }
            
            CanExecute = CompressionSettings.IsValid && _ffmpegManager.IsFFmpegAvailable && !IsProcessing && !IsScanning && FileOrFolderExists() && (!IsBatchMode || BatchFileCount > 0);
            SaveSelectedSourceTabSettings();
            UpdateExecuteAllText();
        }

        private void UpdateConversionOptionVisibility()
        {
            IsAdvancedVideoControlsVisible = IsAdvancedMode && !IsImageMode;
            IsAdvancedQualityControlsVisible = IsAdvancedMode && (!IsImageMode || HasSelectedInput) && !IsPngImage;
            IsMetadataClearOptionVisible = IsAdvancedMode && HasSelectedInput;
            IsMetadataPreviewVisible = IsMetadataClearOptionVisible && CanClearMetadata && ClearMetadata;
            CanEditVideoConversionTools = CanUseVideoConversionTools && !EnableAudioConversion;
            var isIconImageFormat = IsImageMode && SelectedImageFormatOption != null && IsIconFormat(SelectedImageFormatOption.Value);
            CanEditResolutionConversionTools = CanEditVideoConversionTools && !isIconImageFormat;
            UpdateControlEditability();

            if (IsImageMode)
            {
                IsFormatConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableFormatConversion;
                IsAudioConversionOptionsVisible = false;
                IsResolutionConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableResolutionConversion;
                IsVideoOutputOptionsVisible = false;
                IsIconOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableFormatConversion && isIconImageFormat;
                return;
            }

            IsIconOptionsVisible = false;
            IsFormatConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && !IsSelectedAudioInput && EnableFormatConversion;
            IsAudioConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableAudioConversion;
            IsResolutionConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && !IsSelectedAudioInput && EnableResolutionConversion;
            IsVideoOutputOptionsVisible = IsAdvancedMode && HasSelectedInput && !IsSelectedAudioInput && !EnableAudioConversion;
        }

        private void UpdateControlEditability()
        {
            IsCrfControlsVisible = IsAdvancedMode && !IsImageMode && UseCrf;
            CanEditTargetSize = IsImageMode || (!UseCrf && !IsPresetSizeEstimateLocked());
            CanEditBitrate = IsImageMode || (!UseCrf && !IsBatchMode);
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

            if (SelectedImageFormatOption != null)
            {
                CompressionSettings.ImageOutputFormat = SelectedImageFormatOption.Value;
            }

            if (SelectedAudioFormatOption != null)
            {
                CompressionSettings.AudioOutputFormat = SelectedAudioFormatOption.Value;
            }

            if (CompressionSettings.EnableAudioConversion &&
                SelectedAudioBitrateOption != null &&
                int.TryParse(SelectedAudioBitrateOption.Value, out var audioBitrate))
            {
                CompressionSettings.AudioBitrate = audioBitrate;
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

        private string AppendExifToolPreview(string ffmpegCommand, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return ffmpegCommand;
            }

            return $"{ffmpegCommand}\n{_exifToolManager.BuildClearMetadataCommand(outputPath)}";
        }

        private IEnumerable<string> GetDiscoveredBatchInputFiles()
        {
            if (!Directory.Exists(CompressionSettings.InputPath))
            {
                return Array.Empty<string>();
            }

            return _mediaInputService.DiscoverFolderFiles(
                CompressionSettings.InputPath,
                IsImageMode,
                EnableAudioConversion,
                IncludeSubfolders);
        }

        private IEnumerable<string> GetBatchInputFiles()
        {
            if (BatchTasks.Count > 0)
            {
                return BatchTasks.Where(task => task.IsIncluded).Select(task => task.InputPath);
            }

            return Array.Empty<string>();
        }

        private void PopulateBatchTasks(IEnumerable<string> files)
        {
            _processingWorkspace.ReplaceSharedTasks(
                files,
                path => new ProcessingTask(path, CompressionSettings, ProcessingSettingsScope.Shared)
                {
                    Status = LocalizationService.T("SourceTabs.Pending"),
                    StatusColor = "Gray"
                });
            foreach (var task in BatchTasks)
            {
                task.Settings = CompressionSettings;
                task.UsesRelativeTarget = true;
                task.RelativeTargetPercentage = TargetSizeMB;
                task.MinimumVideoBitrateKbps = SelectedCompressionPresetOption?.MinVideoBitrateKbps ?? 0;
                task.MaximumVideoBitrateKbps = SelectedCompressionPresetOption?.MaxVideoBitrateKbps ?? 0;
            }

            IsBatchTaskListVisible = BatchTasks.Count > 0;
        }

        private void ClearBatchTasks()
        {
            _processingWorkspace.ClearSharedTasks();
            IsBatchTaskListVisible = false;
        }

        private void RefreshBatchModeSummary()
        {
            if (!IsBatchMode)
            {
                return;
            }

            BatchFileCount = GetBatchInputFiles().Count();
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            BatchModeText = BatchFileCount == 0
                ? LocalizationService.T(IsImageMode ? "Image.NoSupportedFiles" : "Batch.Empty")
                : $"{LocalizationService.Format("Batch.Included", BatchFileCount, BatchTasks.Count)} " +
                  (isEnglish
                      ? $"Each file targets {TargetSizeMB:0.0}% of its original size."
                      : $"每个文件按 {TargetSizeMB:0.0}% 原始大小压缩。");
            EstimatedBitrateText = IsImageMode
                ? BatchFileCount == 0
                    ? LocalizationService.T("Image.NoSupportedFiles")
                    : BuildImageEstimateText()
                : BatchFileCount == 0
                    ? LocalizationService.T("Estimate.NonVideo")
                    : LocalizationService.Format("Batch.Found", BatchFileCount);
            EstimatedBitrateColor = BatchFileCount == 0 ? "Gray" : "Green";
            CanRetryFailed = BatchTasks.Any(task => task.IsIncluded && task.IsFailed);
            UpdateExecuteAllText();
        }

        private void UpdateConversionHint()
        {
            if (IsImageMode)
            {
                ConversionModeHint = EnableFormatConversion || EnableResolutionConversion
                    ? LocalizationService.T("Image.ConversionHint")
                    : LocalizationService.T("Image.CompressHint");
                return;
            }

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
            return MediaFileSupport.IsVideoExtension(extension);
        }

        private static bool IsAudioExtension(string? extension)
        {
            return MediaFileSupport.IsAudioExtension(extension);
        }

        private static bool IsImageExtension(string? extension)
        {
            return MediaFileSupport.IsImageExtension(extension);
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
            _appConfig.Language = LocalizationService.CurrentLanguage;
            IsChineseLanguage = LocalizationService.CurrentLanguage == "zh-CN";
            IsEnglishLanguage = LocalizationService.CurrentLanguage == "en-US";
            RefreshModeText();
            RefreshAdvancedVideoLabels();
            UpdateThemeStateTexts();
            UpdateCodecOptions();
            UpdateConversionOptionLists();
            UpdateAudioBitrateOptions();
            UpdateCompressionPresetOptions();
            var hardware = SelectedHardwareEncoderOption?.Value ?? "";
            HardwareEncoderOptions = CreateHardwareEncoderOptions(_availableVideoEncoders);
            SelectedHardwareEncoderOption = HardwareEncoderOptions.Find(option => option.Value == hardware) ?? HardwareEncoderOptions[0];
            RefreshGoalOptions();
            UpdateBitrateTexts();
            UpdateCrfText();
            UpdateTargetSizeTexts();
            UpdateSourceInfoTexts();
            UpdateFFmpegStatus();
            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateExifToolStatus();

            foreach (var tab in SourceTabs)
            {
                tab.Status = tab.StatusColor switch
                {
                    "Blue" => LocalizationService.T("SourceTabs.Processing"),
                    "Green" => LocalizationService.T("SourceTabs.Completed"),
                    "Red" => LocalizationService.T("SourceTabs.Failed"),
                    "Orange" => LocalizationService.T("Improve.Warning"),
                    _ => LocalizationService.T("SourceTabs.Pending")
                };
            }

            foreach (var task in BatchTasks)
            {
                task.Status = task.StatusColor switch
                {
                    "Blue" => LocalizationService.T("SourceTabs.Processing"),
                    "Green" => LocalizationService.T("SourceTabs.Completed"),
                    "Red" => LocalizationService.T("SourceTabs.Failed"),
                    _ => LocalizationService.T("SourceTabs.Pending")
                };
            }

            if (SourceTabs.Count > 0)
            {
                IsInputStatusVisible = true;
                BatchModeText = LocalizationService.Format("SourceTabs.Mode", SourceTabs.Count);
            }

            UpdateExecuteAllText();

            if (CurrentVideoInfo == null && string.IsNullOrWhiteSpace(CompressionSettings.InputPath))
            {
                EstimatedBitrateText = "";
                CommandText = LocalizationService.T("Command.SelectInput");
            }
            else if (IsImageMode)
            {
                UpdateImageEstimation();
                UpdateCommand();
            }
            else
            {
                UpdateBitrateWarningAndEstimation();
                UpdateCommand();
            }
        }

        private void RefreshModeText()
        {
            if (IsImageMode)
            {
                CurrentModeTitle = LocalizationService.T("Mode.Image");
                InputSourceTitle = LocalizationService.T("Image.InputSource");
                CompressionParamsTitle = LocalizationService.T("Image.CompressionParams");
                TargetSizeLabel = LocalizationService.T("Image.TargetSize");
                TargetSizeUnitText = SelectedImageTargetSizeUnit;
                IsImageTargetUnitSelectorVisible = true;
                AdvancedBitrateLabel = LocalizationService.CurrentLanguage == "en-US" ? "Quality" : "质量";
                FormatOptionLabel = LocalizationService.T("Image.FormatLabel");
            }
            else
            {
                CurrentModeTitle = IsWorkspaceVisible ? LocalizationService.T("Mode.Video") : LocalizationService.T("Mode.Choose");
                InputSourceTitle = LocalizationService.T("Main.InputSource");
                CompressionParamsTitle = LocalizationService.T("Main.CompressionParams");
                TargetSizeLabel = LocalizationService.T("Main.TargetSize");
                TargetSizeUnitText = "MB";
                IsImageTargetUnitSelectorVisible = false;
                AdvancedBitrateLabel = LocalizationService.T("Main.TargetBitrate");
                FormatOptionLabel = LocalizationService.T("Main.VideoFormat");
            }
        }

        private void RefreshAdvancedVideoLabels()
        {
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            HardwareEncoderLabel = isEnglish ? "Hardware" : "硬件加速";
            CrfQualityLabel = isEnglish ? "CRF" : "CRF 清晰度";
            EnableCrfLabel = isEnglish ? "CRF mode" : "启用 CRF";
        }

        private static ThemeVariant ParseThemeVariant(string? themeName)
        {
            return themeName switch
            {
                "Dark" => ThemeVariant.Dark,
                "Light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
        }

        private static string GetThemeName(ThemeVariant theme)
        {
            if (theme == ThemeVariant.Dark)
            {
                return "Dark";
            }

            if (theme == ThemeVariant.Light)
            {
                return "Light";
            }

            return "Default";
        }

        private void UpdateThemeStateTexts()
        {
            var current = GetThemeName(CurrentTheme);
            IsThemeSystem = current == "Default";
            IsThemeLight = current == "Light";
            IsThemeDarkManual = current == "Dark";

            ThemeSystemMenuText = BuildCheckedMenuText("Default", LocalizationService.T("Theme.System"));
            ThemeLightMenuText = BuildCheckedMenuText("Light", LocalizationService.T("Theme.Light"));
            ThemeDarkMenuText = BuildCheckedMenuText("Dark", LocalizationService.T("Theme.Dark"));
        }

        private string BuildCheckedMenuText(string themeName, string label)
        {
            return GetThemeName(CurrentTheme) == themeName ? $"● {label}" : $"  {label}";
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
            if (IsImageMode)
            {
                BitrateValueText = $"{Bitrate}%";
                BitrateSelectionText = LocalizationService.Format("Main.CurrentSelection", BitrateValueText);
                return;
            }

            BitrateValueText = $"{Bitrate}k";
            BitrateSelectionText = LocalizationService.Format("Main.CurrentSelection", BitrateValueText);
        }

        private void UpdateCrfText()
        {
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            var qualityText = Crf <= 18
                ? isEnglish ? "visually high" : "高清"
                : Crf <= 23
                    ? isEnglish ? "balanced" : "均衡"
                    : Crf <= 30
                        ? isEnglish ? "smaller file" : "更小体积"
                        : isEnglish ? "very small file" : "极小体积";

            CrfSelectionText = isEnglish
                ? $"CRF {Crf}: lower = clearer, larger"
                : $"CRF {Crf}（{qualityText}）；数值越低越清晰、体积越大";
        }

        private void UpdateTargetSizeTexts()
        {
            if (IsBatchMode)
            {
                var isEnglish = LocalizationService.CurrentLanguage == "en-US";
                TargetSizeLabel = isEnglish ? "Per-file ratio" : "单文件比例";
                TargetSizeUnitText = "%";
                IsImageTargetUnitSelectorVisible = false;
                TargetSizeValueText = $"{TargetSizeMB:0.0}%";
                TargetSizeSelectionText = isEnglish
                    ? $"Each file targets about {TargetSizeValueText} of its original size"
                    : $"每个文件按原始大小约 {TargetSizeValueText} 计算目标";
                return;
            }

            TargetSizeUnitText = IsImageMode ? SelectedImageTargetSizeUnit : "MB";
            TargetSizeLabel = IsImageMode ? LocalizationService.T("Image.TargetSize") : LocalizationService.T("Main.TargetSize");
            IsImageTargetUnitSelectorVisible = IsImageMode;
            TargetSizeValueText = IsImageMode
                ? $"{TargetSizeMB:0.0} {SelectedImageTargetSizeUnit}"
                : $"{TargetSizeMB:F1} MB";
            TargetSizeSelectionText = IsImageMode
                ? LocalizationService.Format("Main.CurrentSelection", TargetSizeValueText)
                : LocalizationService.Format("Main.CurrentSelection", TargetSizeValueText);
        }

        private void EstimateImageQualityFromTargetSize()
        {
            var sourceSizeKB = CurrentVideoInfo?.FileSize > 0 ? CurrentVideoInfo.FileSize / 1024.0 : TargetSizeSliderMaximum;
            var targetKB = ImageTargetDisplayValueToKB(TargetSizeMB);
            if (sourceSizeKB <= 0)
            {
                return;
            }

            var estimatedQuality = EstimateImageQualityFromRatioPercent(targetKB / sourceSizeKB * 100.0);
            _isSyncingCompressionValues = true;
            Bitrate = Math.Max(10, Math.Min(100, estimatedQuality));
            BitrateSliderValue = Bitrate;
            _isSyncingCompressionValues = false;
            CompressionSettings.ImageQuality = Bitrate;
            CompressionSettings.ImageTargetSizeKB = targetKB;
            UpdateBitrateTexts();
            UpdateImageEstimation();
        }

        private void UpdateImageEstimation()
        {
            EstimatedBitrateText = BuildImageEstimateText();
            EstimatedBitrateColor = "Green";
        }

        private string BuildImageEstimateText()
        {
            var outputFormat = (SelectedImageFormatOption?.Name ?? CompressionSettings.ImageOutputFormat).ToUpperInvariant();
            var sizeSuffix = EnableResolutionConversion && SelectedResolutionOption?.Value != "0"
                ? LocalizationService.Format("Image.EstimateSizeSuffix", SelectedResolutionOption?.Name ?? "")
                : "";
            var displayTarget = FormatImageEstimatedTotalSize();
            return LocalizationService.Format("Image.Estimate", displayTarget, outputFormat, sizeSuffix);
        }

        private string FormatImageEstimatedTotalSize()
        {
            var estimatedBytes = EstimateImageTotalOutputBytes();
            if (estimatedBytes > 0)
            {
                return VideoInfo.FormatFileSize(estimatedBytes);
            }

            return IsBatchMode
                ? $"{TargetSizeMB:0.0}%"
                : $"{TargetSizeMB:0.0} {SelectedImageTargetSizeUnit}";
        }

        private long EstimateImageTotalOutputBytes()
        {
            if (!IsImageMode)
            {
                return 0;
            }

            if (IsBatchMode)
            {
                var ratio = ClampImageRatioPercent(TargetSizeMB) / 100.0;
                return BatchTasks.Where(task => task.IsIncluded).Sum(task => (long)Math.Max(1, Math.Round(task.SourceBytes * ratio)));
            }

            var targetKB = CompressionSettings.ImageTargetSizeKB > 0
                ? CompressionSettings.ImageTargetSizeKB
                : ImageTargetDisplayValueToKB(TargetSizeMB);
            return (long)Math.Max(1, Math.Round(targetKB * 1024));
        }

        private void UpdateImageTargetFromQuality()
        {
            var ratioPercent = RoundTargetSize(RatioPercentFromImageQuality(Bitrate));

            _isSyncingCompressionValues = true;
            if (IsBatchMode)
            {
                TargetSizeMB = ratioPercent;
                TargetSizeSliderValue = ratioPercent;
                CompressionPercentage = (int)Math.Round(ratioPercent);
                CompressionSettings.CompressionPercentage = CompressionPercentage;
                CompressionSettings.TargetSizeMB = ratioPercent;
                CompressionSettings.ImageTargetSizeKB = ratioPercent;
            }
            else
            {
                var sourceSizeKB = CurrentVideoInfo?.FileSize > 0
                    ? CurrentVideoInfo.FileSize / 1024.0
                    : Math.Max(TargetSizeSliderMaximum, 1);
                var targetKB = Math.Max(1, sourceSizeKB * ratioPercent / 100.0);
                targetKB = Math.Min(targetKB, sourceSizeKB);
                CompressionSettings.ImageTargetSizeKB = targetKB;
                TargetSizeMB = RoundTargetSize(ImageTargetKBToDisplayValue(targetKB));
                TargetSizeSliderValue = TargetSizeMB;
            }
            _isSyncingCompressionValues = false;

            UpdateTargetSizeTexts();
        }

        private static int EstimateImageQualityFromRatioPercent(double ratioPercent)
        {
            return (int)Math.Round(Math.Sqrt(ClampImageRatioPercent(ratioPercent) / 100.0) * 100);
        }

        private static double RatioPercentFromImageQuality(int quality)
        {
            var clampedQuality = Math.Max(1, Math.Min(quality, 100));
            return ClampImageRatioPercent(clampedQuality * clampedQuality / 100.0);
        }

        private static double ClampImageRatioPercent(double ratioPercent)
        {
            return Math.Max(1, Math.Min(ratioPercent, 100));
        }

        private static double RoundTargetSize(double value)
        {
            return Math.Round(value, 1, MidpointRounding.AwayFromZero);
        }

        private double ImageTargetDisplayValueToKB(double value)
        {
            return string.Equals(SelectedImageTargetSizeUnit, "MB", StringComparison.OrdinalIgnoreCase)
                ? value * 1024
                : value;
        }

        private double ImageTargetKBToDisplayValue(double valueKB)
        {
            return string.Equals(SelectedImageTargetSizeUnit, "MB", StringComparison.OrdinalIgnoreCase)
                ? valueKB / 1024
                : valueKB;
        }

        private void ConfigureImageTargetRange(double targetKB)
        {
            var sourceSizeKB = CurrentVideoInfo?.FileSize > 0
                ? CurrentVideoInfo.FileSize / 1024.0
                : Math.Max(targetKB, 1);
            var maxKB = Math.Max(sourceSizeKB, 1);
            var minKB = 1;

            CompressionSettings.ImageTargetSizeKB = Math.Max(minKB, Math.Min(targetKB, maxKB));
            TargetSizeSliderMinimum = ImageTargetKBToDisplayValue(minKB);
            TargetSizeSliderMaximum = ImageTargetKBToDisplayValue(maxKB);
            TargetSizeMB = RoundTargetSize(ImageTargetKBToDisplayValue(CompressionSettings.ImageTargetSizeKB));
            TargetSizeSliderValue = TargetSizeMB;
            TargetSizeUnitText = SelectedImageTargetSizeUnit;
        }

        private void UpdateSourceInfoTexts()
        {
            if (CurrentVideoInfo == null)
            {
                SourceInfoTitle = IsImageMode ? LocalizationService.T("Image.SourceInfo") : LocalizationService.T("Main.SourceInfo");
                SourceSizeLabel = LocalizationService.T("Main.Size");
                SourceSecondMetricLabel = IsImageMode ? LocalizationService.T("Image.Format") : LocalizationService.T("Main.Duration");
                SourceSecondMetricValue = "";
                SourceThirdMetricLabel = IsImageMode ? LocalizationService.T("Image.Resolution") : LocalizationService.T("Main.OriginalBitrate");
                SourceThirdMetricValue = "";
                SourceBadgeText = "";
                IsSourceBadgeVisible = false;
                SourceMetadataText = "";
                IsMetadataPreviewVisible = false;
                return;
            }

            SourceInfoTitle = IsImageMode ? LocalizationService.T("Image.SourceInfo") : LocalizationService.T("Main.SourceInfo");
            SourceSizeLabel = LocalizationService.T("Main.Size");
            SourceBadgeText = CurrentVideoInfo.Resolution;
            IsSourceBadgeVisible = !string.IsNullOrWhiteSpace(SourceBadgeText);
            UpdateMetadataPreviewText();

            if (IsImageMode)
            {
                SourceSecondMetricLabel = LocalizationService.T("Image.Format");
                SourceSecondMetricValue = ProcessingResultFormatter.GetDisplayExtension(CurrentVideoInfo.FilePath);
                SourceThirdMetricLabel = LocalizationService.T("Image.Resolution");
                SourceThirdMetricValue = string.IsNullOrWhiteSpace(CurrentVideoInfo.Resolution)
                    ? LocalizationService.T("Result.Unknown")
                    : CurrentVideoInfo.Resolution;
                return;
            }

            SourceSecondMetricLabel = LocalizationService.T("Main.Duration");
            SourceSecondMetricValue = CurrentVideoInfo.FormattedDuration;
            SourceThirdMetricLabel = LocalizationService.T("Main.OriginalBitrate");
            SourceThirdMetricValue = CurrentVideoInfo.Bitrate > 0
                ? $"{CurrentVideoInfo.Bitrate} kbps"
                : LocalizationService.T("Result.Unknown");
        }

        private void UpdateMetadataPreviewText()
        {
            if (CurrentVideoInfo?.HasMetadata == true)
            {
                SourceMetadataText = CurrentVideoInfo.MetadataSummary;
                return;
            }

            SourceMetadataText = LocalizationService.CurrentLanguage == "en-US"
                ? "None"
                : "无";
        }

        private string BuildOutputLabel(CompressionSettings settings)
        {
            if (settings.IsImageProcessing && settings.EnableFormatConversion && IsIconFormat(settings.ImageOutputFormat))
            {
                return $"{settings.ImageOutputFormat}_{settings.IconSizesCsv.Replace(',', '-')}";
            }

            if (settings.UseCrf && !settings.IsImageProcessing)
            {
                return settings.EnableTrim ? $"clip_crf{settings.Crf}" : $"crf{settings.Crf}";
            }

            if (IsBatchMode)
            {
                return $"ratio{TargetSizeMB:0.0}pct";
            }

            if (settings.IsImageProcessing)
            {
                return settings.ImageTargetSizeKB > 0
                    ? $"{settings.ImageTargetSizeKB:0.##}KB"
                    : $"q{settings.ImageQuality}";
            }

            var label = settings.TargetSizeMB > 0
                ? $"{settings.TargetSizeMB:0.##}MB"
                : $"{settings.CompressionPercentage}pct";
            return settings.EnableTrim ? $"clip_{label}" : label;
        }

        private void ClearLastFailure()
        {
            IsFailureActionsVisible = false;
            LastFailureDetails = "";
            LastFailureCommand = "";
        }

        private enum ExecutionScope
        {
            Current,
            All
        }


        private bool FileOrFolderExists() => File.Exists(CompressionSettings.InputPath) || Directory.Exists(CompressionSettings.InputPath);

        public override void Dispose()
        {
            PersistWorkspace();
            _saveTimer.Stop();
            _queueTimer.Stop();
            _sliderTimer.Stop();
            _searchTimer.Stop();
            foreach (var task in _observedQueueTasks) task.PropertyChanged -= OnQueueTaskChanged;
            _observedQueueTasks.Clear();
            _scanCancellation?.Cancel();
            _inputCancellation?.Cancel();
            _previewCancellation?.Cancel();
            _executionCancellation?.Cancel();
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            base.Dispose();
        }
    }
}
