using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FFGUITool.Models;

namespace FFGUITool.ViewModels;

/// <summary>Owns editable values. MainWindow coordinates media-dependent validation and estimates.</summary>
public partial class ParameterEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private CompressionSettings _compressionSettings = new();
    [ObservableProperty]
    private bool _isAdvancedMode;
    [ObservableProperty]
    private int _compressionPercentage = 70;
    [ObservableProperty]
    private double _targetSizeMB;
    [ObservableProperty]
    private int _bitrate = 2000;
    [ObservableProperty]
    private bool _useCrf;
    [ObservableProperty]
    private int _crf = 23;
    [ObservableProperty]
    private CompressionPresetOption? _selectedCompressionPresetOption;
    [ObservableProperty]
    private CodecOption? _selectedVideoFormatOption;
    [ObservableProperty]
    private CodecOption? _selectedAudioFormatOption;
    [ObservableProperty]
    private CodecOption? _selectedAudioBitrateOption;
    [ObservableProperty]
    private CodecOption? _selectedAudioTrackModeOption;
    [ObservableProperty]
    private CodecOption? _selectedResolutionOption;
    [ObservableProperty]
    private CodecOption? _selectedImageFormatOption;
    [ObservableProperty]
    private CodecOption? _selectedCodecOption;
    [ObservableProperty]
    private CodecOption? _selectedHardwareEncoderOption;
    [ObservableProperty]
    private string _selectedImageTargetSizeUnit = "KB";
    [ObservableProperty] private bool _limitFileSize = true;
    [ObservableProperty] private bool _twoPass;
    [ObservableProperty] private bool _allowImageResize;
    [ObservableProperty] private int _pngCompressionLevel = 6;
    [ObservableProperty] private bool _streamCopy;
    [ObservableProperty] private CodecOption? _selectedGoal;
    [ObservableProperty] private List<CodecOption> _goalOptions = new();

    public TaskEditorState CaptureState()
    {
        var state = new TaskEditorState();
        state.IsAdvancedMode = IsAdvancedMode;
        state.CompressionPercentage = CompressionPercentage;
        state.TargetSizeMB = TargetSizeMB;
        state.Bitrate = Bitrate;
        state.UseCrf = UseCrf;
        state.Crf = Crf;
        state.SelectedPresetValue = SelectedCompressionPresetOption?.Value ?? "none";
        state.SelectedVideoFormatValue = SelectedVideoFormatOption?.Value ?? "mp4";
        state.SelectedAudioFormatValue = SelectedAudioFormatOption?.Value ?? "mp3";
        state.SelectedAudioBitrateValue = SelectedAudioBitrateOption?.Value ?? "96";
        state.SelectedAudioTrackModeValue = SelectedAudioTrackModeOption?.Value ?? "transcode";
        state.SelectedResolutionValue = SelectedResolutionOption?.Value ?? "720";
        state.SelectedImageFormatValue = SelectedImageFormatOption?.Value ?? "jpg";
        state.SelectedCodecValue = SelectedCodecOption?.Value ?? CompressionSettings.Codec;
        state.SelectedHardwareEncoderValue = SelectedHardwareEncoderOption?.Value ?? "";
        state.SelectedImageTargetSizeUnit = SelectedImageTargetSizeUnit;

        return state;
    }

    public void SaveTo(ProcessingTask task)
    {
        task.Settings = CompressionSettings.Clone();
        task.EditorState = CaptureState();
    }

    public static void CopyTo(ProcessingTask source, ProcessingTask target)
    {
        target.Settings = source.Settings.Clone();
        target.Settings.InputPath = target.InputPath;
        target.EditorState = source.EditorState.Clone();
    }
}
