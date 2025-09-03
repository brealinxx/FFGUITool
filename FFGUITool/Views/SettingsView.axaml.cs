using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FFGUITool.ViewModels;
using System.Threading.Tasks;

namespace FFGUITool.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            if (DataContext is SettingsViewModel viewModel)
            {
                viewModel.BrowseFFmpegCommand = new ViewModels.RelayCommand(async _ => await BrowseFFmpegAsync());
                viewModel.InstallFFmpegCommand = new ViewModels.RelayCommand(async _ => await InstallFFmpegAsync());
            }
        }

        private async Task BrowseFFmpegAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择FFmpeg可执行文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("可执行文件")
                    {
                        Patterns = new[] { "ffmpeg.exe", "ffmpeg" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0 && DataContext is SettingsViewModel viewModel)
            {
                await viewModel.SetFFmpegPath(files[0].Path.LocalPath);
            }
        }

        private async Task InstallFFmpegAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择FFmpeg压缩包",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("压缩文件")
                    {
                        Patterns = new[] { "*.zip", "*.7z", "*.tar", "*.tar.gz" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0 && DataContext is SettingsViewModel viewModel)
            {
                await viewModel.InstallFromArchive(files[0].Path.LocalPath);
            }
        }
    }
}