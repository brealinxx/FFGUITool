using System.Threading.Tasks;
using System.Windows.Input;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IFFmpegService _ffmpegService;
        
        private string _ffmpegPath = "";
        private string _ffmpegVersion = "";
        private bool _isFFmpegAvailable;

        public SettingsViewModel(IFFmpegService ffmpegService)
        {
            _ffmpegService = ffmpegService;
            
            BrowseFFmpegCommand = new RelayCommand(async _ => await BrowseFFmpegAsync());
            InstallFFmpegCommand = new RelayCommand(async _ => await InstallFFmpegAsync());
            TestFFmpegCommand = new RelayCommand(async _ => await TestFFmpegAsync());
            
            _ = LoadSettingsAsync();
        }

        public string FFmpegPath
        {
            get => _ffmpegPath;
            set => SetProperty(ref _ffmpegPath, value);
        }

        public string FFmpegVersion
        {
            get => _ffmpegVersion;
            set => SetProperty(ref _ffmpegVersion, value);
        }

        public bool IsFFmpegAvailable
        {
            get => _isFFmpegAvailable;
            set => SetProperty(ref _isFFmpegAvailable, value);
        }

        public ICommand BrowseFFmpegCommand { get; }
        public ICommand InstallFFmpegCommand { get; }
        public ICommand TestFFmpegCommand { get; }

        private async Task LoadSettingsAsync()
        {
            FFmpegPath = _ffmpegService.FFmpegPath;
            IsFFmpegAvailable = _ffmpegService.IsAvailable;
            
            if (IsFFmpegAvailable)
            {
                FFmpegVersion = await _ffmpegService.GetVersionAsync();
            }
        }

        private async Task BrowseFFmpegAsync()
        {
            // Called from View with file dialog result
        }

        public async Task SetFFmpegPath(string path)
        {
            var result = await _ffmpegService.SetCustomPathAsync(path);
            if (result)
            {
                FFmpegPath = path;
                IsFFmpegAvailable = true;
                FFmpegVersion = await _ffmpegService.GetVersionAsync();
            }
        }

        private async Task InstallFFmpegAsync()
        {
            // Called from View with archive file path
        }

        public async Task InstallFromArchive(string archivePath)
        {
            var result = await _ffmpegService.InstallFromArchiveAsync(archivePath);
            if (result)
            {
                await LoadSettingsAsync();
            }
        }

        private async Task TestFFmpegAsync()
        {
            var result = await _ffmpegService.InitializeAsync();
            IsFFmpegAvailable = result;
            
            if (result)
            {
                FFmpegVersion = await _ffmpegService.GetVersionAsync();
            }
        }
    }
}