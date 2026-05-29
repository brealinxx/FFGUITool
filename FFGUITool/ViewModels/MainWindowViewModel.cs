using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        private readonly IDialogService _dialogService;
        private readonly AppConfig _appConfig;
        private const string ReleasesUrl = "https://github.com/brealinxx/FFGUITool/releases";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/brealinxx/FFGUITool/releases/latest";
        private bool _isSyncingCompressionValues;
        private bool _isSyncingConversionToggles;
        private VideoInfo? _batchPreviewInfo;
        private string _batchPreviewInfoPath = "";
        private IReadOnlySet<string> _availableVideoEncoders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        private int _batchFileCount;

        [ObservableProperty]
        private string _batchModeText = "";

        [ObservableProperty]
        private bool _canUseVideoConversionTools;

        [ObservableProperty]
        private bool _canEditVideoConversionTools;

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

        #endregion

        #region 命令

        [RelayCommand]
        private void SelectVideoMode()
        {
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
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        [RelayCommand]
        private void SelectImageMode()
        {
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
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        [RelayCommand]
        private void BackToModeSelection()
        {
            IsModeSelectionVisible = true;
            IsWorkspaceVisible = false;
            ResetSelectedInput();
        }

        [RelayCommand]
        private async Task SelectFile()
        {
            var file = await _dialogService.OpenFileDialog(IsImageMode ? LocalizationService.T("Image.SelectFile") : LocalizationService.T("Picker.SelectVideo"), IsImageMode ? new[]
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

            if (file != null)
            {
                await ProcessSelectedInput(file.Path.LocalPath);
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

        [RelayCommand]
        private async Task Execute()
        {
            if (IsProcessing || !_ffmpegManager.IsFFmpegAvailable) return;

            IsProcessing = true;
            CanExecute = false;
            IsProgressVisible = true;

            try
            {
                ClearLastFailure();
                var summary = await ExecuteFFmpegCommand();
                var message = BuildCompletionMessage(summary);
                SystemNotificationService.Show(LocalizationService.T(IsImageMode ? "Result.ImageComplete" : "Result.VideoComplete"), message);
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Done"), message);
            }
            catch (FFmpegExecutionException ex)
            {
                CaptureFailure(ex);
                var message = BuildFailureMessage(ex);
                SystemNotificationService.Show(LocalizationService.T("Dialog.Error"), message, isError: true);
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), message);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unexpected execution error.", ex);
                LastFailureDetails = ex.ToString();
                LastFailureCommand = CommandText;
                IsFailureActionsVisible = true;
                var message = LocalizationService.Format("Dialog.ExecuteError", ex.Message);
                SystemNotificationService.Show(LocalizationService.T("Dialog.Error"), message, isError: true);
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
            SetTheme(IsThemeDark ? "Light" : "Dark");
        }

        [RelayCommand]
        private void SetTheme(string themeName)
        {
            CurrentTheme = themeName switch
            {
                "Dark" => ThemeVariant.Dark,
                "Light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = CurrentTheme;
                IsThemeDark = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
            }

            _appConfig.Theme = GetThemeName(CurrentTheme);
            AppConfigService.Save(_appConfig);
            UpdateThemeStateTexts();
        }

        [RelayCommand]
        private void SetLanguage(string languageCode)
        {
            _appConfig.Language = languageCode;
            LocalizationService.SetLanguage(languageCode);
        }

        [RelayCommand]
        private async Task CopyCommandText()
        {
            await SetClipboardText(CommandText);
        }

        [RelayCommand]
        private async Task CopyErrorDetails()
        {
            await SetClipboardText(LastFailureDetails);
        }

        [RelayCommand]
        private async Task CopyFullCommand()
        {
            await SetClipboardText(LastFailureCommand);
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            OpenFolderInShell(AppLogger.LogDirectory);
        }

        private static async Task SetClipboardText(string text)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.Clipboard == null)
            {
                return;
            }

            await desktop.MainWindow.Clipboard.SetTextAsync(text);
        }

        private static void OpenFolderInShell(string folderPath)
        {
            try
            {
                Directory.CreateDirectory(folderPath);
                var processInfo = new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                };
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to open folder: {folderPath}", ex);
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to open URL: {url}", ex);
            }
        }

        private static string GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var informationalVersion = assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()
                ?.InformationalVersion;

            return string.IsNullOrWhiteSpace(informationalVersion)
                ? assembly.GetName().Version?.ToString() ?? "1.0.0"
                : informationalVersion;
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            return Version.TryParse(latestVersion.Split('+')[0], out var latest) &&
                   Version.TryParse(currentVersion.Split('+')[0], out var current) &&
                   latest > current;
        }

        [RelayCommand]
        private async Task ShowFFmpegSettings()
        {
            await ShowSetupWindow(0);
        }

        private async Task ShowSetupWindow(int selectedTabIndex)
        {
            var setupViewModel = new SetupWindowViewModel(_ffmpegManager, _exifToolManager);
            setupViewModel.SelectedSetupTabIndex = selectedTabIndex;
            var setupWindow = new Views.SetupWindow
            {
                DataContext = setupViewModel
            };

            var mainWindow = _dialogService.GetMainWindow();
            if (mainWindow != null)
            {
                await setupWindow.ShowDialog(mainWindow);
                await InitializeExifTool();
                if (CurrentVideoInfo != null)
                {
                    await RefreshCurrentInputMetadata();
                }

                UpdateConversionOptionVisibility();
                UpdateCommand();

                if (setupViewModel.SetupCompleted)
                {
                    await _ffmpegManager.InitializeAsync();
                    UpdateFFmpegStatus();
                    await RefreshHardwareEncoderOptions();
                    await _dialogService.ShowMessage(LocalizationService.T("Dialog.Success"), LocalizationService.T("Dialog.FFmpegConfigUpdated"));
                }
            }
        }

        [RelayCommand]
        private async Task ConfigureExifTool()
        {
            await ShowSetupWindow(1);
        }

        [RelayCommand]
        private async Task RedetectFFmpeg()
        {
            FfmpegStatusText = LocalizationService.T("Status.Redetecting");
            FfmpegStatusColor = "Gray";

            await _ffmpegManager.InitializeAsync();
            UpdateFFmpegStatus();
            await InitializeExifTool();
            await RefreshHardwareEncoderOptions();

            var ffmpegStatus = _ffmpegManager.IsFFmpegAvailable
                ? LocalizationService.T("Status.Ready")
                : LocalizationService.T("Status.NotConfigured");
            var exifToolStatus = _exifToolManager.IsExifToolAvailable
                ? LocalizationService.T("ExifTool.Ready")
                : LocalizationService.T("ExifTool.NotConfigured");
            var message = LocalizationService.Format("Dialog.RedetectToolsResult", ffmpegStatus.Trim(), exifToolStatus);

            await _dialogService.ShowMessage(LocalizationService.T("Dialog.DetectComplete"), message);
        }

        [RelayCommand]
        private async Task ShowAbout()
        {
            var version = GetCurrentVersion();
            var ffmpegVersion = await _ffmpegManager.GetFFmpegVersion();
            var exifToolVersion = await _exifToolManager.GetExifToolVersion();

            var message = LocalizationService.Format("Dialog.AboutMessage", version, ffmpegVersion, exifToolVersion)
                          + $"{Environment.NewLine}{Environment.NewLine}{LocalizationService.Format("Update.Releases", ReleasesUrl)}";

            await _dialogService.ShowMessage(LocalizationService.T("Dialog.AboutTitle"), message);
        }

        [RelayCommand]
        private void OpenReleases()
        {
            OpenUrl(ReleasesUrl);
        }

        [RelayCommand]
        private async Task CheckForUpdates()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FFGUITool");
                using var response = await client.GetAsync(LatestReleaseApiUrl);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);
                var tag = document.RootElement.TryGetProperty("tag_name", out var tagElement)
                    ? tagElement.GetString() ?? ""
                    : "";
                var latestVersion = tag.Trim().TrimStart('v', 'V');
                var currentVersion = GetCurrentVersion();

                var message = IsNewerVersion(latestVersion, currentVersion)
                    ? LocalizationService.Format("Update.NewVersion", latestVersion, ReleasesUrl)
                    : LocalizationService.Format("Update.Latest", currentVersion, ReleasesUrl);
                await _dialogService.ShowMessage(LocalizationService.T("Update.Title"), message);
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Update check failed: {ex.Message}");
                await _dialogService.ShowMessage(
                    LocalizationService.T("Update.Title"),
                    LocalizationService.Format("Update.Unavailable", ReleasesUrl));
            }
        }

        [RelayCommand]
        private async Task CleanupLocalData()
        {
            var confirmed = await _dialogService.ShowConfirmation(
                LocalizationService.T("Cleanup.Title"),
                LocalizationService.Format("Cleanup.ConfirmMessage", LocalDataCleanupService.CleanupTargetDescription));

            if (!confirmed)
            {
                return;
            }

            LocalDataCleanupService.DeleteLocalDataAndRegistry();
            await _dialogService.ShowMessage(
                LocalizationService.T("Dialog.Done"),
                LocalizationService.T("Cleanup.Done"));
        }

        [RelayCommand]
        private void OpenConfigFolder()
        {
            LocalDataCleanupService.OpenConfigFolder();
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

        [RelayCommand]
        private async Task ShowHardwareAccelerationHelp()
        {
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            var title = isEnglish ? "Hardware Acceleration" : "硬件加速说明";
            var message = isEnglish
                ? "Hardware encoders can be much faster and reduce CPU load, especially for long videos. They are not always clearer or smaller than software encoders at the same setting, and availability depends on your GPU, driver, and FFmpeg build.\n\nAuto recommended only picks a likely supported encoder. If output quality or compatibility is not ideal, switch back to Off/software encoding."
                : "硬件编码通常更快，也能降低 CPU 占用，尤其适合长视频。但它不一定比软件编码更清晰或更省体积，效果取决于显卡、驱动和当前 FFmpeg 构建。\n\n自动推荐只会选择一个较可能可用的编码器。如果输出质量、体积或兼容性不理想，可以切回“关闭”使用软件编码。";

            await _dialogService.ShowMessage(title, message);
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
            _exifToolManager = new ExifToolManager();
            _videoAnalyzer = new VideoAnalyzer(_ffmpegManager);
            _commandBuilder = new CommandBuilder();
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
            SelectedResolutionOption = ResolutionOptions[3];
            SelectedImageFormatOption = ImageFormatOptions[0];
            RefreshAdvancedVideoLabels();
            UpdateThemeStateTexts();
            UpdateBitrateTexts();
            UpdateCrfText();

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

        #endregion

        #region 私有方法

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedImageFormatOption):
                    OnImageFormatChanged();
                    break;
                case nameof(SelectedImageTargetSizeUnit):
                    OnImageTargetSizeUnitChanged();
                    break;
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
                    OnCrfSliderChanged();
                    break;
                case nameof(IsThemeDark):
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

        private void OnCompressionPercentageChanged()
        {
            CompressionSettings.CompressionPercentage = CompressionPercentage;
            if (IsBatchMode)
            {
                return;
            }

            CalculateOptimalBitrate();
        }

        private void OnTargetSizeChanged()
        {
            if (_isSyncingCompressionValues)
            {
                return;
            }

            if (IsBatchMode)
            {
                TargetSizeMB = RoundTargetSize(ClampImageRatioPercent(TargetSizeMB));
                TargetSizeSliderValue = TargetSizeMB;
                CompressionPercentage = (int)Math.Round(TargetSizeMB);
                CompressionSettings.CompressionPercentage = CompressionPercentage;
                CompressionSettings.TargetSizeMB = TargetSizeMB;
                CompressionSettings.ImageTargetSizeKB = TargetSizeMB;
                if (IsImageMode)
                {
                    UpdateImageBatchQualityFromRatio();
                }
                UpdateTargetSizeTexts();
                RefreshBatchModeSummary();
                UpdateCommand();
                return;
            }

            if (IsImageMode)
            {
                TargetSizeMB = RoundTargetSize(ClampTargetSize(TargetSizeMB));
                TargetSizeSliderValue = TargetSizeMB;
                CompressionSettings.ImageTargetSizeKB = ImageTargetDisplayValueToKB(TargetSizeMB);
                EstimateImageQualityFromTargetSize();
                UpdateTargetSizeTexts();
                UpdateCommand();
                return;
            }

            if (CurrentVideoInfo != null)
            {
                TargetSizeMB = RoundTargetSize(ClampTargetSize(TargetSizeMB));
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
            TargetSizeMB = RoundTargetSize(TargetSizeSliderValue);
        }

        private void OnImageTargetSizeUnitChanged()
        {
            if (!IsImageMode)
            {
                return;
            }

            var targetKB = CompressionSettings.ImageTargetSizeKB > 0
                ? CompressionSettings.ImageTargetSizeKB
                : ImageTargetDisplayValueToKB(TargetSizeMB);

            _isSyncingCompressionValues = true;
            ConfigureImageTargetRange(targetKB);
            _isSyncingCompressionValues = false;
            UpdateTargetSizeTexts();
            UpdateImageEstimation();
            UpdateCommand();
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
            else if (IsBatchMode && SelectedCompressionPresetOption.Value == "none")
            {
                ConfigureBatchTargetRange();
            }

            CalculateBitrateFromTargetSize();
            UpdateBitrateWarningAndEstimation();
            RefreshBatchModeSummary();
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
            if (IsImageMode)
            {
                UpdateImageEstimation();
            }
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

        private void OnAudioBitrateChanged()
        {
            if (SelectedAudioBitrateOption != null && int.TryParse(SelectedAudioBitrateOption.Value, out var audioBitrate))
            {
                CompressionSettings.AudioBitrate = audioBitrate;
                UpdateBitrateWarningAndEstimation();
                UpdateCommand();
            }
        }

        private void OnImageFormatChanged()
        {
            if (SelectedImageFormatOption != null)
            {
                CompressionSettings.ImageOutputFormat = SelectedImageFormatOption.Value;
                UpdateImageEstimation();
                UpdateCommand();
            }
        }

        private void OnResolutionChanged()
        {
            if (SelectedResolutionOption != null && int.TryParse(SelectedResolutionOption.Value, out var height))
            {
                CompressionSettings.ResolutionHeight = height;
                if (IsImageMode)
                {
                    UpdateImageEstimation();
                }
                UpdateCommand();
            }
        }

        private void OnClearMetadataChanged()
        {
            if (ClearMetadata && !CanClearMetadata)
            {
                ClearMetadata = false;
                return;
            }

            CompressionSettings.ClearMetadata = ClearMetadata;
            IsMetadataPreviewVisible = IsMetadataClearOptionVisible && CanClearMetadata && ClearMetadata;
            UpdateMetadataPreviewText();
            UpdateCommand();
        }

        private async Task RefreshCurrentInputMetadata()
        {
            if (CurrentVideoInfo == null || string.IsNullOrWhiteSpace(CurrentVideoInfo.FilePath))
            {
                return;
            }

            CurrentVideoInfo.MetadataSummary = await _exifToolManager.ReadSensitiveMetadata(CurrentVideoInfo.FilePath);
            UpdateMetadataPreviewText();
        }

        private void OnBitrateChanged()
        {
            if (IsImageMode)
            {
                Bitrate = Math.Max(1, Math.Min(Bitrate, 100));
                CompressionSettings.ImageQuality = Bitrate;
                BitrateSliderValue = Bitrate;
                if (!_isSyncingCompressionValues)
                {
                    UpdateImageTargetFromQuality();
                }
                UpdateBitrateTexts();
                UpdateImageEstimation();
                RefreshBatchModeSummary();
                UpdateCommand();
                return;
            }

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

        private void OnHardwareEncoderChanged()
        {
            var selectedValue = SelectedHardwareEncoderOption?.Value ?? "";
            CompressionSettings.HardwareEncoder = selectedValue == "auto"
                ? RecommendHardwareEncoder()
                : selectedValue;
            UpdateCommand();
        }

        private void OnUseCrfChanged()
        {
            CompressionSettings.UseCrf = UseCrf;
            UpdateControlEditability();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnCrfChanged()
        {
            Crf = Math.Max(0, Math.Min(Crf, 51));
            CompressionSettings.Crf = Crf;
            CrfSliderValue = Crf;
            UpdateCrfText();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnCrfSliderChanged()
        {
            Crf = (int)Math.Round(CrfSliderValue);
        }

        public async Task ProcessSelectedInput(string path)
        {
            CompressionSettings.InputPath = path;
            InputPathText = path;
            HasSelectedInput = true;
            IsBatchMode = Directory.Exists(path);

            if (IsImageMode)
            {
                await ProcessSelectedImageInput(path);
                return;
            }

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
                    await RefreshCurrentInputMetadata();
                    IsVideoInfoVisible = true;
                    UpdateSourceInfoTexts();
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

        private async Task ProcessSelectedImageInput(string path)
        {
            if (IsBatchMode)
            {
                BatchFileCount = GetBatchInputFiles().Count();
                BatchModeText = BatchFileCount == 0
                    ? LocalizationService.T("Image.NoSupportedFiles")
                    : LocalizationService.Format("Image.BatchMode", BatchFileCount);
                CurrentVideoInfo = null;
                IsVideoInfoVisible = false;
                ConfigureBatchTargetRange();
            }
            else
            {
                CurrentVideoInfo = await _videoAnalyzer.AnalyzeVideo(path);
                if (CurrentVideoInfo == null && File.Exists(path))
                {
                    CurrentVideoInfo = new VideoInfo
                    {
                        FilePath = path,
                        FileSize = new FileInfo(path).Length
                    };
                }

                await RefreshCurrentInputMetadata();

                IsVideoInfoVisible = CurrentVideoInfo != null;
                BatchModeText = "";
                var sourceSizeKB = CurrentVideoInfo?.FileSize > 0
                    ? CurrentVideoInfo.FileSize / 1024.0
                    : 1;
                var targetKB = Math.Max(1, sourceSizeKB);
                ConfigureImageTargetRange(targetKB);
            }

            IsSelectedAudioInput = false;
            CanUseVideoConversionTools = true;
            CompressionSettings.ImageQuality = Bitrate;
            EstimatedBitrateColor = "Green";
            UpdateSourceInfoTexts();
            UpdateTargetSizeTexts();
            UpdateBitrateTexts();
            UpdateImageEstimation();
            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateSourceInfoTexts();
            UpdateCommand();
        }

        private async Task ProcessSelectedFolder(string path)
        {
            var files = GetBatchInputFiles().ToList();
            if (files.Count == 0 && !IsImageMode)
            {
                var audioFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => IsAudioExtension(Path.GetExtension(file)))
                    .ToList();
                if (audioFiles.Count > 0)
                {
                    EnableAudioConversion = true;
                    files = GetBatchInputFiles().ToList();
                }
            }

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
                ConfigureBatchTargetRange();
                await PrimeBatchPreviewInfoAsync(files.FirstOrDefault());
                EstimatedBitrateText = LocalizationService.Format("Batch.Found", BatchFileCount);
                EstimatedBitrateColor = "Green";
                BatchModeText = LocalizationService.Format("Batch.Mode", BatchFileCount);
            }

            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateSourceInfoTexts();
            UpdateCommand();
        }

        private async Task PrimeBatchPreviewInfoAsync(string? firstFile)
        {
            _batchPreviewInfo = null;
            _batchPreviewInfoPath = "";

            if (string.IsNullOrWhiteSpace(firstFile) || !File.Exists(firstFile) || !IsVideoExtension(Path.GetExtension(firstFile)))
            {
                return;
            }

            _batchPreviewInfo = await _videoAnalyzer.AnalyzeVideo(firstFile);
            _batchPreviewInfoPath = _batchPreviewInfo == null ? "" : firstFile;
        }

        private void CalculateOptimalBitrate()
        {
            if (CurrentVideoInfo == null)
            {
                EstimatedBitrateText = "";
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

        private void ConfigureBatchTargetRange()
        {
            _isSyncingCompressionValues = true;
            TargetSizeSliderMinimum = 1;
            TargetSizeSliderMaximum = 100;
            TargetSizeMB = RoundTargetSize(ClampImageRatioPercent(CompressionPercentage));
            TargetSizeSliderValue = TargetSizeMB;
            IsImageTargetUnitSelectorVisible = false;
            _isSyncingCompressionValues = false;
            CompressionSettings.TargetSizeMB = TargetSizeMB;
            CompressionSettings.ImageTargetSizeKB = TargetSizeMB;
            if (IsImageMode)
            {
                UpdateImageBatchQualityFromRatio();
            }
            UpdateTargetSizeTexts();
        }

        private void UpdateImageBatchQualityFromRatio()
        {
            var estimatedQuality = EstimateImageQualityFromRatioPercent(TargetSizeMB);
            _isSyncingCompressionValues = true;
            Bitrate = Math.Max(1, Math.Min(100, estimatedQuality));
            BitrateSliderValue = Bitrate;
            _isSyncingCompressionValues = false;
            CompressionSettings.ImageQuality = Bitrate;
            UpdateBitrateTexts();
            UpdateImageEstimation();
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
                UseCrf = false;
                CompressionSettings.UseCrf = false;
                CompressionSettings.Crf = 23;
                Crf = 23;
                CompressionSettings.AudioBitrate = 96;
                CompressionSettings.MaxHeight = 0;
                CompressionSettings.MaxFramerate = 0;
                CompressionSettings.Codec = SelectedCodec;
                return;
            }

            CompressionSettings.Codec = preset.Codec;
            UseCrf = preset.UseCrf;
            CompressionSettings.UseCrf = preset.UseCrf;
            CompressionSettings.Crf = preset.Crf;
            Crf = preset.Crf;
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
            CompressionSettings.ClearMetadata = ClearMetadata;
            if (IsImageMode)
            {
                CompressionSettings.ImageQuality = Bitrate;
                CompressionSettings.ImageTargetSizeKB = ImageTargetDisplayValueToKB(TargetSizeMB);
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
                var firstFile = GetBatchInputFiles().FirstOrDefault();
                var previewInfo = !string.IsNullOrWhiteSpace(firstFile) && string.Equals(firstFile, _batchPreviewInfoPath, StringComparison.OrdinalIgnoreCase)
                    ? _batchPreviewInfo
                    : null;
                var firstCommand = firstFile == null
                    ? LocalizationService.T("Batch.Empty")
                    : _commandBuilder.BuildCommand(CreateSettingsForInput(firstFile, previewInfo)).BuildCommand();
                if (ClearMetadata && CanClearMetadata && firstFile != null && firstCommand != LocalizationService.T("Batch.Empty"))
                {
                    var firstOutput = _commandBuilder.BuildCommand(CreateSettingsForInput(firstFile, previewInfo)).OutputPath;
                    firstCommand = AppendExifToolPreview(firstCommand, firstOutput);
                }
                CommandText = BuildBatchCommandPreview(firstCommand);
            }
            else
            {
                var command = _commandBuilder.BuildCommand(CompressionSettings, CurrentVideoInfo);
                CommandText = ClearMetadata && CanClearMetadata
                    ? AppendExifToolPreview(command.BuildCommand(), command.OutputPath)
                    : command.BuildCommand();
            }
            
            CanExecute = CompressionSettings.IsValid && _ffmpegManager.IsFFmpegAvailable && (!IsBatchMode || BatchFileCount > 0);
        }

        private void UpdateConversionOptionVisibility()
        {
            IsAdvancedVideoControlsVisible = IsAdvancedMode && !IsImageMode;
            IsAdvancedQualityControlsVisible = IsAdvancedMode && (!IsImageMode || HasSelectedInput);
            IsMetadataClearOptionVisible = IsAdvancedMode && HasSelectedInput;
            IsMetadataPreviewVisible = IsMetadataClearOptionVisible && CanClearMetadata && ClearMetadata;
            CanEditVideoConversionTools = CanUseVideoConversionTools && !EnableAudioConversion;
            UpdateControlEditability();

            if (IsImageMode)
            {
                IsFormatConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableFormatConversion;
                IsAudioConversionOptionsVisible = false;
                IsResolutionConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableResolutionConversion;
                return;
            }

            IsFormatConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && !IsSelectedAudioInput && EnableFormatConversion;
            IsAudioConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && EnableAudioConversion;
            IsResolutionConversionOptionsVisible = IsAdvancedMode && HasSelectedInput && !IsSelectedAudioInput && EnableResolutionConversion;
        }

        private void UpdateControlEditability()
        {
            IsCrfControlsVisible = IsAdvancedMode && !IsImageMode && UseCrf;
            CanEditTargetSize = !UseCrf;
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

        private IEnumerable<string> GetBatchInputFiles()
        {
            if (!Directory.Exists(CompressionSettings.InputPath))
            {
                return Array.Empty<string>();
            }

            return MediaFileSupport.GetBatchInputFiles(CompressionSettings.InputPath, IsImageMode, EnableAudioConversion);
        }

        private void RefreshBatchModeSummary()
        {
            if (!IsBatchMode)
            {
                return;
            }

            BatchFileCount = GetBatchInputFiles().Count();
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            BatchModeText = IsImageMode
                ? BatchFileCount == 0
                    ? LocalizationService.T("Image.NoSupportedFiles")
                    : isEnglish
                        ? $"{LocalizationService.Format("Image.BatchMode", BatchFileCount)}; each file targets {TargetSizeMB:0.0}% of its original size"
                        : $"{LocalizationService.Format("Image.BatchMode", BatchFileCount)}；每个文件按 {TargetSizeMB:0.0}% 原始大小压缩"
                : BatchFileCount == 0
                    ? LocalizationService.T("Batch.Empty")
                    : isEnglish
                        ? $"{LocalizationService.Format("Batch.Mode", BatchFileCount)} Each file targets {TargetSizeMB:0.0}% of its original size."
                        : $"{LocalizationService.Format("Batch.Mode", BatchFileCount)} 每个文件按 {TargetSizeMB:0.0}% 原始大小压缩。";
            EstimatedBitrateText = IsImageMode
                ? BatchFileCount == 0
                    ? LocalizationService.T("Image.NoSupportedFiles")
                    : BuildImageEstimateText()
                : BatchFileCount == 0
                    ? LocalizationService.T("Estimate.NonVideo")
                    : LocalizationService.Format("Batch.Found", BatchFileCount);
            EstimatedBitrateColor = BatchFileCount == 0 ? "Gray" : "Green";
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

        private async void OnLanguageChanged(object? sender, EventArgs e)
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
            await RefreshHardwareEncoderOptions();
            UpdateBitrateTexts();
            UpdateCrfText();
            UpdateTargetSizeTexts();
            UpdateSourceInfoTexts();
            UpdateFFmpegStatus();
            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateExifToolStatus();

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

        private void UpdateConversionOptionLists()
        {
            UpdateVideoFormatOptions();
            UpdateAudioFormatOptions();
            UpdateResolutionOptions();
            UpdateImageFormatOptions();
        }

        private void UpdateVideoFormatOptions()
        {
            var selectedValue = SelectedVideoFormatOption?.Value ?? CompressionSettings.OutputFormat;
            VideoFormatOptions = new List<CodecOption>
            {
                new("MP4", "mp4", LocalizationService.T("VideoFormat.MP4.Desc")),
                new("MKV", "mkv", LocalizationService.T("VideoFormat.MKV.Desc")),
                new("WebM", "webm", LocalizationService.T("VideoFormat.WebM.Desc")),
                new("MOV", "mov", LocalizationService.T("VideoFormat.MOV.Desc")),
                new("AVI", "avi", LocalizationService.T("VideoFormat.AVI.Desc")),
                new("GIF", "gif", LocalizationService.T("VideoFormat.GIF.Desc"))
            };
            SelectedVideoFormatOption = VideoFormatOptions.Find(option => option.Value == selectedValue) ?? VideoFormatOptions[0];
        }

        private void UpdateAudioFormatOptions()
        {
            var selectedValue = SelectedAudioFormatOption?.Value ?? CompressionSettings.AudioOutputFormat;
            AudioFormatOptions = new List<CodecOption>
            {
                new("MP3", "mp3", LocalizationService.T("AudioFormat.MP3.Desc")),
                new("AAC", "aac", LocalizationService.T("AudioFormat.AAC.Desc")),
                new("M4A", "m4a", LocalizationService.T("AudioFormat.M4A.Desc")),
                new("WAV", "wav", LocalizationService.T("AudioFormat.WAV.Desc")),
                new("FLAC", "flac", LocalizationService.T("AudioFormat.FLAC.Desc")),
                new("OGG", "ogg", LocalizationService.T("AudioFormat.OGG.Desc"))
            };
            SelectedAudioFormatOption = AudioFormatOptions.Find(option => option.Value == selectedValue) ?? AudioFormatOptions[0];
        }

        private void UpdateResolutionOptions()
        {
            var selectedValue = SelectedResolutionOption?.Value ?? CompressionSettings.ResolutionHeight.ToString();
            ResolutionOptions = new List<CodecOption>
            {
                new(LocalizationService.T("Resolution.Original"), "0", LocalizationService.T("Resolution.Original.Desc")),
                new("2160p", "2160", "4K"),
                new("1080p", "1080", LocalizationService.T("Resolution.1080.Desc")),
                new("720p", "720", LocalizationService.T("Resolution.720.Desc")),
                new("480p", "480", LocalizationService.T("Resolution.480.Desc")),
                new("512px", "512", LocalizationService.T("Resolution.512.Desc")),
                new("360p", "360", LocalizationService.T("Resolution.360.Desc"))
            };
            SelectedResolutionOption = ResolutionOptions.Find(option => option.Value == selectedValue) ?? ResolutionOptions[3];
        }

        private void UpdateImageFormatOptions()
        {
            var selectedValue = SelectedImageFormatOption?.Value ?? CompressionSettings.ImageOutputFormat;
            var options = new List<CodecOption>
            {
                new("JPG", "jpg", LocalizationService.T("ImageFormat.JPG.Desc")),
                new("PNG", "png", LocalizationService.T("ImageFormat.PNG.Desc"))
            };

            if (IsEncoderAvailableOrUnknown("libwebp"))
            {
                options.Add(new("WebP", "webp", LocalizationService.T("ImageFormat.WebP.Desc")));
            }

            ImageFormatOptions = options;
            SelectedImageFormatOption = ImageFormatOptions.Find(option => option.Value == selectedValue) ?? ImageFormatOptions[0];
        }

        private void UpdateCodecOptions()
        {
            var selectedValue = SelectedCodecOption?.Value ?? SelectedCodec;
            var candidates = new List<CodecOption>
            {
                new("H.264 (libx264)", "libx264", LocalizationService.T("Codec.H264.Desc")),
                new("H.265 (libx265)", "libx265", LocalizationService.T("Codec.H265.Desc")),
                new("VP9 (libvpx-vp9)", "libvpx-vp9", LocalizationService.T("Codec.VP9.Desc")),
                new("AV1 (libaom-av1)", "libaom-av1", LocalizationService.T("Codec.AV1.Desc"))
            };

            CodecOptions = candidates
                .Where(option => IsEncoderAvailableOrUnknown(option.Value))
                .ToList();

            if (CodecOptions.Count == 0)
            {
                CodecOptions = candidates;
            }

            SelectedCodecOption = CodecOptions.Find(option => option.Value == selectedValue) ?? CodecOptions[0];
        }

        private void UpdateAudioBitrateOptions()
        {
            var selectedValue = SelectedAudioBitrateOption?.Value ?? CompressionSettings.AudioBitrate.ToString();
            AudioBitrateOptions = new List<CodecOption>
            {
                new("320 kb/s", "320", LocalizationService.T("AudioBitrate.320.Desc")),
                new("256 kb/s", "256", LocalizationService.T("AudioBitrate.256.Desc")),
                new("128 kb/s", "128", LocalizationService.T("AudioBitrate.128.Desc")),
                new("96 kb/s", "96", LocalizationService.T("AudioBitrate.96.Desc")),
                new("64 kb/s", "64", LocalizationService.T("AudioBitrate.64.Desc")),
                new("8 kb/s", "8", LocalizationService.T("AudioBitrate.8.Desc"))
            };
            SelectedAudioBitrateOption = AudioBitrateOptions.Find(option => option.Value == selectedValue)
                                         ?? AudioBitrateOptions[^1];
        }

        private async Task RefreshHardwareEncoderOptions()
        {
            var selectedValue = SelectedHardwareEncoderOption?.Value ?? "";
            _availableVideoEncoders = await _ffmpegManager.GetAvailableVideoEncoders();
            UpdateCodecOptions();
            UpdateImageFormatOptions();
            HardwareEncoderOptions = CreateHardwareEncoderOptions(_availableVideoEncoders);
            SelectedHardwareEncoderOption = HardwareEncoderOptions.Find(option => option.Value == selectedValue)
                ?? HardwareEncoderOptions[0];
            CompressionSettings.HardwareEncoder = SelectedHardwareEncoderOption.Value == "auto"
                ? RecommendHardwareEncoder()
                : SelectedHardwareEncoderOption.Value;
        }

        private bool IsEncoderAvailableOrUnknown(string encoder)
        {
            return !_ffmpegManager.IsFFmpegAvailable ||
                   _availableVideoEncoders.Count == 0 ||
                   _availableVideoEncoders.Contains(encoder);
        }

        private List<CodecOption> CreateHardwareEncoderOptions(IReadOnlySet<string> availableEncoders)
        {
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            var options = new List<CodecOption>
            {
                new(isEnglish ? "Off" : "关闭", "", isEnglish ? "Software" : "软件编码"),
                new(isEnglish ? "Auto" : "自动推荐", "auto", isEnglish ? "Recommended" : "自动选择")
            };

            var candidates = new (string Encoder, string Name, string Description)[]
            {
                ("h264_nvenc", "NVIDIA H.264 (NVENC)", "NVIDIA GPU"),
                ("hevc_nvenc", "NVIDIA H.265 (NVENC)", "NVIDIA GPU"),
                ("h264_qsv", "Intel H.264 (QSV)", "Intel Quick Sync"),
                ("hevc_qsv", "Intel H.265 (QSV)", "Intel Quick Sync"),
                ("h264_amf", "AMD H.264 (AMF)", "AMD GPU"),
                ("hevc_amf", "AMD H.265 (AMF)", "AMD GPU"),
                ("h264_videotoolbox", "Apple VideoToolbox H.264", "Apple"),
                ("hevc_videotoolbox", "Apple VideoToolbox H.265", "Apple"),
                ("h264_vaapi", "VAAPI H.264", "Linux VAAPI"),
                ("hevc_vaapi", "VAAPI H.265", "Linux VAAPI")
            };

            foreach (var candidate in candidates)
            {
                if (availableEncoders.Contains(candidate.Encoder) || IsAppleVideoToolboxCandidate(candidate.Encoder))
                {
                    options.Add(new CodecOption(candidate.Name, candidate.Encoder, candidate.Description));
                }
            }

            if (options.Count == 2)
            {
                options[0].Description = isEnglish
                    ? "No hardware encoder reported by current FFmpeg"
                    : "当前 FFmpeg 未报告可用硬件编码器";
            }

            return options;
        }

        private static bool IsAppleVideoToolboxCandidate(string encoder)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                   && encoder.EndsWith("_videotoolbox", StringComparison.OrdinalIgnoreCase);
        }

        private string RecommendHardwareEncoder()
        {
            var availableEncoders = HardwareEncoderOptions
                .Select(option => option.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "auto")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return PickFirstAvailable(availableEncoders, "hevc_videotoolbox", "h264_videotoolbox");
            }

            var gpuText = GetLocalGpuText();
            if (gpuText.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                return PickFirstAvailable(availableEncoders, "hevc_nvenc", "h264_nvenc");
            }

            if (gpuText.Contains("AMD", StringComparison.OrdinalIgnoreCase) || gpuText.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            {
                return PickFirstAvailable(availableEncoders, "hevc_amf", "h264_amf");
            }

            if (gpuText.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                return PickFirstAvailable(availableEncoders, "hevc_qsv", "h264_qsv");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return PickFirstAvailable(availableEncoders, "hevc_vaapi", "h264_vaapi");
            }

            return PickFirstAvailable(
                availableEncoders,
                "hevc_nvenc",
                "h264_nvenc",
                "hevc_qsv",
                "h264_qsv",
                "hevc_amf",
                "h264_amf",
                "hevc_videotoolbox",
                "h264_videotoolbox",
                "hevc_vaapi",
                "h264_vaapi");
        }

        private static string PickFirstAvailable(IReadOnlySet<string> availableEncoders, params string[] candidates)
        {
            return candidates.FirstOrDefault(availableEncoders.Contains) ?? "";
        }

        private static string GetLocalGpuText()
        {
            var nvidiaInfo = TryReadProcessOutput("nvidia-smi", "--query-gpu=name --format=csv,noheader");
            if (!string.IsNullOrWhiteSpace(nvidiaInfo))
            {
                return nvidiaInfo;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return TryReadProcessOutput("wmic", "path win32_VideoController get name");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return TryReadProcessOutput("system_profiler", "SPDisplaysDataType");
            }

            return TryReadProcessOutput("lspci", "");
        }

        private static string TryReadProcessOutput(string fileName, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();
                if (!process.WaitForExit(1500))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }

                return process.StandardOutput.ReadToEnd();
            }
            catch
            {
                return "";
            }
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
                return GetBatchInputFiles()
                    .Where(File.Exists)
                    .Sum(file => (long)Math.Max(1, Math.Round(new FileInfo(file).Length * ratio)));
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

        private void ResetSelectedInput()
        {
            CompressionSettings.InputPath = "";
            InputPathText = "";
            CurrentVideoInfo = null;
            IsVideoInfoVisible = false;
            HasSelectedInput = false;
            IsBatchMode = false;
            BatchFileCount = 0;
            _batchPreviewInfo = null;
            _batchPreviewInfoPath = "";
            BatchModeText = "";
            CanExecute = false;
            CommandText = LocalizationService.T("Command.SelectInput");
            EstimatedBitrateText = "";
            EstimatedBitrateColor = "Gray";
            SourceMetadataText = "";
            IsMetadataPreviewVisible = false;
            UpdateSourceInfoTexts();
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
                SourceSecondMetricValue = GetDisplayExtension(CurrentVideoInfo.FilePath);
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
                    var inputInfo = await GetInputInfo(file);
                    var command = _commandBuilder.BuildCommand(CreateSettingsForInput(file, inputInfo));
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

        private CompressionSettings CreateSettingsForInput(string inputPath, VideoInfo? inputInfo = null)
        {
            var settings = new CompressionSettings
            {
                CompressionPercentage = CompressionSettings.CompressionPercentage,
                TargetSizeMB = CompressionSettings.TargetSizeMB,
                Bitrate = CompressionSettings.Bitrate,
                Codec = CompressionSettings.Codec,
                HardwareEncoder = CompressionSettings.HardwareEncoder,
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
                ClearMetadata = CompressionSettings.ClearMetadata,
                ResolutionHeight = CompressionSettings.ResolutionHeight,
                InputPath = inputPath,
                OutputPath = CompressionSettings.OutputPath,
                OutputLabel = CompressionSettings.OutputLabel,
                IsImageProcessing = CompressionSettings.IsImageProcessing,
                ImageQuality = CompressionSettings.ImageQuality,
                ImageTargetSizeKB = CompressionSettings.ImageTargetSizeKB,
                ImageOutputFormat = CompressionSettings.ImageOutputFormat
            };

            if (IsBatchMode)
            {
                ApplyPerFileBatchTarget(settings, inputInfo);
            }

            settings.ClearMetadata = CompressionSettings.ClearMetadata;
            return settings;
        }

        private void ApplyPerFileBatchTarget(CompressionSettings settings, VideoInfo? inputInfo)
        {
            var sourceBytes = inputInfo?.FileSize > 0
                ? inputInfo.FileSize
                : File.Exists(settings.InputPath)
                    ? new FileInfo(settings.InputPath).Length
                    : 0;
            var ratio = Math.Max(1, Math.Min(TargetSizeMB, 100)) / 100.0;

            if (settings.IsImageProcessing)
            {
                settings.ImageTargetSizeKB = sourceBytes > 0
                    ? Math.Max(1, sourceBytes / 1024.0 * ratio)
                    : Math.Max(1, TargetSizeMB);
                settings.OutputLabel = $"ratio{TargetSizeMB:0.0}pct";
                return;
            }

            settings.TargetSizeMB = sourceBytes > 0
                ? Math.Max(0.1, sourceBytes / 1024.0 / 1024.0 * ratio)
                : Math.Max(0.1, TargetSizeMB);
            settings.OutputLabel = settings.UseCrf ? $"crf{settings.Crf}" : $"ratio{TargetSizeMB:0.0}pct";

            if (!settings.UseCrf && inputInfo?.Duration > 0)
            {
                var targetBitrate = _commandBuilder.CalculateBitrateForTargetSize(inputInfo, settings.TargetSizeMB);
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

                settings.Bitrate = targetBitrate;
            }
        }

        private string BuildOutputLabel(CompressionSettings settings)
        {
            if (settings.UseCrf && !settings.IsImageProcessing)
            {
                return $"crf{settings.Crf}";
            }

            if (IsBatchMode)
            {
                return $"ratio{TargetSizeMB:0.0}pct";
            }

            if (settings.IsImageProcessing)
            {
                return settings.ImageTargetSizeKB > 0
                    ? $"{settings.ImageTargetSizeKB:F0}KB"
                    : $"q{settings.ImageQuality}";
            }

            return settings.TargetSizeMB > 0
                ? $"{settings.TargetSizeMB:F0}MB"
                : $"{settings.CompressionPercentage}pct";
        }

        private async Task<VideoInfo?> GetInputInfo(string inputPath)
        {
            var inputInfo = CurrentVideoInfo?.FilePath == inputPath
                ? CurrentVideoInfo
                : await _videoAnalyzer.AnalyzeVideo(inputPath);

            if (inputInfo == null && File.Exists(inputPath))
            {
                inputInfo = new VideoInfo
                {
                    FilePath = inputPath,
                    FileSize = new FileInfo(inputPath).Length
                };
            }

            return inputInfo;
        }

        private static long EstimateExpectedOutputBytes(FFmpegCommand command, VideoInfo? inputInfo)
        {
            if (command.ImageOutput && command.ImageTargetSizeKB > 0)
            {
                return (long)(command.ImageTargetSizeKB * 1024);
            }

            if (!command.UseCrf && command.Bitrate > 0 && inputInfo?.Duration > 0)
            {
                var totalKbps = command.AudioOnly
                    ? command.AudioBitrate
                    : command.Bitrate + command.AudioBitrate;
                return (long)(totalKbps * 1024.0 * inputInfo.Duration / 8.0);
            }

            return inputInfo?.FileSize > 0 ? inputInfo.FileSize : 0;
        }

        private async Task<ConversionResult> RunFFmpegCommand(FFmpegCommand command)
        {
            var inputInfo = await GetInputInfo(command.InputPath);
            var expectedOutputBytes = EstimateExpectedOutputBytes(command, inputInfo);
            var outputDiagnostics = OutputDiagnostics.Check(command, expectedOutputBytes);
            AppLogger.Info($"Output diagnostics before FFmpeg run:{Environment.NewLine}{outputDiagnostics.Summary}");
            if (!outputDiagnostics.CanWrite)
            {
                throw new FFmpegExecutionException(
                    "Output path check failed before running FFmpeg.",
                    1,
                    outputDiagnostics.Summary,
                    command.BuildCommand(),
                    command.OutputPath,
                    outputDiagnostics.Summary);
            }

            var output = command.ImageOutput && command.ImageTargetSizeKB > 0
                ? await RunImageCommandWithTargetSize(command, inputInfo)
                : await RunSingleFFmpegCommand(command);

            if (output.ExitCode != 0)
            {
                throw new FFmpegExecutionException(
                    $"FFmpeg执行失败，退出代码: {output.ExitCode}",
                    output.ExitCode,
                    output.Error,
                    command.BuildCommand(),
                    command.OutputPath,
                    outputDiagnostics.Summary);
            }

            if (CompressionSettings.ClearMetadata && _exifToolManager.IsExifToolAvailable)
            {
                var clearResult = await _exifToolManager.ClearMetadata(command.OutputPath);
                if (!clearResult.Success)
                {
                    throw new FFmpegExecutionException(
                        "ExifTool元数据清除失败",
                        1,
                        clearResult.Error,
                        command.BuildCommand(),
                        command.OutputPath,
                        outputDiagnostics.Summary);
                }
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

            return new ConversionResult(command.InputPath, command.OutputPath, command, inputInfo, outputInfo);
        }

        private async Task<(int ExitCode, string Error)> RunImageCommandWithTargetSize(FFmpegCommand command, VideoInfo? inputInfo)
        {
            var targetBytes = (long)Math.Max(1, command.ImageTargetSizeKB * 1024);
            var baseHeight = command.MaxHeight > 0 ? command.MaxHeight : TryParseHeight(inputInfo?.Resolution);
            var lastResult = (ExitCode: 0, Error: "");

            foreach (var quality in BuildImageQualityAttempts(command.ImageQuality))
            {
                command.ImageQuality = quality;
                lastResult = await RunSingleFFmpegCommand(command);
                if (lastResult.ExitCode != 0 || IsOutputWithinTarget(command.OutputPath, targetBytes))
                {
                    return lastResult;
                }
            }

            if (baseHeight <= 0)
            {
                return lastResult;
            }

            var currentHeight = Math.Max(128, (int)Math.Round(baseHeight * 0.85));
            while (currentHeight >= 128)
            {
                command.MaxHeight = currentHeight;
                foreach (var quality in new[] { 35, 25, 15, 8, 1 })
                {
                    command.ImageQuality = quality;
                    lastResult = await RunSingleFFmpegCommand(command);
                    if (lastResult.ExitCode != 0 || IsOutputWithinTarget(command.OutputPath, targetBytes))
                    {
                        return lastResult;
                    }
                }

                currentHeight = (int)Math.Round(currentHeight * 0.85);
            }

            return lastResult;
        }

        private async Task<(int ExitCode, string Error)> RunSingleFFmpegCommand(FFmpegCommand command)
        {
            if (File.Exists(command.OutputPath))
            {
                File.Delete(command.OutputPath);
            }

            var commandText = command.BuildCommand();
            AppLogger.Info($"Running FFmpeg command:{Environment.NewLine}{commandText}");
            var arguments = commandText.StartsWith("ffmpeg ", StringComparison.OrdinalIgnoreCase)
                ? commandText["ffmpeg ".Length..]
                : commandText;

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

            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            AppLogger.Info($"FFmpeg exited with code {process.ExitCode}.");
            var errorSummary = AppLogger.Summarize(error);
            if (!string.IsNullOrWhiteSpace(errorSummary))
            {
                AppLogger.Info($"FFmpeg stderr summary:{Environment.NewLine}{errorSummary}");
            }

            return (process.ExitCode, error);
        }

        private static IEnumerable<int> BuildImageQualityAttempts(int startQuality)
        {
            var quality = Math.Max(1, Math.Min(100, startQuality));
            for (var current = quality; current >= 1; current -= 8)
            {
                yield return current;
            }

            if (quality != 1)
            {
                yield return 1;
            }
        }

        private static bool IsOutputWithinTarget(string outputPath, long targetBytes)
        {
            return File.Exists(outputPath) && new FileInfo(outputPath).Length <= targetBytes;
        }

        private static int TryParseHeight(string? resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
            {
                return 0;
            }

            var parts = resolution.Split('x', 'X');
            return parts.Length == 2 && int.TryParse(parts[1], out var height) ? height : 0;
        }

        private static string BuildCompletionMessage(ConversionSummary summary)
        {
            var lines = new List<string>
            {
                summary.Results.Count > 1
                    ? LocalizationService.Format("Result.BatchComplete", summary.Results.Count)
                    : LocalizationService.T(summary.Results.FirstOrDefault()?.Command.ImageOutput == true ? "Result.ImageComplete" : "Result.VideoComplete"),
                LocalizationService.Format("Result.Elapsed", FormatDuration(summary.Elapsed))
            };

            foreach (var result in summary.Results)
            {
                lines.Add("");
                lines.Add(Path.GetFileName(result.InputPath));
                lines.Add(LocalizationService.Format("Result.Output", result.OutputPath));

                if (result.InputInfo != null && result.OutputInfo != null)
                {
                    lines.Add(LocalizationService.Format("Result.SizeCompare", FormatBytes(result.InputInfo.FileSize), FormatBytes(result.OutputInfo.FileSize), FormatSizeChange(result.InputInfo.FileSize, result.OutputInfo.FileSize)));

                    if (result.Command.ImageOutput)
                    {
                        lines.Add(LocalizationService.Format("Result.ImageFormat", GetDisplayExtension(result.InputPath), GetDisplayExtension(result.OutputPath)));
                        if (!string.IsNullOrWhiteSpace(result.InputInfo.Resolution) || !string.IsNullOrWhiteSpace(result.OutputInfo.Resolution))
                        {
                            lines.Add(LocalizationService.Format(
                                "Result.Resolution",
                                string.IsNullOrWhiteSpace(result.InputInfo.Resolution) ? LocalizationService.T("Result.Unknown") : result.InputInfo.Resolution,
                                string.IsNullOrWhiteSpace(result.OutputInfo.Resolution) ? LocalizationService.T("Result.Unknown") : result.OutputInfo.Resolution));
                        }

                        continue;
                    }

                    if (result.Command.AudioOnly)
                    {
                        lines.Add(LocalizationService.Format("Result.AudioFormat", GetDisplayExtension(result.InputPath), GetDisplayExtension(result.OutputPath)));
                        lines.Add(LocalizationService.Format("Result.AudioBitrate", $"{result.Command.AudioBitrate} kb/s"));
                        continue;
                    }

                    if (result.InputInfo.Bitrate > 0 || result.OutputInfo.Bitrate > 0)
                    {
                        lines.Add(LocalizationService.Format("Result.BitrateCompare", FormatBitrate(result.InputInfo.Bitrate), FormatBitrate(result.OutputInfo.Bitrate)));
                    }
                }
                else if (result.OutputInfo != null)
                {
                    lines.Add(LocalizationService.Format("Result.OutputSize", FormatBytes(result.OutputInfo.FileSize)));
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildFailureMessage(FFmpegExecutionException exception)
        {
            var lines = new List<string> { exception.Message };

            if (!string.IsNullOrWhiteSpace(exception.OutputDiagnostics))
            {
                lines.Add("");
                lines.Add("Diagnostics:");
                lines.Add(exception.OutputDiagnostics);
            }

            if (!string.IsNullOrWhiteSpace(exception.FFmpegOutput))
            {
                lines.Add("");
                lines.Add(LocalizationService.T("Result.FFmpegInfo"));
                lines.Add(TrimFFmpegOutput(exception.FFmpegOutput));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void CaptureFailure(FFmpegExecutionException exception)
        {
            LastFailureCommand = string.IsNullOrWhiteSpace(exception.CommandText)
                ? CommandText
                : exception.CommandText;
            LastFailureDetails = BuildFailureDetails(exception);
            IsFailureActionsVisible = true;
            AppLogger.Error($"FFmpeg execution failed. Details:{Environment.NewLine}{LastFailureDetails}", exception);
        }

        private void ClearLastFailure()
        {
            IsFailureActionsVisible = false;
            LastFailureDetails = "";
            LastFailureCommand = "";
        }

        private static string BuildFailureDetails(FFmpegExecutionException exception)
        {
            var lines = new List<string>
            {
                exception.Message,
                $"Exit code: {exception.ExitCode}"
            };

            if (!string.IsNullOrWhiteSpace(exception.CommandText))
            {
                lines.Add("");
                lines.Add("Command:");
                lines.Add(exception.CommandText);
            }

            if (!string.IsNullOrWhiteSpace(exception.OutputPath))
            {
                lines.Add("");
                lines.Add($"Output path: {exception.OutputPath}");
            }

            if (!string.IsNullOrWhiteSpace(exception.OutputDiagnostics))
            {
                lines.Add("");
                lines.Add("Diagnostics:");
                lines.Add(exception.OutputDiagnostics);
            }

            if (!string.IsNullOrWhiteSpace(exception.FFmpegOutput))
            {
                lines.Add("");
                lines.Add("stderr summary:");
                lines.Add(TrimFFmpegOutput(exception.FFmpegOutput));
                lines.Add("");
                lines.Add("stderr full:");
                lines.Add(exception.FFmpegOutput);
            }

            lines.Add("");
            lines.Add($"Log file: {AppLogger.CurrentLogPath}");
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
            return VideoInfo.FormatFileSize(bytes);
        }

        private static string FormatSizeChange(long before, long after)
        {
            if (before <= 0)
            {
                return LocalizationService.T("Result.ChangeUnavailable");
            }

            var percentage = (after - before) * 100.0 / before;
            return percentage <= 0
                ? LocalizationService.Format("Result.Reduced", Math.Abs(percentage))
                : LocalizationService.Format("Result.Increased", percentage);
        }

        private static string FormatBitrate(int bitrate)
        {
            return bitrate > 0 ? $"{bitrate} kb/s" : LocalizationService.T("Result.Unknown");
        }

        private static string GetDisplayExtension(string path)
        {
            var extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
            return string.IsNullOrWhiteSpace(extension) ? LocalizationService.T("Result.Unknown") : extension;
        }

        private sealed record ConversionSummary(List<ConversionResult> Results, TimeSpan Elapsed);

        private sealed record ConversionResult(
            string InputPath,
            string OutputPath,
            FFmpegCommand Command,
            VideoInfo? InputInfo,
            VideoInfo? OutputInfo);

        private sealed class FFmpegExecutionException : Exception
        {
            public FFmpegExecutionException(
                string message,
                int exitCode,
                string ffmpegOutput,
                string commandText = "",
                string outputPath = "",
                string outputDiagnostics = "")
                : base(message)
            {
                ExitCode = exitCode;
                FFmpegOutput = ffmpegOutput;
                CommandText = commandText;
                OutputPath = outputPath;
                OutputDiagnostics = outputDiagnostics;
            }

            public int ExitCode { get; }
            public string FFmpegOutput { get; }
            public string CommandText { get; }
            public string OutputPath { get; }
            public string OutputDiagnostics { get; }
        }

        #endregion

        public override void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            base.Dispose();
        }
    }
}
