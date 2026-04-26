using System;
using System.Collections.Generic;
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
        private readonly IDialogService _dialogService;

        #region 可观察属性

        [ObservableProperty]
        private string _ffmpegPathText = "";

        [ObservableProperty]
        private string _archivePathText = "";

        [ObservableProperty]
        private string _statusText = "";

        [ObservableProperty]
        private bool _setupCompleted;

        [ObservableProperty]
        private bool _isProcessing;

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

        #endregion

        #region 事件

        public event Action? OnCloseRequested;

        #endregion

        #region 构造函数

        public SetupWindowViewModel() : this(new FFmpegManager(), new DialogService())
        {
        }

        public SetupWindowViewModel(FFmpegManager ffmpegManager) : this(ffmpegManager, new DialogService())
        {
        }

        public SetupWindowViewModel(FFmpegManager ffmpegManager, IDialogService dialogService)
        {
            _ffmpegManager = ffmpegManager;
            _dialogService = dialogService;
            LocalizationService.LanguageChanged += OnLanguageChanged;
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
