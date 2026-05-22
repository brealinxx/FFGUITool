using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFGUITool.Services;
using Avalonia.Platform.Storage;

namespace FFGUITool.ViewModels
{
    /// <summary>
    /// FFmpeg设置窗口视图模型
    /// </summary>
    public partial class SetupWindowViewModel : ViewModelBase
    {
        private readonly FFmpegManager _ffmpegManager;
        private readonly ExifToolManager _exifToolManager;
        private readonly IDialogService _dialogService;

        #region 可观察属性

        [ObservableProperty]
        private string _ffmpegPathText = "";

        [ObservableProperty]
        private string _archivePathText = "";

        [ObservableProperty]
        private string _exifToolPathText = "";

        [ObservableProperty]
        private string _exifToolFolderText = "";

        [ObservableProperty]
        private string _exifToolArchivePathText = "";

        [ObservableProperty]
        private string _exifToolStatusText = "";

        [ObservableProperty]
        private string _statusText = "";

        [ObservableProperty]
        private bool _setupCompleted;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private int _selectedSetupTabIndex;

        [ObservableProperty]
        private IReadOnlyList<LanguageOption> _languageOptions = LocalizationService.AvailableLanguages;

        [ObservableProperty]
        private LanguageOption? _selectedLanguage = LocalizationService.AvailableLanguages
            .FirstOrDefault(language => language.Code == LocalizationService.CurrentLanguage);

        #endregion

        #region 命令

        [RelayCommand]
        private async Task BrowseFFmpeg()
        {
            var file = await _dialogService.OpenFileDialog(LocalizationService.T("Picker.SelectFFmpeg"), new[]
            {
                new FilePickerFileType(LocalizationService.T("Picker.Executable"))
                {
                    Patterns = new[] { "*.exe", "ffmpeg", "ffmpeg.exe" }
                },
                new FilePickerFileType(LocalizationService.T("Picker.AllFiles"))
                {
                    Patterns = new[] { "*.*" }
                }
            });

            if (file != null)
            {
                FfmpegPathText = file.Path.LocalPath;
            }
        }

        [RelayCommand]
        private async Task BrowseArchive()
        {
            var file = await _dialogService.OpenFileDialog(LocalizationService.T("Picker.SelectArchive"), new[]
            {
                new FilePickerFileType(LocalizationService.T("Picker.Archive"))
                {
                    Patterns = new[] { "*.zip", "*.7z", "*.tar.gz", "*.tar" }
                },
                new FilePickerFileType(LocalizationService.T("Picker.AllFiles"))
                {
                    Patterns = new[] { "*.*" }
                }
            });

            if (file != null)
            {
                ArchivePathText = file.Path.LocalPath;
            }
        }

        [RelayCommand]
        private async Task BrowseExifTool()
        {
            var file = await _dialogService.OpenFileDialog(LocalizationService.T("Picker.SelectExifTool"), new[]
            {
                new FilePickerFileType(LocalizationService.T("Picker.Executable"))
                {
                    Patterns = new[] { "*.exe", "exiftool", "exiftool.exe", "exiftool(-k).exe" }
                },
                new FilePickerFileType(LocalizationService.T("Picker.AllFiles"))
                {
                    Patterns = new[] { "*.*" }
                }
            });

            if (file != null)
            {
                ExifToolPathText = file.Path.LocalPath;
            }
        }

        [RelayCommand]
        private async Task BrowseExifToolFolder()
        {
            var folder = await _dialogService.OpenFolderDialog(LocalizationService.T("Picker.SelectExifToolFolder"));
            if (folder != null)
            {
                ExifToolFolderText = folder.Path.LocalPath;
            }
        }

        [RelayCommand]
        private async Task BrowseExifToolArchive()
        {
            var file = await _dialogService.OpenFileDialog(LocalizationService.T("Picker.SelectExifToolArchive"), new[]
            {
                new FilePickerFileType(LocalizationService.T("Picker.Archive"))
                {
                    Patterns = new[] { "*.zip" }
                },
                new FilePickerFileType(LocalizationService.T("Picker.AllFiles"))
                {
                    Patterns = new[] { "*.*" }
                }
            });

            if (file != null)
            {
                ExifToolArchivePathText = file.Path.LocalPath;
            }
        }

        [RelayCommand]
        private async Task SetCustomPath()
        {
            if (string.IsNullOrWhiteSpace(FfmpegPathText))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("Setup.SelectFFmpegFirst"));
                return;
            }

            await ProcessCustomPath(FfmpegPathText);
        }

        [RelayCommand]
        private async Task DetectSystemFFmpeg()
        {
            await ProcessFFmpegSystemDetect();
        }

        [RelayCommand]
        private async Task InstallFromArchive()
        {
            if (string.IsNullOrWhiteSpace(ArchivePathText))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("Setup.SelectArchiveFirst"));
                return;
            }

            await ProcessArchiveInstall(ArchivePathText);
        }

        [RelayCommand]
        private async Task DetectSystemExifTool()
        {
            await ProcessExifToolSystemDetect();
        }

        [RelayCommand]
        private async Task SetExifToolPath()
        {
            if (string.IsNullOrWhiteSpace(ExifToolPathText))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("ExifTool.SelectExecutableFirst"));
                return;
            }

            await ProcessExifToolPath(ExifToolPathText);
        }

        [RelayCommand]
        private async Task SetExifToolFolder()
        {
            if (string.IsNullOrWhiteSpace(ExifToolFolderText))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("ExifTool.SelectFolderFirst"));
                return;
            }

            await ProcessExifToolFolder(ExifToolFolderText);
        }

        [RelayCommand]
        private async Task InstallExifToolArchive()
        {
            if (string.IsNullOrWhiteSpace(ExifToolArchivePathText))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("ExifTool.SelectArchiveFirst"));
                return;
            }

            await ProcessExifToolArchive(ExifToolArchivePathText);
        }

        [RelayCommand]
        private async Task Confirm()
        {
            // 优先处理已选择的路径
            if (!string.IsNullOrWhiteSpace(FfmpegPathText))
            {
                await ProcessCustomPath(FfmpegPathText);
            }
            else if (!string.IsNullOrWhiteSpace(ArchivePathText))
            {
                await ProcessArchiveInstall(ArchivePathText);
            }
            else
            {
                await _dialogService.ShowMessage(
                    LocalizationService.T("Dialog.Info"),
                    LocalizationService.T("Setup.SelectPathOrSkip"));
            }
        }

        [RelayCommand]
        private void Skip()
        {
            OnCloseRequested?.Invoke();
        }

        [RelayCommand]
        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        #endregion

        #region 事件

        public event Action? OnCloseRequested;

        #endregion

        #region 构造函数

        public SetupWindowViewModel() : this(new FFmpegManager(), new ExifToolManager(), new DialogService())
        {
        }

        public SetupWindowViewModel(FFmpegManager ffmpegManager) : this(ffmpegManager, new ExifToolManager(), new DialogService())
        {
        }

        public SetupWindowViewModel(FFmpegManager ffmpegManager, ExifToolManager exifToolManager) : this(ffmpegManager, exifToolManager, new DialogService())
        {
        }

        public SetupWindowViewModel(FFmpegManager ffmpegManager, ExifToolManager exifToolManager, IDialogService dialogService)
        {
            _ffmpegManager = ffmpegManager;
            _exifToolManager = exifToolManager;
            _dialogService = dialogService;
            LocalizationService.LanguageChanged += OnLanguageChanged;
            RefreshExifToolStatus();
        }

        #endregion

        #region 私有方法

        private async Task ProcessCustomPath(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("Setup.FileMissing"));
                return;
            }

            try
            {
                IsProcessing = true;
                StatusText = LocalizationService.T("Setup.Validating");
                
                var success = await _ffmpegManager.SetCustomPath(path);
                
                if (success)
                {
                    SetupCompleted = true;
                    await _dialogService.ShowMessage(LocalizationService.T("Dialog.Success"), LocalizationService.T("Setup.PathSuccess"));
                    OnCloseRequested?.Invoke();
                }
                else
                {
                    await _dialogService.ShowMessage(
                        LocalizationService.T("Dialog.Error"),
                        LocalizationService.T("Setup.InvalidFFmpeg"));
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage(
                    LocalizationService.T("Dialog.Error"),
                    LocalizationService.Format("Setup.PathError", ex.Message));
            }
            finally
            {
                IsProcessing = false;
                StatusText = "";
            }
        }

        private async Task ProcessFFmpegSystemDetect()
        {
            try
            {
                IsProcessing = true;
                StatusText = LocalizationService.T("Setup.Validating");

                var success = await _ffmpegManager.SetSystemFFmpeg();
                if (success)
                {
                    SetupCompleted = true;
                    StatusText = LocalizationService.T("Setup.PathSuccess");
                    await _dialogService.ShowMessage(LocalizationService.T("Dialog.Success"), LocalizationService.T("Setup.PathSuccess"));
                }
                else
                {
                    await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("Setup.SystemFFmpegMissing"));
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage(
                    LocalizationService.T("Dialog.Error"),
                    LocalizationService.Format("Setup.PathError", ex.Message));
            }
            finally
            {
                IsProcessing = false;
                StatusText = "";
            }
        }

        private async Task ProcessArchiveInstall(string archivePath)
        {
            if (!System.IO.File.Exists(archivePath))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("Setup.ArchiveMissing"));
                return;
            }

            try
            {
                IsProcessing = true;
                StatusText = LocalizationService.T("Setup.Installing");
                
                var success = await _ffmpegManager.InstallFFmpegFromArchive(archivePath);
                
                if (success)
                {
                    SetupCompleted = true;
                    await _dialogService.ShowMessage(LocalizationService.T("Dialog.Success"), LocalizationService.T("Setup.InstallSuccess"));
                    OnCloseRequested?.Invoke();
                }
                else
                {
                    await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("Setup.InstallFailed"));
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage(
                    LocalizationService.T("Dialog.Error"),
                    LocalizationService.Format("Setup.InstallError", ex.Message));
            }
            finally
            {
                IsProcessing = false;
                StatusText = "";
            }
        }

        private async Task ProcessExifToolSystemDetect()
        {
            try
            {
                IsProcessing = true;
                ExifToolStatusText = LocalizationService.T("ExifTool.Validating");
                var success = await _exifToolManager.SetSystemExifTool();
                ExifToolStatusText = success
                    ? LocalizationService.T("ExifTool.PathSuccess")
                    : LocalizationService.T("ExifTool.SystemMissing");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ProcessExifToolPath(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("Setup.FileMissing"));
                return;
            }

            try
            {
                IsProcessing = true;
                ExifToolStatusText = LocalizationService.T("ExifTool.Validating");
                var success = await _exifToolManager.SetCustomPath(path);
                ExifToolStatusText = success
                    ? LocalizationService.T("ExifTool.PathSuccess")
                    : LocalizationService.T("ExifTool.Invalid");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ProcessExifToolFolder(string folder)
        {
            if (!System.IO.Directory.Exists(folder))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("ExifTool.FolderMissing"));
                return;
            }

            try
            {
                IsProcessing = true;
                ExifToolStatusText = LocalizationService.T("ExifTool.Validating");
                var success = await _exifToolManager.SetCustomDirectory(folder);
                ExifToolStatusText = success
                    ? LocalizationService.T("ExifTool.PathSuccess")
                    : LocalizationService.T("ExifTool.InvalidFolder");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ProcessExifToolArchive(string archivePath)
        {
            if (!System.IO.File.Exists(archivePath))
            {
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), LocalizationService.T("ExifTool.ArchiveMissing"));
                return;
            }

            try
            {
                IsProcessing = true;
                ExifToolStatusText = LocalizationService.T("ExifTool.Installing");
                var success = await _exifToolManager.InstallFromArchive(archivePath);
                ExifToolStatusText = success
                    ? LocalizationService.T("ExifTool.InstallSuccess")
                    : LocalizationService.T("ExifTool.InstallFailed");
            }
            catch (Exception ex)
            {
                ExifToolStatusText = LocalizationService.Format("ExifTool.InstallError", ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void RefreshExifToolStatus()
        {
            ExifToolStatusText = _exifToolManager.IsExifToolAvailable
                ? LocalizationService.T("ExifTool.Ready")
                : LocalizationService.T("ExifTool.OptionalNotConfigured");
        }

        partial void OnSelectedLanguageChanged(LanguageOption? value)
        {
            if (value != null)
            {
                LocalizationService.SetLanguage(value.Code);
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            SelectedLanguage = LanguageOptions.FirstOrDefault(language => language.Code == LocalizationService.CurrentLanguage);
        }

        public override void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            base.Dispose();
        }

        #endregion
    }
}
