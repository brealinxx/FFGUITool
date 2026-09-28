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
        private void CalculateOptimalBitrate()
        {
            if (CurrentVideoInfo == null)
            {
                EstimatedBitrateText = "";
                return;
            }

            EstimatedBitrateText = LocalizationService.T("Estimate.Calculating");

            CalculateBitrateFromTargetSize();

            UpdateBitrateWarningAndEstimation();
        }

        private void InitializeTargetSizeFromVideo()
        {
            if (CurrentVideoInfo == null)
            {
                return;
            }

            var originalSizeMB = CurrentVideoInfo.FileSize / 1024.0 / 1024.0;
            TargetSizeSliderMaximum = originalSizeMB > 0 ? originalSizeMB : 1;
            var minimumTargetSize = originalSizeMB >= 1
                ? Math.Max(1, originalSizeMB * 0.03)
                : Math.Max(0.1, originalSizeMB * 0.03);
            TargetSizeSliderMinimum = Math.Min(TargetSizeSliderMaximum, minimumTargetSize);
            TargetSizeMB = TargetSizeSliderMaximum;
            TargetSizeSliderValue = TargetSizeMB;
            CompressionSettings.TargetSizeMB = TargetSizeMB;
            UpdateTargetSizeTexts();
            UpdateBitrateControlsRange(CurrentVideoInfo.Bitrate);
        }

        private void ConfigureBatchTargetRange()
        {
            _isSyncingCompressionValues = true;
            TargetSizeSliderMinimum = 1;
            TargetSizeSliderMaximum = 100;
            TargetSizeMB = RoundTargetSize(ClampImageRatioPercent(CompressionPercentage));
            TargetSizeSliderValue = TargetSizeMB;
            IsImageTargetUnitSelectorVisible = false;
            _isSyncingCompressionValues = false;
            CompressionSettings.TargetSizeMB = TargetSizeMB;
            CompressionSettings.ImageTargetSizeKB = TargetSizeMB;
            if (IsImageMode)
            {
                UpdateImageBatchQualityFromRatio();
            }
            UpdateTargetSizeTexts();
        }

        private void UpdateImageBatchQualityFromRatio()
        {
            var estimatedQuality = EstimateImageQualityFromRatioPercent(TargetSizeMB);
            _isSyncingCompressionValues = true;
            Bitrate = Math.Max(1, Math.Min(100, estimatedQuality));
            BitrateSliderValue = Bitrate;
            _isSyncingCompressionValues = false;
            CompressionSettings.ImageQuality = Bitrate;
            UpdateBitrateTexts();
            UpdateImageEstimation();
        }

        private void CalculateBitrateFromTargetSize()
        {
            if (CurrentVideoInfo == null)
            {
                return;
            }

            var targetBitrate = _commandBuilder.CalculateBitrateForTargetSize(CurrentVideoInfo, TargetSizeMB);
            try { targetBitrate = EncodingPolicy.TargetBitrate(TargetSizeMB, EncodingPolicy.OutputDuration(CompressionSettings, CurrentVideoInfo),
                EncodingPolicy.AudioBudget(CompressionSettings, CurrentVideoInfo)); }
            catch (ArgumentException ex) { ValidationMessage = ex.Message; }
            if (SelectedCompressionPresetOption != null && SelectedCompressionPresetOption.Value != "none")
            {
                if (SelectedCompressionPresetOption.MinVideoBitrateKbps > 0)
                {
                    targetBitrate = Math.Max(targetBitrate, SelectedCompressionPresetOption.MinVideoBitrateKbps);
                }

                if (SelectedCompressionPresetOption.MaxVideoBitrateKbps > 0)
                {
                    targetBitrate = Math.Min(targetBitrate, SelectedCompressionPresetOption.MaxVideoBitrateKbps);
                }
            }

            _isSyncingCompressionValues = true;
            Bitrate = targetBitrate;
            BitrateSliderValue = targetBitrate;
            _isSyncingCompressionValues = false;
            CompressionSettings.Bitrate = Bitrate;
            UpdateBitrateTexts();
        }

        private void UpdateTargetSizeFromBitrate()
        {
            if (_isSyncingCompressionValues || CurrentVideoInfo == null)
            {
                return;
            }

            var estimatedSize = _commandBuilder.CalculateEstimatedFileSize(Bitrate, GetEstimateDuration(), EncodingPolicy.AudioBudget(CompressionSettings, CurrentVideoInfo));
            var estimatedSizeMB = estimatedSize / 1024.0 / 1024.0;

            _isSyncingCompressionValues = true;
            TargetSizeMB = ClampTargetSize(estimatedSizeMB);
            TargetSizeSliderValue = TargetSizeMB;
            _isSyncingCompressionValues = false;
            CompressionSettings.TargetSizeMB = TargetSizeMB;
            UpdateTargetSizeTexts();
        }

        private double ClampTargetSize(double targetSizeMB)
        {
            if (TargetSizeSliderMaximum <= 0)
            {
                return targetSizeMB;
            }

            return Math.Max(TargetSizeSliderMinimum, Math.Min(targetSizeMB, TargetSizeSliderMaximum));
        }

        private void ApplyCompressionPreset(CompressionPresetOption preset)
        {
            if (preset.Value == "none")
            {
                UseCrf = false;
                CompressionSettings.UseCrf = false;
                CompressionSettings.Crf = 23;
                Crf = 23;
                CompressionSettings.AudioBitrate = 96;
                CompressionSettings.MaxHeight = 0;
                CompressionSettings.MaxFramerate = 0;
                CompressionSettings.Codec = SelectedCodec;
                return;
            }

            CompressionSettings.Codec = preset.Codec;
            UseCrf = preset.UseCrf;
            CompressionSettings.UseCrf = preset.UseCrf;
            CompressionSettings.Crf = preset.Crf;
            Crf = preset.Crf;
            CompressionSettings.AudioBitrate = preset.AudioBitrateKbps;
            CompressionSettings.MaxHeight = preset.MaxHeight;
            CompressionSettings.MaxFramerate = preset.MaxFramerate;

            SelectedCodec = preset.Codec;
            SelectedCodecOption = CodecOptions.Find(option => option.Value == preset.Codec) ?? SelectedCodecOption;
        }

        private bool IsPresetSizeEstimateLocked()
        {
            return !IsImageMode
                   && string.Equals(SelectedCompressionPresetOption?.Value, "chat", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyPresetEditability()
        {
            CanEditAdvancedMode = true;
            UpdateConversionOptionVisibility();
        }

        private void UpdateBitrateControlsRange(int originalBitrate)
        {
            BitrateSliderMaximum = Math.Max(originalBitrate * 3 / 2, 50000);
            BitrateSliderMinimum = 1;
        }

        private void UpdateBitrateWarningAndEstimation()
        {
            if (IsImageMode)
            {
                IsBitrateWarningVisible = false;
                UpdateImageEstimation();
                return;
            }
            if (CurrentVideoInfo == null) return;

            if (CompressionSettings.UseCrf)
            {
                IsBitrateWarningVisible = false;
                EstimatedBitrateText = LocalizationService.Format("Estimate.Crf", CompressionSettings.Crf, CompressionSettings.AudioBitrate);
                EstimatedBitrateColor = "Green";
                return;
            }

            IsBitrateWarningVisible = Bitrate > CurrentVideoInfo.Bitrate;

            var estimatedSize = _commandBuilder.CalculateEstimatedFileSize(Bitrate, GetEstimateDuration(), EncodingPolicy.AudioBudget(CompressionSettings, CurrentVideoInfo));
            var originalSizeMB = CurrentVideoInfo.FileSize / 1024.0 / 1024.0;
            var estimatedSizeMB = estimatedSize / 1024.0 / 1024.0;

            if (estimatedSizeMB > originalSizeMB)
            {
                var increaseRatio = (estimatedSizeMB / originalSizeMB - 1) * 100;
                EstimatedBitrateText = LocalizationService.Format(
                    "Estimate.Current",
                    Bitrate,
                    estimatedSizeMB,
                    LocalizationService.T("Estimate.Increase"),
                    increaseRatio);
                EstimatedBitrateColor = "Orange";
            }
            else
            {
                var compressionRatio = (1 - estimatedSizeMB / originalSizeMB) * 100;
                EstimatedBitrateText = LocalizationService.Format(
                    "Estimate.Current",
                    Bitrate,
                    estimatedSizeMB,
                    LocalizationService.T("Estimate.Compress"),
                    compressionRatio);
                EstimatedBitrateColor = "Green";
            }
        }

}
