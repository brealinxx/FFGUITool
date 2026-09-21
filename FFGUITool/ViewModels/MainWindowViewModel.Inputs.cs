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
        public async Task ProcessSelectedInput(string path)
        {
            await ProcessSelectedInputs(new[] { path });
        }

        private async Task ProcessSelectedInput(string path, bool clearSourceTabs)
        {
            _inputCancellation?.Cancel();
            _inputCancellation = new CancellationTokenSource();
            CompressionSettings.InputPath = path;
            SetInputPathText(path);
            HasSelectedInput = true;
            IsBatchMode = Directory.Exists(path);
            IsInputStatusVisible = false;
            ClearBatchTasks();

            if (clearSourceTabs)
            {
                _processingWorkspace.ClearIndependentTasks();
                IsSourceTabsVisible = false;
            }

            MarkSelectedSourceTab(path);

            if (IsImageMode)
            {
                await ProcessSelectedImageInput(path);
                return;
            }

            if (IsBatchMode)
            {
                await ProcessSelectedFolder(path);
                return;
            }

            // 分析视频文件
            var extension = Path.GetExtension(path).ToLower();
            if (IsVideoExtension(extension))
            {
                IsSelectedAudioInput = false;
                CanUseVideoConversionTools = true;
                BatchModeText = "";
                EstimatedBitrateText = LocalizationService.T("Estimate.Analyzing");
                EstimatedBitrateColor = "Blue";

                CurrentVideoInfo = await _mediaInputService.AnalyzeAsync(path, cancellationToken: _inputCancellation?.Token ?? default);

                if (CurrentVideoInfo != null)
                {
                    await RefreshCurrentInputMetadata();
                    IsVideoInfoVisible = true;
                    UpdateSourceInfoTexts();
                    InitializeTargetSizeFromVideo();
                    CalculateBitrateFromTargetSize();
                    UpdateBitrateWarningAndEstimation();
                }
            }
            else if (IsAudioExtension(extension))
            {
                IsSelectedAudioInput = true;
                CanUseVideoConversionTools = false;
                BatchModeText = "";
                CurrentVideoInfo = null;
                IsVideoInfoVisible = false;
                EstimatedBitrateText = LocalizationService.T("Estimate.AudioFile");
                EstimatedBitrateColor = "Gray";
                EnableAudioConversion = true;
            }
            else
            {
                IsSelectedAudioInput = false;
                CanUseVideoConversionTools = false;
                BatchModeText = "";
                CurrentVideoInfo = null;
                IsVideoInfoVisible = false;
                EstimatedBitrateText = LocalizationService.T("Estimate.NonVideo");
                EstimatedBitrateColor = "Gray";
            }

            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateCommand();
        }

        public async Task ProcessSelectedInputs(IEnumerable<string> paths)
        {
            if (IsProcessing || IsScanning || IsPreviewing) return;
            var inputs = paths.ToList();
            await _inputGate.WaitAsync();
            try
            {
                if (IsModeSelectionVisible && inputs.Any(File.Exists))
                {
                    if (MediaFileSupport.IsImageExtension(Path.GetExtension(inputs.First(File.Exists)))) SelectImageMode();
                    else SelectVideoMode();
                }
                await ProcessSelectedInputsCore(inputs);
                RefreshVisibleTasks();
                ScheduleWorkspaceSave();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { ValidationMessage = ex.Message; }
            finally { _inputGate.Release(); }
        }

        private async Task ProcessSelectedInputsCore(IEnumerable<string> paths)
        {
            var inputs = paths.ToList();
            var unique = inputs.Distinct(PathIdentity.Comparer).ToList();
            if (unique.Count == 1 && Directory.Exists(unique[0]))
            {
                _workspaceActivated = true;
                await ProcessSelectedInput(unique[0], clearSourceTabs: true);
                return;
            }

            var existing = SourceTabs.Select(task => task.InputPath).ToArray();
            var report = await Task.Run(() => MediaImportReport.SelectFiles(inputs, IsImageMode, existing));
            SetImportSummary(report, 0);
            var supported = report.Files;
            if (supported.Count == 0) return;

            _workspaceActivated = true;
            PreserveCurrentFileAsSourceTab();
            SaveSelectedSourceTabSettings();

            var tabToSelect = _processingWorkspace.AddIndependentTasks(
                supported,
                path => new ProcessingTask(
                        path,
                        new CompressionSettings { InputPath = path, IsImageProcessing = IsImageMode },
                        ProcessingSettingsScope.Independent)
                    {
                        Status = LocalizationService.T("SourceTabs.Pending"),
                        StatusColor = "Gray"
                    });

            IsSourceTabsVisible = SourceTabs.Count > 0;
            IsInputStatusVisible = true;
            BatchModeText = LocalizationService.Format("SourceTabs.Mode", SourceTabs.Count);

            SetImportSummary(report, supported.Count);
            if (tabToSelect != null)
            {
                await SelectSourceTabCore(tabToSelect, force: true);
            }
        }

        private void MarkSelectedSourceTabPending(string? propertyName)
        {
            if (_isLoadingSourceTab || _isRestoringSourceTab || IsProcessing)
            {
                return;
            }

            var isProcessingSetting = propertyName is nameof(LimitFileSize) or nameof(TwoPass) or nameof(AllowImageResize) or nameof(PngCompressionLevel)
                or nameof(StreamCopy) or nameof(OutputNamePattern) or nameof(PreserveFolderStructure) or nameof(CompressionPercentage)
                or nameof(TargetSizeMB)
                or nameof(IsAdvancedMode)
                or nameof(Bitrate)
                or nameof(SelectedCodecOption)
                or nameof(SelectedHardwareEncoderOption)
                or nameof(UseCrf)
                or nameof(Crf)
                or nameof(SelectedCompressionPresetOption)
                or nameof(EnableFormatConversion)
                or nameof(EnableAudioConversion)
                or nameof(EnableResolutionConversion)
                or nameof(SelectedVideoFormatOption)
                or nameof(SelectedAudioFormatOption)
                or nameof(SelectedAudioBitrateOption)
                or nameof(SelectedAudioTrackModeOption)
                or nameof(SelectedResolutionOption)
                or nameof(SelectedImageFormatOption)
                or nameof(SelectedImageTargetSizeUnit)
                or nameof(EnableTrim)
                or nameof(TrimStartText)
                or nameof(TrimEndText)
                or nameof(ClearMetadata)
                or nameof(IcoSize16)
                or nameof(IcoSize24)
                or nameof(IcoSize32)
                or nameof(IcoSize48)
                or nameof(IcoSize64)
                or nameof(IcoSize128)
                or nameof(IcoSize256)
                or nameof(OutputPathText);
            if (!isProcessingSetting)
            {
                return;
            }

            if (IsBatchMode)
            {
                foreach (var task in BatchTasks.Where(task => task.IsIncluded))
                {
                    task.State = ProcessingTaskState.Pending;
                    task.Status = LocalizationService.T("SourceTabs.Pending");
                    task.StatusColor = "Gray";
                    task.Message = "";
                    task.IsFailed = false;
                }

                CanRetryFailed = false;
                return;
            }

            var tab = SourceTabs.FirstOrDefault(item => item.IsSelected);
            if (tab == null)
            {
                return;
            }

            tab.State = ProcessingTaskState.Pending;
            tab.Status = LocalizationService.T("SourceTabs.Pending");
            tab.StatusColor = "Gray";
            tab.Message = "";
            tab.IsFailed = false;
            CanRetryFailed = SourceTabs.Any(item => item.IsFailed);
        }

        [RelayCommand]
        private async Task SelectSourceTab(ProcessingTask? tab)
        {
            await SelectSourceTabCore(tab, force: false);
        }

        [RelayCommand]
        private async Task CloseSourceTab(ProcessingTask? tab)
        {
            if (tab == null || IsProcessing)
            {
                return;
            }

            var nextTask = _processingWorkspace.RemoveIndependentTask(tab);
            CanRetryFailed = SourceTabs.Any(item => item.IsFailed);

            if (SourceTabs.Count == 0)
            {
                ResetSelectedInput();
                return;
            }

            IsSourceTabsVisible = true;
            IsInputStatusVisible = true;
            BatchModeText = LocalizationService.Format("SourceTabs.Mode", SourceTabs.Count);
            if (nextTask != null)
            {
                await SelectSourceTabCore(nextTask, force: true);
            }
        }

        private async Task OnInputPathTextChanged()
        {
            if (_isUpdatingInputPathText || IsProcessing || IsScanning)
            {
                return;
            }

            var path = InputPathText.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(path))
            {
                ResetSelectedInput();
                return;
            }

            if (!MediaFileSupport.IsSupportedDroppedPath(path, IsImageMode))
            {
                CompressionSettings.InputPath = path;
                HasSelectedInput = false;
                BatchModeText = "无效文件";
                IsInputStatusVisible = true;
                UpdateCommand();
                return;
            }

            await ProcessSelectedInputs(new[] { path });
        }

        private async Task ProcessSelectedImageInput(string path)
        {
            if (IsBatchMode)
            {
                await ProcessSelectedFolder(path);
                return;
            }

            CurrentVideoInfo = await _mediaInputService.AnalyzeAsync(path, fallbackToFileInfo: true, cancellationToken: _inputCancellation?.Token ?? default);

            await RefreshCurrentInputMetadata();

            IsVideoInfoVisible = CurrentVideoInfo != null;
            BatchModeText = "";
            var sourceSizeKB = CurrentVideoInfo?.FileSize > 0
                ? CurrentVideoInfo.FileSize / 1024.0
                : 1;
            var targetKB = Math.Max(1, sourceSizeKB);
            ConfigureImageTargetRange(targetKB);

            IsSelectedAudioInput = false;
            CanUseVideoConversionTools = true;
            CompressionSettings.ImageQuality = Bitrate;
            EstimatedBitrateColor = "Green";
            UpdateSourceInfoTexts();
            UpdateTargetSizeTexts();
            UpdateBitrateTexts();
            UpdateImageEstimation();
            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateSourceInfoTexts();
            UpdateCommand();
        }

        private async Task ProcessSelectedFolder(string path)
        {
            IsInputStatusVisible = true;
            _scanCancellation?.Cancel();
            using var scan = new CancellationTokenSource();
            _scanCancellation = scan;
            IsScanning = true;
            ValidationMessage = "";
            var report = new MediaImportReport();
            var cancelled = false;
            try
            {
            var imageMode = IsImageMode;
            var audio = EnableAudioConversion;
            var subfolders = IncludeSubfolders;
            var files = await Task.Run(() =>
            {
                var found = new List<string>();
                foreach (var file in _mediaInputService.DiscoverFolderFiles(path, imageMode, audio, subfolders, scan.Token, report))
                { scan.Token.ThrowIfCancellationRequested(); found.Add(file); }
                return found;
            }, scan.Token);
            if (files.Count == 0 && !IsImageMode)
            {
                var audioReport = new MediaImportReport();
                var audioFiles = await Task.Run(() => _mediaInputService.DiscoverFolderFiles(
                        path,
                        imageMode: false,
                        enableAudioConversion: true,
                        includeSubfolders: IncludeSubfolders, cancellationToken: scan.Token, report: audioReport)
                    .Where(file => IsAudioExtension(Path.GetExtension(file)))
                    .ToList(), scan.Token);
                if (audioFiles.Count > 0)
                {
                    EnableAudioConversion = true;
                    files = audioFiles;
                    report = audioReport;
                }
            }

            scan.Token.ThrowIfCancellationRequested();
            var sizes = await Task.Run(() => files.ToDictionary(file => file, file =>
            { scan.Token.ThrowIfCancellationRequested(); try { return new FileInfo(file).Length; } catch (IOException) { return 0L; } }, PathIdentity.Comparer), scan.Token);
            var existingTasks = BatchTasks.ToDictionary(task => task.InputPath, PathIdentity.Comparer);
            BatchTasks.Clear();
            foreach (var group in files.Chunk(100))
            {
                scan.Token.ThrowIfCancellationRequested();
                foreach (var file in group)
                {
                    var task = existingTasks.TryGetValue(file, out var existing) ? existing :
                        new ProcessingTask(file, CompressionSettings, ProcessingSettingsScope.Shared) { Status = LocalizationService.T("SourceTabs.Pending") };
                    task.SourceBytes = sizes[file]; task.Settings = CompressionSettings; task.UsesRelativeTarget = true;
                    BatchTasks.Add(task);
                }
                RefreshVisibleTasks();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Background);
            }
            IsBatchTaskListVisible = BatchTasks.Count > 0;
            BatchFileCount = files.Count;
            CurrentVideoInfo = null;
            IsVideoInfoVisible = false;
            IsSelectedAudioInput = false;
            CanUseVideoConversionTools = files.Any(file => IsVideoExtension(Path.GetExtension(file)));

            if (BatchFileCount == 0)
            {
                EstimatedBitrateText = LocalizationService.T("Estimate.NonVideo");
                EstimatedBitrateColor = "Gray";
                BatchModeText = LocalizationService.T("Batch.Empty");
            }
            else
            {
                ConfigureBatchTargetRange();
                await PrimeBatchPreviewInfoAsync(files.FirstOrDefault(), scan.Token);
                EstimatedBitrateText = LocalizationService.Format("Batch.Found", BatchFileCount);
                EstimatedBitrateColor = "Green";
                BatchModeText = LocalizationService.Format("Batch.Mode", BatchFileCount);
            }

            UpdateConversionHint();
            UpdateConversionOptionVisibility();
            UpdateSourceInfoTexts();
            UpdateCommand();
            UpdateExecuteAllText();
            }
            catch (OperationCanceledException) { cancelled = true; ValidationMessage = LocalizationService.T("Improve.ScanCancelled"); }
            catch (Exception ex) { ValidationMessage = ex.Message; }
            finally
            {
                if (ReferenceEquals(_scanCancellation, scan))
                {
                    _scanCancellation = null;
                    SetImportSummary(report, BatchTasks.Count, cancelled);
                    IsScanning = false; RefreshVisibleTasks(); RefreshBatchTaskSelection(); ScheduleWorkspaceSave();
                }
            }
        }

        private async Task RefreshFolderPreviewAsync()
        {
            if (!IsBatchMode || !Directory.Exists(CompressionSettings.InputPath))
            {
                return;
            }

            await ProcessSelectedFolder(CompressionSettings.InputPath);
        }

        private async Task PrimeBatchPreviewInfoAsync(string? firstFile, CancellationToken cancellationToken = default)
        {
            _batchPreviewInfo = null;
            _batchPreviewInfoPath = "";

            if (string.IsNullOrWhiteSpace(firstFile) || !File.Exists(firstFile) || !IsVideoExtension(Path.GetExtension(firstFile)))
            {
                return;
            }

            _batchPreviewInfo = await _mediaInputService.AnalyzeAsync(firstFile, cancellationToken: cancellationToken);
            _batchPreviewInfoPath = _batchPreviewInfo == null ? "" : firstFile;
        }

}
