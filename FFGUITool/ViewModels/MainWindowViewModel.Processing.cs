using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FFGUITool.Models;
using FFGUITool.Services;

namespace FFGUITool.ViewModels
{
    public partial class MainWindowViewModel
    {
        private bool _isPreparingExecution;
        [RelayCommand]
        private async Task Execute()
        {
            await RunExecution(ExecutionScope.All, failedOnly: false);
        }

        [RelayCommand]
        private async Task ExecuteCurrent()
        {
            await RunExecution(ExecutionScope.Current, failedOnly: false);
        }

        [RelayCommand]
        private async Task RetryFailed()
        {
            await RunExecution(ExecutionScope.All, failedOnly: true);
        }

        [RelayCommand]
        private void CancelExecution()
        {
            _executionCancellation?.Cancel();
            CanCancel = false;
            ProgressText = LocalizationService.T("Queue.Cancelling");
        }

        [RelayCommand]
        private void ApplyCurrentSettingsToAll()
        {
            FlushSliderUpdate();
            SaveSelectedSourceTabSettings();
            var source = _processingWorkspace.SelectedIndependentTask;
            if (source == null || SourceTabs.Count < 2)
            {
                return;
            }

            foreach (var target in SourceTabs.Where(item => !ReferenceEquals(item, source)))
            {
                CopySourceTabSettings(source, target);
            }

            BatchModeText = LocalizationService.Format("SourceTabs.AppliedToAll", SourceTabs.Count - 1);
            IsInputStatusVisible = true;
            UpdateExecuteAllText();
        }

        [RelayCommand]
        private void IncludeAllBatchTasks()
        {
            foreach (var task in BatchTasks)
            {
                task.IsIncluded = true;
            }

            RefreshBatchTaskSelection();
        }

        [RelayCommand]
        private void ExcludeAllBatchTasks()
        {
            foreach (var task in BatchTasks)
            {
                task.IsIncluded = false;
            }

            RefreshBatchTaskSelection();
        }

        [RelayCommand]
        private void RefreshBatchTaskSelection()
        {
            BatchFileCount = _batchSummary.IncludedCount;
            CanExecute = HasSelectedInput && _ffmpegManager.IsFFmpegAvailable && BatchFileCount > 0 && !IsProcessing;
            UpdateExecuteAllText();
            RefreshBatchModeSummary();
            UpdateCommand();
        }

        private async Task RunExecution(ExecutionScope scope, bool failedOnly)
        {
            FlushSliderUpdate();
            if (IsProcessing || _isPreparingExecution || IsScanning || IsPreviewing || !_ffmpegManager.IsFFmpegAvailable)
            {
                return;
            }

            _isPreparingExecution = true;
            try
            {
                if (!await ConfirmOutputConflictPolicy(scope, failedOnly)) return;
            }
            catch (Exception ex) { ValidationMessage = ex.Message; return; }
            finally { _isPreparingExecution = false; }

            IsProcessing = true;
            CanExecute = false;
            IsProgressVisible = true;
            ProgressValue = 0;
            _progressTotals.Clear();
            PersistWorkspace();
            ProgressText = "";
            CanCancel = true;
            _executionCancellation = new CancellationTokenSource();

            try
            {
                ClearLastFailure();
                var tasks = GetTasksForExecution(scope, failedOnly);
                var options = new ProcessingExecutionOptions
                {
                    OutputConflictPolicy = _outputConflictPolicy,
                    AvailableVideoDecoders = _availableVideoDecoders,
                    ImageParallelism = ImageParallelism
                };
                // Freeze on the UI thread before starting any background work.
                var frozen = tasks.Select(task => task.CreateSnapshot()).ToArray();
                var originals = frozen.Select((task, index) => (task, original: tasks[index]))
                    .ToDictionary(pair => pair.task, pair => pair.original);
                using var progress = new UiProcessingProgress(value =>
                    ApplyProcessingProgress(value with { Task = originals[value.Task] }));
                var token = _executionCancellation.Token;
                var summary = await Task.Run(() => _processingExecutor.ExecuteAsync(frozen, options, progress, token));
                summary = summary with
                {
                    Failures = summary.Failures.Select(failure => failure with { Task = originals[failure.Task] }).ToArray()
                };
                var message = ProcessingResultFormatter.FormatSummary(summary);
                CanRetryFailed = summary.Failures.Count > 0;
                CaptureExecutionFailures(summary);
                var title = summary.Failures.Count > 0
                    ? LocalizationService.T("Queue.CompletedWithErrors")
                    : summary.WasCancelled
                        ? LocalizationService.T("Queue.Cancelled")
                        : LocalizationService.T("Dialog.Done");
                SystemNotificationService.Show(title, message, summary.Failures.Count > 0);
                RecordResults(summary, frozen);
                ProgressText = $"{title} · {summary.Results.Count}/{tasks.Count}";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unexpected execution error.", ex);
                LastFailureDetails = ex.ToString();
                LastFailureCommand = CommandText;
                IsFailureActionsVisible = true;
                var message = LocalizationService.Format("Dialog.ExecuteError", ex.Message);
                SystemNotificationService.Show(LocalizationService.T("Dialog.Error"), message, isError: true);
                await _dialogService.ShowMessage(LocalizationService.T("Dialog.Error"), message);
            }
            finally
            {
                IsProcessing = false;
                UpdateCommand();
                IsProgressVisible = true;
                CanCancel = false;
                _executionCancellation?.Dispose();
                _executionCancellation = null;
                UpdateExecuteAllText();
            }
        }

        private void UpdateExecutionProgress(int completed, int total, string fileName)
        {
            ProgressValue = total <= 0 ? 0 : completed * 100.0 / total;
            ProgressText = LocalizationService.Format("Queue.Progress", completed, total, fileName);
        }

        private void UpdateExecuteAllText()
        {
            var count = IsBatchMode
                ? _batchSummary.IncludedCount
                : SourceTabs.Count > 0
                    ? SourceTabs.Count
                    : HasSelectedInput ? 1 : 0;
            ExecuteAllText = count > 1
                ? LocalizationService.Format("Queue.ProcessAll", count)
                : LocalizationService.T("Main.StartConvert");
        }

        private async Task<bool> ConfirmOutputConflictPolicy(ExecutionScope scope, bool failedOnly)
        {
            UpdateCommand();
            if (!CanExecute && !failedOnly) return false;
            SaveSelectedSourceTabSettings();
            var tasks = GetTasksForExecution(scope, failedOnly);
            var outputPaths = _processingExecutor.GetPlannedOutputPaths(
                tasks,
                new ProcessingExecutionOptions { AvailableVideoDecoders = _availableVideoDecoders });
            var conflictCount = outputPaths.Count(System.IO.File.Exists) +
                                outputPaths.GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
                                    .Sum(group => Math.Max(0, group.Count() - 1));
            if (conflictCount == 0)
            {
                _outputConflictPolicy = OutputConflictPolicy.AutoRename;
                return true;
            }

            var result = await _dialogService.ShowActionMessage(
                LocalizationService.T("OutputConflict.Title"),
                LocalizationService.Format("OutputConflict.Message", conflictCount),
                new[]
                {
                    ("Rename", LocalizationService.T("OutputConflict.AutoRename")),
                    ("Overwrite", LocalizationService.T("OutputConflict.Overwrite")),
                    ("Cancel", LocalizationService.T("Dialog.Cancel"))
                });
            _outputConflictPolicy = result == "Overwrite" ? OutputConflictPolicy.Overwrite : OutputConflictPolicy.AutoRename;
            return result is "Rename" or "Overwrite";
        }

        private IReadOnlyList<ProcessingTask> GetTasksForExecution(ExecutionScope scope, bool failedOnly)
        {
            if (!IsBatchMode && SourceTabs.Count > 0)
            {
                return _processingWorkspace.GetExecutionTasks(
                    sharedMode: false,
                    selection: scope == ExecutionScope.Current ? ProcessingTaskSelection.Current : ProcessingTaskSelection.All,
                    failedOnly: failedOnly);
            }

            if (IsBatchMode)
            {
                RefreshSharedTaskPolicy();
                return _processingWorkspace.GetExecutionTasks(
                    sharedMode: true,
                    selection: ProcessingTaskSelection.All,
                    failedOnly: failedOnly);
            }

            return failedOnly
                ? Array.Empty<ProcessingTask>()
                : new[]
                {
                    new ProcessingTask(
                        CompressionSettings.InputPath,
                        CompressionSettings.Clone(),
                        ProcessingSettingsScope.Independent)
                };
        }

        private void RefreshSharedTaskPolicy()
        {
            foreach (var task in BatchTasks)
            {
                ApplySharedTaskPolicy(task);
            }
        }

        private void ApplySharedTaskPolicy(ProcessingTask task)
        {
            task.Settings = CompressionSettings;
            task.EditorState = Editor.CaptureState();
            task.HasSettings = true;
            task.UsesRelativeTarget = true;
            task.RelativeTargetPercentage = TargetSizeMB;
            task.MinimumVideoBitrateKbps = SelectedCompressionPresetOption?.MinVideoBitrateKbps ?? 0;
            task.MaximumVideoBitrateKbps = SelectedCompressionPresetOption?.MaxVideoBitrateKbps ?? 0;
        }

        private void ApplyProcessingProgress(ProcessingProgress progress)
        {
            var task = progress.Task;
            task.State = progress.State;
            switch (progress.State)
            {
                case ProcessingTaskState.Running:
                    task.Status = LocalizationService.T("SourceTabs.Processing");
                    task.StatusColor = "Blue";
                    task.Message = "";
                    break;
                case ProcessingTaskState.Warning:
                case ProcessingTaskState.Completed:
                    task.Status = LocalizationService.T(progress.State == ProcessingTaskState.Warning ? "Improve.Warning" : "SourceTabs.Completed");
                    task.StatusColor = progress.State == ProcessingTaskState.Warning ? "Orange" : "Green";
                    task.OutputPath = progress.OutputPath;
                    task.Message = progress.Message;
                    task.IsFailed = false;
                    break;
                case ProcessingTaskState.Failed:
                    task.Status = LocalizationService.T("SourceTabs.Failed");
                    task.StatusColor = "Red";
                    task.Message = progress.Message;
                    task.IsFailed = true;
                    break;
                case ProcessingTaskState.Cancelled:
                    task.Status = LocalizationService.T("Queue.Cancelled");
                    task.StatusColor = "Gray";
                    break;
            }

            ProgressValue = _progressTotals.Apply(progress);
            ProgressText = LocalizationService.Format("Improve.Progress", progress.CompletedCount, progress.TotalCount, task.FileName,
                progress.Fraction * 100, progress.Speed, TimeSpan.FromSeconds(Math.Clamp(progress.RemainingSeconds, 0, 864000)).ToString(@"hh\:mm\:ss"), progress.Message);
            if (progress.State != ProcessingTaskState.Running) ScheduleWorkspaceSave();
        }

        private void CaptureExecutionFailures(ProcessingExecutionSummary summary)
        {
            if (summary.Failures.Count == 0)
            {
                return;
            }

            var details = new System.Collections.Generic.List<string>();
            foreach (var failure in summary.Failures)
            {
                details.Add(failure.Task.FileName);
                details.Add(failure.Exception is ProcessingExecutionException executionException
                    ? ProcessingResultFormatter.FormatFailureDetails(executionException)
                    : failure.Exception.ToString());
                details.Add("");
            }

            LastFailureDetails = string.Join(Environment.NewLine, details);
            LastFailureCommand = string.Join(Environment.NewLine, summary.Failures.Select(f => f.Exception).OfType<ProcessingExecutionException>().Select(e => e.CommandText));
            IsFailureActionsVisible = true;
            AppLogger.Error($"Queue completed with {summary.Failures.Count} failure(s).{Environment.NewLine}{LastFailureDetails}");
        }
    }
}
