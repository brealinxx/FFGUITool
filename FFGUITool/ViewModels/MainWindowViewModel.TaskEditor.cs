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
        private void ResetSelectedInput()
        {
            if (HasSelectedInput || HasQueue) _workspaceActivated = true;
            SetImportSummary(null);
            CompressionSettings.InputPath = "";
            SetInputPathText("");
            CurrentVideoInfo = null;
            IsVideoInfoVisible = false;
            HasSelectedInput = false;
            IsBatchMode = false;
            BatchFileCount = 0;
            _processingWorkspace.ClearIndependentTasks();
            IsSourceTabsVisible = false;
            _processingWorkspace.ClearSharedTasks();
            IsBatchTaskListVisible = false;
            IsInputStatusVisible = false;
            _batchPreviewInfo = null;
            _batchPreviewInfoPath = "";
            BatchModeText = "";
            CanExecute = false;
            CanRetryFailed = false;
            CanCancel = false;
            ProgressValue = 0;
            ProgressText = "";
            CommandText = LocalizationService.T("Command.SelectInput");
            EstimatedBitrateText = "";
            EstimatedBitrateColor = "Gray";
            SourceMetadataText = "";
            IsMetadataPreviewVisible = false;
            UpdateSourceInfoTexts();
            UpdateExecuteAllText();
        }

        private void SetInputPathText(string path)
        {
            _isUpdatingInputPathText = true;
            InputPathText = path;
            _isUpdatingInputPathText = false;
        }

        private void MarkSelectedSourceTab(string path)
        {
            _processingWorkspace.SelectIndependentTask(path);
            Queue.NotifySelectionChanged();
        }

        private void PreserveCurrentFileAsSourceTab()
        {
            var currentPath = CompressionSettings.InputPath;
            if (IsBatchMode || !File.Exists(currentPath) ||
                SourceTabs.Any(item => string.Equals(item.InputPath, currentPath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var currentTab = new ProcessingTask(
                currentPath,
                CompressionSettings.Clone(),
                ProcessingSettingsScope.Independent)
            {
                IsSelected = true,
                Status = LocalizationService.T("SourceTabs.Pending"),
                StatusColor = "Gray"
            };
            _processingWorkspace.AddIndependentTasks(new[] { currentPath }, _ => currentTab);
            SaveSelectedSourceTabSettings();
        }

        private async Task SelectSourceTabCore(ProcessingTask? tab, bool force)
        {
            FlushSliderUpdate();
            if (tab == null || IsProcessing || IsScanning)
            {
                return;
            }

            var isCurrent = string.Equals(tab.InputPath, CompressionSettings.InputPath, StringComparison.OrdinalIgnoreCase);
            if (isCurrent && !force)
            {
                MarkSelectedSourceTab(tab.InputPath);
                return;
            }

            SaveSelectedSourceTabSettings();
            _isLoadingSourceTab = true;
            try
            {
                await ProcessSelectedInput(tab.InputPath, clearSourceTabs: false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _isLoadingSourceTab = false;
            }

            if (tab.HasSettings)
            {
                RestoreSourceTabSettings(tab);
            }
            else
            {
                SaveSelectedSourceTabSettings();
            }
            IsInputStatusVisible = true;
            BatchModeText = LocalizationService.Format("SourceTabs.Mode", SourceTabs.Count);
        }

        private void SaveSelectedSourceTabSettings()
        {
            // Restored selection flags do not yet represent the live editor.
            if (_isLoadingSourceTab || _restoringWorkspace)
            {
                return;
            }

            var tab = SourceTabs.FirstOrDefault(item => item.IsSelected);
            if (tab == null)
            {
                return;
            }

            Editor.SaveTo(tab);
            tab.SettingsSummary = BuildSourceTabSettingsSummary(tab.Settings);
            if (string.IsNullOrWhiteSpace(tab.Status))
            {
                tab.State = ProcessingTaskState.Pending;
                tab.Status = LocalizationService.T("SourceTabs.Pending");
                tab.StatusColor = "Gray";
            }
            tab.HasSettings = true;
        }

        private void RestoreSourceTabSettings(ProcessingTask tab)
        {
            if (!tab.HasSettings)
            {
                return;
            }

            _pendingSliderUpdate = null; _sliderTimer.Stop();
            _isRestoringSourceTab = true;
            try
            {
                CompressionSettings = tab.Settings.Clone();
                CompressionSettings.InputPath = tab.InputPath;
                RestoreFeatureSettings();
                IsAdvancedMode = tab.IsAdvancedMode;
                CompressionPercentage = tab.CompressionPercentage;
                SelectedImageTargetSizeUnit = tab.SelectedImageTargetSizeUnit;
                TargetSizeMB = tab.TargetSizeMB;
                TargetSizeSliderValue = tab.TargetSizeMB;
                Bitrate = tab.Bitrate;
                BitrateSliderValue = tab.Bitrate;
                UseCrf = tab.UseCrf;
                Crf = tab.Crf;
                CrfSliderValue = tab.Crf;
                SelectedCompressionPresetOption = CompressionPresetOptions.Find(option => option.Value == tab.SelectedPresetValue) ?? CompressionPresetOptions[0];
                SelectedVideoFormatOption = VideoFormatOptions.Find(option => option.Value == tab.SelectedVideoFormatValue) ?? VideoFormatOptions[0];
                SelectedAudioFormatOption = AudioFormatOptions.Find(option => option.Value == tab.SelectedAudioFormatValue) ?? AudioFormatOptions[0];
                SelectedAudioBitrateOption = AudioBitrateOptions.Find(option => option.Value == tab.SelectedAudioBitrateValue) ?? AudioBitrateOptions[^1];
                SelectedAudioTrackModeOption = AudioTrackModeOptions.Find(option => option.Value == tab.SelectedAudioTrackModeValue) ?? AudioTrackModeOptions[0];
                SelectedResolutionOption = ResolutionOptions.Find(option => option.Value == tab.SelectedResolutionValue) ?? ResolutionOptions[0];
                SelectedImageFormatOption = ImageFormatOptions.Find(option => option.Value == tab.SelectedImageFormatValue) ?? ImageFormatOptions[0];
                SelectedCodecOption = CodecOptions.Find(option => option.Value == tab.SelectedCodecValue) ?? SelectedCodecOption;
                SelectedHardwareEncoderOption = HardwareEncoderOptions.Find(option => option.Value == tab.SelectedHardwareEncoderValue) ?? HardwareEncoderOptions[0];
                EnableFormatConversion = CompressionSettings.EnableFormatConversion;
                EnableAudioConversion = CompressionSettings.EnableAudioConversion;
                EnableResolutionConversion = CompressionSettings.EnableResolutionConversion;
                EnableTrim = CompressionSettings.EnableTrim;
                TrimStartText = CompressionSettings.TrimStart;
                TrimEndText = CompressionSettings.TrimEnd;
                ClearMetadata = CompressionSettings.ClearMetadata;
                OutputPathText = CompressionSettings.OutputPath;
                RestoreIconSizeSelection(CompressionSettings.IconSizesCsv);
            }
            finally
            {
                _isRestoringSourceTab = false;
            }

            if (IsBatchMode) RefreshSharedTaskPolicy();
            UpdateTargetSizeTexts();
            UpdateBitrateTexts();
            UpdateCrfText();
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        private string BuildSourceTabSettingsSummary(CompressionSettings settings)
        {
            if (settings.IsImageProcessing)
            {
                var target = settings.ImageTargetSizeKB > 0 ? $"{settings.ImageTargetSizeKB:0} KB" : $"Q{settings.ImageQuality}";
                var imageFormat = settings.EnableFormatConversion
                    ? settings.ImageOutputFormat
                    : Path.GetExtension(settings.InputPath).TrimStart('.');
                return $"{imageFormat.ToUpperInvariant()} · {target}";
            }

            var format = settings.EnableAudioConversion
                ? settings.AudioOutputFormat
                : settings.EnableFormatConversion
                    ? settings.OutputFormat
                    : Path.GetExtension(settings.InputPath).TrimStart('.');
            var quality = settings.UseCrf ? $"CRF {settings.Crf}" : $"{settings.TargetSizeMB:0.#} MB";
            return $"{format.ToUpperInvariant()} · {quality}";
        }

        private void RestoreIconSizeSelection(string sizesCsv)
        {
            var sizes = sizesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => int.TryParse(value, out _))
                .Select(int.Parse)
                .ToHashSet();
            IcoSize16 = sizes.Contains(16);
            IcoSize24 = sizes.Contains(24);
            IcoSize32 = sizes.Contains(32);
            IcoSize48 = sizes.Contains(48);
            IcoSize64 = sizes.Contains(64);
            IcoSize128 = sizes.Contains(128);
            IcoSize256 = sizes.Contains(256);
        }

        private void CopySourceTabSettings(ProcessingTask source, ProcessingTask target)
        {
            ParameterEditorViewModel.CopyTo(source, target);
            target.SettingsSummary = BuildSourceTabSettingsSummary(target.Settings);
            // An excluded missing input remains failed even when its parameters change.
            if (target.IsIncluded)
            {
                target.State = ProcessingTaskState.Pending;
                target.Status = LocalizationService.T("SourceTabs.Pending");
                target.StatusColor = "Gray";
                target.OutputPath = "";
                target.Message = "";
                target.IsFailed = false;
            }
            target.HasSettings = true;
        }

}
