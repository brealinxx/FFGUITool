using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FFGUITool.ViewModels;
using System.Threading.Tasks;

namespace FFGUITool.Views
{
    public partial class AudioConversionView : UserControl
    {
        public AudioConversionView()
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
            
            if (DataContext is AudioConversionViewModel viewModel)
            {
                viewModel.SelectFileCommand = new ViewModels.RelayCommand(async _ => await SelectFileAsync());
                viewModel.SelectOutputCommand = new ViewModels.RelayCommand(async _ => await SelectOutputDirectoryAsync());
            }
        }

        private async Task SelectFileAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择音频或视频文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("音频文件")
                    {
                        Patterns = new[] { "*.mp3", "*.wav", "*.flac", "*.aac", "*.ogg", "*.m4a", "*.wma" }
                    },
                    new FilePickerFileType("视频文件")
                    {
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0 && DataContext is AudioConversionViewModel viewModel)
            {
                viewModel.InputFile = files[0].Path.LocalPath;
            }
        }

        private async Task SelectOutputDirectoryAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择输出文件夹",
                AllowMultiple = false
            });

            if (folders.Count > 0 && DataContext is AudioConversionViewModel viewModel)
            {
                viewModel.OutputDirectory = folders[0].Path.LocalPath;
            }
        }
    }
}
