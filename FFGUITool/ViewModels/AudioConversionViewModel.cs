using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FFGUITool.Models;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.ViewModels
{
    public class AudioConversionViewModel : ViewModelBase
    {
        private readonly IAudioProcessor _audioProcessor;
        private readonly IMediaAnalyzer _mediaAnalyzer;
        
        private string _inputFile = "";
        private string _outputDirectory = "";
        private AudioInfo? _currentAudioInfo;
        private AudioFormat _selectedFormat = AudioFormat.MP3;
        private int _bitrate = 128;
        private int _sampleRate = 44100;
        private int _channels = 2;
        private bool _isProcessing;
        private double _progress;
        private string _statusMessage = "";

        public AudioConversionViewModel(IAudioProcessor audioProcessor, IMediaAnalyzer mediaAnalyzer)
        {
            _audioProcessor = audioProcessor;
            _mediaAnalyzer = mediaAnalyzer;
            
            AudioFormats = new ObservableCollection<AudioFormat>(Enum.GetValues<AudioFormat>());
            
            SelectFileCommand = new RelayCommand(async _ => await SelectFileAsync());
            SelectOutputCommand = new RelayCommand(async _ => await SelectOutputDirectoryAsync());
            StartConversionCommand = new RelayCommand(
                async _ => await StartConversionAsync(),
                _ => !IsProcessing && !string.IsNullOrEmpty(InputFile));
            ExtractAudioCommand = new RelayCommand(
                async _ => await ExtractAudioAsync(),
                _ => !IsProcessing && !string.IsNullOrEmpty(InputFile));
        }

        public ObservableCollection<AudioFormat> AudioFormats { get; }

        public string InputFile
        {
            get => _inputFile;
            set
            {
                if (SetProperty(ref _inputFile, value))
                {
                    _ = AnalyzeAudioAsync();
                }
            }
        }

        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }

        public AudioInfo? CurrentAudioInfo
        {
            get => _currentAudioInfo;
            set => SetProperty(ref _currentAudioInfo, value);
        }

        public AudioFormat SelectedFormat
        {
            get => _selectedFormat;
            set => SetProperty(ref _selectedFormat, value);
        }

        public int Bitrate
        {
            get => _bitrate;
            set => SetProperty(ref _bitrate, value);
        }

        public int SampleRate
        {
            get => _sampleRate;
            set => SetProperty(ref _sampleRate, value);
        }

        public int Channels
        {
            get => _channels;
            set => SetProperty(ref _channels, value);
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

        public ICommand SelectFileCommand { get; }
        public ICommand SelectOutputCommand { get; }
        public ICommand StartConversionCommand { get; }
        public ICommand ExtractAudioCommand { get; }

        private async Task SelectFileAsync()
        {
            // Called from View with file dialog result
        }

        private async Task SelectOutputDirectoryAsync()
        {
            // Called from View with folder dialog result
        }

        private async Task AnalyzeAudioAsync()
        {
            if (string.IsNullOrEmpty(InputFile) || !File.Exists(InputFile))
                return;

            StatusMessage = "分析音频文件...";
            
            var mediaType = _mediaAnalyzer.GetMediaType(InputFile);
            if (mediaType == MediaType.Audio)
            {
                CurrentAudioInfo = await _mediaAnalyzer.AnalyzeAudioAsync(InputFile);
            }
            else if (mediaType == MediaType.Video)
            {
                // For video files, we can extract audio info
                var videoInfo = await _mediaAnalyzer.AnalyzeVideoAsync(InputFile);
                if (videoInfo != null)
                {
                    StatusMessage = "检测到视频文件，可以提取音频";
                }
            }
            
            if (CurrentAudioInfo != null)
            {
                StatusMessage = "音频分析完成";
            }
        }

        private async Task StartConversionAsync()
        {
            if (IsProcessing || string.IsNullOrEmpty(InputFile))
                return;

            IsProcessing = true;
            Progress = 0;
            StatusMessage = "开始转换...";

            try
            {
                var outputFile = GenerateOutputPath(SelectedFormat);
                var options = new AudioConversionOptions
                {
                    InputFile = InputFile,
                    OutputFile = outputFile,
                    Codec = GetCodecForFormat(SelectedFormat),
                    Bitrate = Bitrate,
                    SampleRate = SampleRate,
                    Channels = Channels,
                    OutputFormat = SelectedFormat
                };

                var progress = new Progress<ProcessingProgress>(p =>
                {
                    Progress = p.Percentage;
                    StatusMessage = $"转换中: {p.Percentage:F1}%";
                });

                var result = await _audioProcessor.ConvertFormatAsync(options, progress);

                StatusMessage = result ? "转换完成！" : "转换失败";
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                Progress = 0;
            }
        }

        private async Task ExtractAudioAsync()
        {
            if (IsProcessing || string.IsNullOrEmpty(InputFile))
                return;

            IsProcessing = true;
            StatusMessage = "提取音频...";

            try
            {
                var outputFile = GenerateOutputPath(SelectedFormat);
                var result = await _audioProcessor.ExtractAudioFromVideoAsync(
                    InputFile, 
                    outputFile, 
                    SelectedFormat);

                StatusMessage = result ? "音频提取完成！" : "音频提取失败";
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private string GenerateOutputPath(AudioFormat format)
        {
            if (string.IsNullOrEmpty(InputFile))
                return "";

            var directory = string.IsNullOrEmpty(OutputDirectory)
                ? Path.GetDirectoryName(InputFile) ?? ""
                : OutputDirectory;

            var fileName = Path.GetFileNameWithoutExtension(InputFile);
            var extension = format.ToString().ToLower();
            var outputFileName = $"{fileName}_converted.{extension}";

            return Path.Combine(directory, outputFileName);
        }

        private string GetCodecForFormat(AudioFormat format)
        {
            return format switch
            {
                AudioFormat.MP3 => "libmp3lame",
                AudioFormat.AAC => "aac",
                AudioFormat.WAV => "pcm_s16le",
                AudioFormat.FLAC => "flac",
                AudioFormat.OGG => "libvorbis",
                AudioFormat.M4A => "aac",
                AudioFormat.WMA => "wmav2",
                _ => "copy"
            };
        }
    }
}