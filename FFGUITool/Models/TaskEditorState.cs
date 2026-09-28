namespace FFGUITool.Models;

/// <summary>Display values and disabled selections retained between edits; encoding reads Settings only.</summary>
public sealed class TaskEditorState
{
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

    public static TaskEditorState FromSettings(CompressionSettings settings) => new()
    {
        IsAdvancedMode = true, CompressionPercentage = settings.CompressionPercentage,
        TargetSizeMB = settings.IsImageProcessing ? settings.ImageTargetSizeKB : settings.TargetSizeMB,
        Bitrate = settings.IsImageProcessing ? settings.ImageQuality : settings.Bitrate,
        UseCrf = settings.UseCrf, Crf = settings.Crf,
        SelectedVideoFormatValue = settings.OutputFormat, SelectedAudioFormatValue = settings.AudioOutputFormat,
        SelectedImageFormatValue = settings.ImageOutputFormat, SelectedAudioBitrateValue = settings.AudioBitrate.ToString(),
        SelectedAudioTrackModeValue = settings.AudioTrackMode, SelectedResolutionValue = settings.ResolutionHeight.ToString(),
        SelectedCodecValue = settings.Codec, SelectedHardwareEncoderValue = settings.AllowHardwareFallback ? "auto" : settings.HardwareEncoder
    };

    public TaskEditorState Clone() => (TaskEditorState)MemberwiseClone();
}
