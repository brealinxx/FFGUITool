using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IFFmpegService _ffmpegService;
        private int _selectedTabIndex;
        private string _ffmpegStatus = "检测中...";
        private bool _isFFmpegAvailable;

        public MainWindowViewModel(
            IFFmpegService ffmpegService,
            VideoCompressionViewModel videoCompressionViewModel,
            BatchProcessViewModel batchProcessViewModel,
            AudioConversionViewModel audioConversionViewModel,
            SettingsViewModel settingsViewModel)
        {
            _ffmpegService = ffmpegService;
            
            VideoCompressionViewModel = videoCompressionViewModel;
            BatchProcessViewModel = batchProcessViewModel;
            AudioConversionViewModel = audioConversionViewModel;
            SettingsViewModel = settingsViewModel;

            InitializeCommand = new RelayCommand(async _ => await InitializeAsync());
        }

        public VideoCompressionViewModel VideoCompressionViewModel { get; }
        public BatchProcessViewModel BatchProcessViewModel { get; }
        public AudioConversionViewModel AudioConversionViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public string FFmpegStatus
        {
            get => _ffmpegStatus;
            set => SetProperty(ref _ffmpegStatus, value);
        }

        public bool IsFFmpegAvailable
        {
            get => _isFFmpegAvailable;
            set => SetProperty(ref _isFFmpegAvailable, value);
        }

        public ICommand InitializeCommand { get; }

        private async Task InitializeAsync()
        {
            FFmpegStatus = "正在检测FFmpeg...";
            
            var result = await _ffmpegService.InitializeAsync();
            IsFFmpegAvailable = result;

            if (result)
            {
                var version = await _ffmpegService.GetVersionAsync();
                FFmpegStatus = $"FFmpeg已就绪 - {version}";
            }
            else
            {
                FFmpegStatus = "FFmpeg未配置 - 请在设置中配置";
                SelectedTabIndex = 3; // Switch to settings tab
            }
        }
    }
}