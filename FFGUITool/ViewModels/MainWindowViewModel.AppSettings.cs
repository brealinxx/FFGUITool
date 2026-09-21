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

namespace FFGUITool.ViewModels;

public partial class MainWindowViewModel
{
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

            var config = AppConfigService.Load();
            config.Theme = GetThemeName(CurrentTheme);
            AppConfigService.Save(config);
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
        private async Task ShowAppPreferences()
        {
            if (_dialogService.GetMainWindow() is { } owner)
                await new Views.AppPreferencesWindow { DataContext = this }.ShowDialog(owner);
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



}
