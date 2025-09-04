using System;
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

        #endregion

        #region 命令

        [RelayCommand]
        private async Task BrowseFFmpeg()
        {
            var file = await _dialogService.OpenFileDialog("选择FFmpeg可执行文件", new[]
            {
                new FilePickerFileType("可执行文件")
                {
                    Patterns = new[] { "*.exe", "ffmpeg", "ffmpeg.exe" }
                },
                new FilePickerFileType("所有文件")
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
            var file = await _dialogService.OpenFileDialog("选择FFmpeg压缩包", new[]
            {
                new FilePickerFileType("压缩包文件")
                {
                    Patterns = new[] { "*.zip", "*.7z", "*.tar.gz", "*.tar" }
                },
                new FilePickerFileType("所有文件")
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
                await _dialogService.ShowMessage("错误", "请先选择FFmpeg可执行文件路径");
                return;
            }

            await ProcessCustomPath(FfmpegPathText);
        }

        [RelayCommand]
        private async Task InstallFromArchive()
        {
            if (string.IsNullOrWhiteSpace(ArchivePathText))
            {
                await _dialogService.ShowMessage("错误", "请先选择FFmpeg压缩包");
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
                await _dialogService.ShowMessage("提示", 
                    "请先选择FFmpeg路径或压缩包，或点击跳过继续使用程序");
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
        }

        #endregion

        #region 私有方法

        private async Task ProcessCustomPath(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                await _dialogService.ShowMessage("错误", "指定的文件不存在");
                return;
            }

            try
            {
                IsProcessing = true;
                StatusText = "验证FFmpeg路径...";
                
                var success = await _ffmpegManager.SetCustomPath(path);
                
                if (success)
                {
                    SetupCompleted = true;
                    await _dialogService.ShowMessage("成功", "FFmpeg路径设置成功！");
                    OnCloseRequested?.Invoke();
                }
                else
                {
                    await _dialogService.ShowMessage("错误", 
                        "指定的文件不是有效的FFmpeg可执行文件");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage("错误", 
                    $"设置FFmpeg路径时出错: {ex.Message}");
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
                await _dialogService.ShowMessage("错误", "指定的压缩包文件不存在");
                return;
            }

            try
            {
                IsProcessing = true;
                StatusText = "正在安装FFmpeg...";
                
                var success = await _ffmpegManager.InstallFFmpegFromArchive(archivePath);
                
                if (success)
                {
                    SetupCompleted = true;
                    await _dialogService.ShowMessage("成功", "FFmpeg安装成功！");
                    OnCloseRequested?.Invoke();
                }
                else
                {
                    await _dialogService.ShowMessage("错误", "FFmpeg安装失败");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage("错误", 
                    $"安装FFmpeg时出错: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                StatusText = "";
            }
        }

        #endregion
    }
}