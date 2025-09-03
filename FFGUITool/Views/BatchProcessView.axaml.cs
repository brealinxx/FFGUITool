using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FFGUITool.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace FFGUITool.Views
{
    public partial class BatchProcessView : UserControl
    {
        public BatchProcessView()
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
            
            if (DataContext is BatchProcessViewModel viewModel)
            {
                viewModel.AddFilesCommand = new ViewModels.RelayCommand(async _ => await AddFilesAsync());
                viewModel.AddFolderCommand = new ViewModels.RelayCommand(async _ => await AddFolderAsync());
            }
        }

        private async Task AddFilesAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择文件",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("视频文件")
                    {
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm" }
                    },
                    new FilePickerFileType("音频文件")
                    {
                        Patterns = new[] { "*.mp3", "*.wav", "*.flac", "*.aac", "*.ogg", "*.m4a", "*.wma" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0 && DataContext is BatchProcessViewModel viewModel)
            {
                var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
                await viewModel.AddFiles(filePaths);
            }
        }

        private async Task AddFolderAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择文件夹",
                AllowMultiple = false
            });

            if (folders.Count > 0 && DataContext is BatchProcessViewModel viewModel)
            {
                await viewModel.AddFolder(folders[0].Path.LocalPath);
            }
        }
    }
}