using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FFGUITool.Helpers;
using FFGUITool.Models;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.ViewModels
{
    public class VideoCompressionViewModel : ViewModelBase
    {
        private readonly IVideoProcessor _videoProcessor;
        private readonly IMediaAnalyzer _mediaAnalyzer;
        
        private string _inputFile = "";
        private string _outputDirectory = "";
        private VideoInfo? _currentVideoInfo;
        private CompressionProfile _selectedProfile;
        private int _compressionPercentage = 70;
        private int _targetBitrate = 2000;
        private string _selectedCodec = "libx264";
        private bool _isProcessing;
        private double _progress;
        private string _statusMessage = "";
        private string _generatedCommand = "";
        private CancellationTokenSource? _cancellationTokenSource;

        public VideoCompressionViewModel(IVideoProcessor videoProcessor, IMediaAnalyzer mediaAnalyzer)
        {
            _videoProcessor = videoProcessor;
            _mediaAnalyzer = mediaAnalyzer;
            
            Profiles = new ObservableCollection<CompressionProfile>(CompressionProfile.GetDefaultProfiles());
            _selectedProfile = Profiles[2]; // Default to "Balanced"
            
            SelectFileCommand = new RelayCommand(async _ => await SelectFileAsync());
            SelectOutputCommand = new RelayCommand(async _ => await SelectOutputDirectoryAsync());
            StartCompressionCommand = new RelayCommand(
                async _ => await StartCompressionAsync(),
                _ => !IsProcessing && !string.IsNullOrEmpty(InputFile));
            CancelCommand = new RelayCommand(_ => Cancel(), _ => IsProcessing);
        }

        public ObservableCollection<CompressionProfile> Profiles { get; }

        public string InputFile
        {
            get => _inputFile;
            set
            {
                if (SetProperty(ref _inputFile, value))
                {
                    _ = AnalyzeVideoAsync();
                }
            }
        }

        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }

        public VideoInfo? CurrentVideoInfo
        {
            get => _currentVideoInfo;
            set
            {
                if (SetProperty(ref _currentVideoInfo, value))
                {
                    if (value != null)
                    {
                        CalculateOptimalBitrate();
                    }
                }
            }
        }

        public CompressionProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value))
                {
                    ApplyProfile(value);
                }
            }
        }

        public int CompressionPercentage
        {
            get => _compressionPercentage;
            set
            {
                if (SetProperty(ref _compressionPercentage, value))
                {
                    CalculateOptimalBitrate();
                }
            }
        }

        public int TargetBitrate
        {
            get => _targetBitrate;
            set
            {
                if (SetProperty(ref _targetBitrate, value))
                {
                    UpdateGeneratedCommand();
                }
            }
        }

        public string SelectedCodec
        {
            get => _selectedCodec;
            set
            {
                if (SetProperty(ref _selectedCodec, value))
                {
                    CalculateOptimalBitrate();
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string GeneratedCommand
        {
            get => _generatedCommand;
            set => SetProperty(ref _generatedCommand, value);
        }

        public long EstimatedFileSize => CurrentVideoInfo != null 
            ? BitrateCalculator.EstimateFileSize(TargetBitrate, CurrentVideoInfo.Duration)
            : 0;

        public string EstimatedFileSizeFormatted => FormatFileSize(EstimatedFileSize);

        public ICommand SelectFileCommand { get; set; }
        public ICommand SelectOutputCommand { get; set; }
        public ICommand StartCompressionCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task SelectFileAsync()
        {
            // This would be called from the View with file dialog result
            // The View handles the actual file dialog
        }

        private async Task SelectOutputDirectoryAsync()
        {
            // This would be called from the View with folder dialog result
        }

        private async Task AnalyzeVideoAsync()
        {
            if (string.IsNullOrEmpty(InputFile) || !File.Exists(InputFile))
                return;

            StatusMessage = "分析视频文件...";
            CurrentVideoInfo = await _mediaAnalyzer.AnalyzeVideoAsync(InputFile);
            
            if (CurrentVideoInfo != null)
            {
                StatusMessage = "视频分析完成";
                UpdateGeneratedCommand();
            }
            else
            {
                StatusMessage = "无法分析视频文件";
            }
        }

        private void CalculateOptimalBitrate()
        {
            if (CurrentVideoInfo == null)
                return;

            TargetBitrate = BitrateCalculator.CalculateOptimalBitrate(
                CurrentVideoInfo, 
                CompressionPercentage, 
                SelectedCodec);
        }

        private void ApplyProfile(CompressionProfile profile)
        {
            if (profile == null)
                return;

            TargetBitrate = profile.TargetBitrate;
            SelectedCodec = profile.VideoCodec;
            UpdateGeneratedCommand();
        }

        private void UpdateGeneratedCommand()
        {
            if (string.IsNullOrEmpty(InputFile))
            {
                GeneratedCommand = "请先选择输入文件";
                return;
            }

            var outputFile = GenerateOutputPath();
            var options = new VideoCompressionOptions
            {
                InputFile = InputFile,
                OutputFile = outputFile,
                TargetBitrate = TargetBitrate,
                VideoCodec = SelectedCodec,
                AudioCodec = "aac",
                CompressionPercentage = CompressionPercentage,
                Profile = SelectedProfile,
                SourceVideoInfo = CurrentVideoInfo
            };

            GeneratedCommand = _videoProcessor.GenerateCommand(options);
        }

        private string GenerateOutputPath()
        {
            if (string.IsNullOrEmpty(InputFile))
                return "";

            var directory = string.IsNullOrEmpty(OutputDirectory) 
                ? Path.GetDirectoryName(InputFile) ?? ""
                : OutputDirectory;

            var fileName = Path.GetFileNameWithoutExtension(InputFile);
            var outputFileName = $"{fileName}_compressed_{CompressionPercentage}%.mp4";

            return Path.Combine(directory, outputFileName);
        }

        private async Task StartCompressionAsync()
        {
            if (IsProcessing || string.IsNullOrEmpty(InputFile))
                return;

            IsProcessing = true;
            Progress = 0;
            StatusMessage = "开始压缩...";
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var options = new VideoCompressionOptions
                {
                    InputFile = InputFile,
                    OutputFile = GenerateOutputPath(),
                    TargetBitrate = TargetBitrate,
                    VideoCodec = SelectedCodec,
                    AudioCodec = "aac",
                    CompressionPercentage = CompressionPercentage,
                    Profile = SelectedProfile,
                    SourceVideoInfo = CurrentVideoInfo
                };

                var progress = new Progress<ProcessingProgress>(p =>
                {
                    Progress = p.Percentage;
                    StatusMessage = $"{p.CurrentOperation} - {p.Percentage:F1}% - 剩余时间: {p.TimeRemaining:mm\\:ss}";
                });

                var result = await _videoProcessor.CompressVideoAsync(
                    options, 
                    progress, 
                    _cancellationTokenSource.Token);

                if (result)
                {
                    StatusMessage = "压缩完成！";
                }
                else
                {
                    StatusMessage = "压缩失败";
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "压缩已取消";
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                Progress = 0;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}