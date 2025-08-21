using Avalonia.Controls;
using Avalonia.Interactivity;
using FFGUITool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FFGUITool.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // Get ViewModel from DI or create default
            _viewModel = Program.ServiceProvider?.GetRequiredService<MainWindowViewModel>()
                ?? CreateDefaultViewModel();
            
            DataContext = _viewModel;
            
            Loaded += OnLoaded;
        }

        // Constructor for DI
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            Loaded += OnLoaded;
        }

        private MainWindowViewModel CreateDefaultViewModel()
        {
            // Fallback creation if DI is not available
            var ffmpegService = new Services.FFmpegService();
            var mediaAnalyzer = new Services.MediaAnalyzer(ffmpegService);
            var videoProcessor = new Services.VideoProcessor(ffmpegService, mediaAnalyzer);
            var audioProcessor = new Services.AudioProcessor(ffmpegService, mediaAnalyzer);
            var batchProcessor = new Services.BatchProcessor(videoProcessor, mediaAnalyzer);
            
            return new MainWindowViewModel(
                ffmpegService,
                new VideoCompressionViewModel(videoProcessor, mediaAnalyzer),
                new BatchProcessViewModel(batchProcessor, mediaAnalyzer),
                new AudioConversionViewModel(audioProcessor, mediaAnalyzer),
                new SettingsViewModel(ffmpegService)
            );
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Initialize the application
            if (_viewModel.InitializeCommand.CanExecute(null))
            {
                _viewModel.InitializeCommand.Execute(null);
            }
        }
    }
}