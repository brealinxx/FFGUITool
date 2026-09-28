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
        [ObservableProperty] private long _sourceBytes;
        public ProcessingTaskState State { get; set; }
        public void RefreshStatusBrush() => OnPropertyChanged(nameof(StatusColor));
        public ProcessingSettingsScope SettingsScope { get; }
        public bool UsesSharedSettings => SettingsScope == ProcessingSettingsScope.Shared;
        public CompressionSettings Settings { get; set; }

        // Detached data only: never copy ObservableObject event subscriptions.
        public ProcessingTask CreateSnapshot() => new(InputPath, Settings.Clone(), SettingsScope)
        {
            EditorState = EditorState.Clone(),
            SourceBytes = SourceBytes,
            State = State,
            HasSettings = HasSettings,
            UsesRelativeTarget = UsesRelativeTarget,
            RelativeTargetPercentage = RelativeTargetPercentage,
            MinimumVideoBitrateKbps = MinimumVideoBitrateKbps,
            MaximumVideoBitrateKbps = MaximumVideoBitrateKbps,
            IsSelected = IsSelected,
            IsIncluded = IsIncluded,
            SettingsSummary = SettingsSummary,
            Status = Status,
            StatusColor = StatusColor,
            OutputPath = OutputPath,
            Message = Message,
            IsFailed = IsFailed,
        };

        // Editor state retained by independent tasks and the shared folder policy.
        public bool HasSettings { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public TaskEditorState EditorState { get; set; } = new();

        // Compatibility properties keep version 1 workspace documents readable.
        public bool IsAdvancedMode { get => EditorState.IsAdvancedMode; set => EditorState.IsAdvancedMode = value; }
        public int CompressionPercentage { get => EditorState.CompressionPercentage; set => EditorState.CompressionPercentage = value; }
        public double TargetSizeMB { get => EditorState.TargetSizeMB; set => EditorState.TargetSizeMB = value; }
        public int Bitrate { get => EditorState.Bitrate; set => EditorState.Bitrate = value; }
        public bool UseCrf { get => EditorState.UseCrf; set => EditorState.UseCrf = value; }
        public int Crf { get => EditorState.Crf; set => EditorState.Crf = value; }
        public string SelectedPresetValue { get => EditorState.SelectedPresetValue; set => EditorState.SelectedPresetValue = value; }
        public string SelectedVideoFormatValue { get => EditorState.SelectedVideoFormatValue; set => EditorState.SelectedVideoFormatValue = value; }
        public string SelectedAudioFormatValue { get => EditorState.SelectedAudioFormatValue; set => EditorState.SelectedAudioFormatValue = value; }
        public string SelectedAudioBitrateValue { get => EditorState.SelectedAudioBitrateValue; set => EditorState.SelectedAudioBitrateValue = value; }
        public string SelectedAudioTrackModeValue { get => EditorState.SelectedAudioTrackModeValue; set => EditorState.SelectedAudioTrackModeValue = value; }
        public string SelectedResolutionValue { get => EditorState.SelectedResolutionValue; set => EditorState.SelectedResolutionValue = value; }
        public string SelectedImageFormatValue { get => EditorState.SelectedImageFormatValue; set => EditorState.SelectedImageFormatValue = value; }
        public string SelectedCodecValue { get => EditorState.SelectedCodecValue; set => EditorState.SelectedCodecValue = value; }
        public string SelectedHardwareEncoderValue { get => EditorState.SelectedHardwareEncoderValue; set => EditorState.SelectedHardwareEncoderValue = value; }
        public string SelectedImageTargetSizeUnit { get => EditorState.SelectedImageTargetSizeUnit; set => EditorState.SelectedImageTargetSizeUnit = value; }

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
