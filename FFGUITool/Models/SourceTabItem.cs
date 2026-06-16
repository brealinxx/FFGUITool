using CommunityToolkit.Mvvm.ComponentModel;

namespace FFGUITool.Models
{
    public partial class SourceTabItem : ObservableObject
    {
        public SourceTabItem(string inputPath)
        {
            InputPath = inputPath;
            FileName = System.IO.Path.GetFileName(inputPath);
        }

        public string InputPath { get; }
        public string FileName { get; }
        public bool HasSettings { get; set; }
        public CompressionSettings Settings { get; set; } = new();
        public bool IsAdvancedMode { get; set; }
        public double TargetSizeMB { get; set; }
        public int Bitrate { get; set; }
        public bool UseCrf { get; set; }
        public int Crf { get; set; }
        public string SelectedPresetValue { get; set; } = "none";
        public string SelectedVideoFormatValue { get; set; } = "mp4";
        public string SelectedAudioFormatValue { get; set; } = "mp3";
        public string SelectedAudioBitrateValue { get; set; } = "96";
        public string SelectedAudioTrackModeValue { get; set; } = "transcode";
        public string SelectedResolutionValue { get; set; } = "720";
        public string SelectedImageFormatValue { get; set; } = "jpg";
        public string SelectedCodecValue { get; set; } = "libx264";

        [ObservableProperty]
        private bool _isSelected;
    }
}
