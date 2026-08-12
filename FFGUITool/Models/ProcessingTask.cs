using CommunityToolkit.Mvvm.ComponentModel;

namespace FFGUITool.Models
{
    public enum ProcessingSettingsScope
    {
        Independent,
        Shared
    }

    /// <summary>
    /// A single media-processing job. Independent tasks own a settings snapshot;
    /// folder tasks reference one shared settings instance.
    /// </summary>
    public partial class ProcessingTask : ObservableObject
    {
        public ProcessingTask(
            string inputPath,
            CompressionSettings settings,
            ProcessingSettingsScope settingsScope)
        {
            InputPath = inputPath;
            FileName = System.IO.Path.GetFileName(inputPath);
            Settings = settings;
            SettingsScope = settingsScope;
        }

        public string InputPath { get; }
        public string FileName { get; }
        public ProcessingSettingsScope SettingsScope { get; }
        public bool UsesSharedSettings => SettingsScope == ProcessingSettingsScope.Shared;
        public CompressionSettings Settings { get; set; }

        // Editor state used only by independent file tabs.
        public bool HasSettings { get; set; }
        public bool IsAdvancedMode { get; set; }
        public int CompressionPercentage { get; set; } = 70;
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
        public string SelectedHardwareEncoderValue { get; set; } = "";
        public string SelectedImageTargetSizeUnit { get; set; } = "KB";

        // Folder tasks share settings but carry the same execution policy.
        public bool UsesRelativeTarget { get; set; }
        public double RelativeTargetPercentage { get; set; }
        public int MinimumVideoBitrateKbps { get; set; }
        public int MaximumVideoBitrateKbps { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isIncluded = true;

        [ObservableProperty]
        private string _settingsSummary = "";

        [ObservableProperty]
        private string _status = "";

        [ObservableProperty]
        private string _statusColor = "Gray";

        [ObservableProperty]
        private string _outputPath = "";

        [ObservableProperty]
        private string _message = "";

        [ObservableProperty]
        private bool _isFailed;
    }
}
