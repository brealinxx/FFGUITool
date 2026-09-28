using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using FFGUITool.Models;
using FFGUITool.Services;

namespace FFGUITool.ViewModels;

public partial class MainWindowViewModel
{
        private void OnCompressionPercentageChanged()
        {
            CompressionSettings.CompressionPercentage = CompressionPercentage;
            if (IsBatchMode)
            {
                return;
            }

            CalculateOptimalBitrate();
        }

        private void OnTargetSizeChanged()
        {
            if (_isSyncingCompressionValues)
            {
                return;
            }

            if (IsBatchMode)
            {
                TargetSizeMB = RoundTargetSize(ClampImageRatioPercent(TargetSizeMB));
                TargetSizeSliderValue = TargetSizeMB;
                CompressionPercentage = (int)Math.Round(TargetSizeMB);
                CompressionSettings.CompressionPercentage = CompressionPercentage;
                CompressionSettings.TargetSizeMB = TargetSizeMB;
                CompressionSettings.ImageTargetSizeKB = TargetSizeMB;
                if (IsImageMode)
                {
                    UpdateImageBatchQualityFromRatio();
                }
                UpdateTargetSizeTexts();
                RefreshBatchModeSummary();
                UpdateCommand();
                return;
            }

            if (IsImageMode)
            {
                TargetSizeMB = RoundTargetSize(ClampTargetSize(TargetSizeMB));
                TargetSizeSliderValue = TargetSizeMB;
                CompressionSettings.ImageTargetSizeKB = LimitFileSize ? ImageTargetDisplayValueToKB(TargetSizeMB) : 0;
                EstimateImageQualityFromTargetSize();
                UpdateTargetSizeTexts();
                UpdateCommand();
                return;
            }

            if (CurrentVideoInfo != null)
            {
                TargetSizeMB = RoundTargetSize(ClampTargetSize(TargetSizeMB));
            }

            CompressionSettings.TargetSizeMB = TargetSizeMB;
            TargetSizeSliderValue = TargetSizeMB;
            CalculateBitrateFromTargetSize();
            UpdateTargetSizeTexts();
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        private void OnTargetSizeSliderChanged()
        {
            TargetSizeMB = RoundTargetSize(TargetSizeSliderValue);
        }

        private void OnImageTargetSizeUnitChanged()
        {
            if (!IsImageMode)
            {
                return;
            }

            var targetKB = CompressionSettings.ImageTargetSizeKB > 0
                ? CompressionSettings.ImageTargetSizeKB
                : ImageTargetDisplayValueToKB(TargetSizeMB);

            _isSyncingCompressionValues = true;
            ConfigureImageTargetRange(targetKB);
            _isSyncingCompressionValues = false;
            UpdateTargetSizeTexts();
            UpdateImageEstimation();
            UpdateCommand();
        }

        private void OnCompressionPresetChanged()
        {
            if (SelectedCompressionPresetOption == null)
            {
                return;
            }

            ApplyCompressionPreset(SelectedCompressionPresetOption);
            ApplyPresetEditability();

            if (CurrentVideoInfo != null && SelectedCompressionPresetOption.TargetSizeMB > 0)
            {
                TargetSizeMB = ClampTargetSize(SelectedCompressionPresetOption.TargetSizeMB);
            }
            else if (CurrentVideoInfo != null && SelectedCompressionPresetOption.Value == "extreme")
            {
                TargetSizeMB = TargetSizeSliderMinimum;
            }
            else if (IsBatchMode && SelectedCompressionPresetOption.Value == "none")
            {
                ConfigureBatchTargetRange();
            }

            CalculateBitrateFromTargetSize();
            UpdateBitrateWarningAndEstimation();
            RefreshBatchModeSummary();
            UpdateCommand();
        }

        private void OnConversionToggleChanged(string? changedPropertyName)
        {
            if (_isSyncingConversionToggles)
            {
                return;
            }

            _isSyncingConversionToggles = true;
            if (changedPropertyName == nameof(EnableAudioConversion) && EnableAudioConversion)
            {
                EnableFormatConversion = false;
                EnableResolutionConversion = false;
            }
            else if ((changedPropertyName == nameof(EnableFormatConversion) || changedPropertyName == nameof(EnableResolutionConversion))
                     && (EnableFormatConversion || EnableResolutionConversion))
            {
                EnableAudioConversion = false;
            }

            if (!CanUseVideoConversionTools)
            {
                EnableFormatConversion = false;
                EnableResolutionConversion = false;
            }

            _isSyncingConversionToggles = false;
            CompressionSettings.EnableFormatConversion = EnableFormatConversion;
            CompressionSettings.EnableAudioConversion = EnableAudioConversion;
            CompressionSettings.EnableResolutionConversion = EnableResolutionConversion;
            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            ApplySelectedConversionOptions();
            RefreshBatchModeSummary();
            if (IsImageMode)
            {
                UpdateImageEstimation();
            }
            UpdateCommand();
        }

        private void OnVideoFormatChanged()
        {
            if (SelectedVideoFormatOption != null)
            {
                if (SelectedVideoFormatOption.Value == "gif")
                {
                    _isSyncingConversionToggles = true;
                    EnableAudioConversion = false;
                    _isSyncingConversionToggles = false;
                }

                CompressionSettings.OutputFormat = SelectedVideoFormatOption.Value;
                if (SelectedVideoFormatOption.Value is "webm" or "gif") SelectedHardwareEncoderOption = HardwareEncoderOptions[0];
                UpdateConversionHint();
                UpdateCommand();
            }
        }

        private void OnAudioFormatChanged()
        {
            if (SelectedAudioFormatOption != null)
            {
                CompressionSettings.AudioOutputFormat = SelectedAudioFormatOption.Value;
                UpdateCommand();
            }
        }

        private void OnAudioBitrateChanged()
        {
            if (SelectedAudioBitrateOption != null && int.TryParse(SelectedAudioBitrateOption.Value, out var audioBitrate))
            {
                CompressionSettings.AudioBitrate = audioBitrate;
                UpdateBitrateWarningAndEstimation();
                UpdateCommand();
            }
        }

        private void OnAudioTrackModeChanged()
        {
            if (SelectedAudioTrackModeOption == null)
            {
                return;
            }

            CompressionSettings.AudioTrackMode = SelectedAudioTrackModeOption.Value;
            UpdateCommand();
        }

        private void OnTrimChanged()
        {
            CompressionSettings.EnableTrim = EnableTrim;
            UpdateTrimHintText();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnTrimTextChanged()
        {
            CompressionSettings.TrimStart = TrimStartText.Trim();
            CompressionSettings.TrimEnd = TrimEndText.Trim();
            UpdateTrimHintText();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnIconSizeChanged()
        {
            CompressionSettings.IconSizesCsv = string.Join(",", GetSelectedIconSizes());
            UpdateCommand();
        }

        private List<int> GetSelectedIconSizes()
        {
            var sizes = new List<int>();
            if (IcoSize16) sizes.Add(16);
            if (IcoSize24) sizes.Add(24);
            if (IcoSize32) sizes.Add(32);
            if (IcoSize48) sizes.Add(48);
            if (IcoSize64) sizes.Add(64);
            if (IcoSize128) sizes.Add(128);
            if (IcoSize256) sizes.Add(256);
            return sizes.Count == 0 ? new List<int> { 16, 32, 48, 256 } : sizes;
        }

        private static bool IsIconFormat(string format)
        {
            return string.Equals(format, "ico", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(format, "icns", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateTrimHintText()
        {
            if (!EnableTrim)
            {
                TrimHintText = LocalizationService.CurrentLanguage == "en-US" ? "Disabled" : "未启用";
                return;
            }

            var start = string.IsNullOrWhiteSpace(TrimStartText) ? "0" : TrimStartText.Trim();
            var end = string.IsNullOrWhiteSpace(TrimEndText)
                ? (LocalizationService.CurrentLanguage == "en-US" ? "end" : "结尾")
                : TrimEndText.Trim();
            TrimHintText = LocalizationService.CurrentLanguage == "en-US"
                ? $"Only process {start} to {end}"
                : $"仅处理 {start} 到 {end}";
        }

        private void OnImageFormatChanged()
        {
            if (SelectedImageFormatOption != null)
            {
                CompressionSettings.ImageOutputFormat = SelectedImageFormatOption.Value;
                if (IsIconFormat(SelectedImageFormatOption.Value))
                {
                    EnableResolutionConversion = false;
                }

                IsIconOptionsVisible = IsImageMode && IsAdvancedMode && HasSelectedInput && EnableFormatConversion && IsIconFormat(SelectedImageFormatOption.Value);
                CompressionSettings.IconSizesCsv = string.Join(",", GetSelectedIconSizes());
                UpdateConversionOptionVisibility();
                UpdateImageEstimation();
                UpdateCommand();
            }
        }

        private void OnResolutionChanged()
        {
            if (SelectedResolutionOption != null && int.TryParse(SelectedResolutionOption.Value, out var height))
            {
                CompressionSettings.ResolutionHeight = height;
                if (IsImageMode)
                {
                    UpdateImageEstimation();
                }
                UpdateCommand();
            }
        }

        private void OnClearMetadataChanged()
        {
            if (ClearMetadata && !CanClearMetadata)
            {
                ClearMetadata = false;
                return;
            }

            CompressionSettings.ClearMetadata = ClearMetadata;
            IsMetadataPreviewVisible = IsMetadataClearOptionVisible && CanClearMetadata && ClearMetadata;
            UpdateMetadataPreviewText();
            UpdateCommand();
        }

        private async Task RefreshCurrentInputMetadata()
        {
            if (CurrentVideoInfo == null || string.IsNullOrWhiteSpace(CurrentVideoInfo.FilePath))
            {
                return;
            }

            CurrentVideoInfo.MetadataSummary = await _exifToolManager.ReadSensitiveMetadata(CurrentVideoInfo.FilePath);
            UpdateMetadataPreviewText();
        }

        private void OnBitrateChanged()
        {
            if (IsImageMode)
            {
                Bitrate = Math.Max(1, Math.Min(Bitrate, 100));
                CompressionSettings.ImageQuality = Bitrate;
                BitrateSliderValue = Bitrate;
                if (!_isSyncingCompressionValues)
                {
                    UpdateImageTargetFromQuality();
                }
                UpdateBitrateTexts();
                UpdateImageEstimation();
                RefreshBatchModeSummary();
                UpdateCommand();
                return;
            }

            CompressionSettings.Bitrate = Bitrate;
            BitrateSliderValue = Bitrate;
            UpdateTargetSizeFromBitrate();
            UpdateBitrateTexts();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnBitrateSliderChanged()
        {
            Bitrate = (int)BitrateSliderValue;
        }

        private void OnCodecChanged()
        {
            if (SelectedCodecOption != null)
            {
                SelectedCodec = SelectedCodecOption.Value;
                CompressionSettings.Codec = SelectedCodec;
                CalculateOptimalBitrate();
                UpdateCommand();
            }
        }

        private void OnHardwareEncoderChanged()
        {
            var selectedValue = SelectedHardwareEncoderOption?.Value ?? "";
            if (EnableFormatConversion && SelectedVideoFormatOption?.Value is "webm" or "gif") selectedValue = "";
            CompressionSettings.HardwareEncoder = selectedValue == "auto"
                ? RecommendHardwareEncoder()
                : selectedValue;
            UpdateCommand();
        }

        private void OnUseCrfChanged()
        {
            CompressionSettings.UseCrf = UseCrf;
            if (_featuresReady && !_syncingGoal && !_isRestoringSourceTab)
            {
                _syncingGoal = true;
                try
                {
                    if (UseCrf) LimitFileSize = false;
                    SelectedGoal = GoalOptions.Find(option => option.Value == (UseCrf ? "quality" : LimitFileSize ? "size" : IsImageMode ? "quality" : "bitrate"));
                }
                finally { _syncingGoal = false; }
            }
            UpdateControlEditability();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnCrfChanged()
        {
            Crf = Math.Max(0, Math.Min(Crf, 51));
            CompressionSettings.Crf = Crf;
            CrfSliderValue = Crf;
            UpdateCrfText();
            UpdateBitrateWarningAndEstimation();
            UpdateCommand();
        }

        private void OnCrfSliderChanged()
        {
            Crf = (int)Math.Round(CrfSliderValue);
        }

}
