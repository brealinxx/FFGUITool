using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FFGUITool.Models;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.ViewModels
{
    public class BatchProcessViewModel : ViewModelBase
    {
        private readonly IBatchProcessor _batchProcessor;
        private readonly IMediaAnalyzer _mediaAnalyzer;
        
        private bool _isProcessing;
        private int _parallelTasks = 1;
        private double _overallProgress;
        private string _statusMessage = "";
        private ProcessingOptions _processingOptions;
        private CancellationTokenSource? _cancellationTokenSource;

        public BatchProcessViewModel(IBatchProcessor batchProcessor, IMediaAnalyzer mediaAnalyzer)
        {
            _batchProcessor = batchProcessor;
            _mediaAnalyzer = mediaAnalyzer;
            
            Tasks = new ObservableCollection<ProcessingTask>();
            _processingOptions = new ProcessingOptions
            {
                Type = ProcessingType.VideoCompression,
                VideoOptions = new VideoCompressionOptions
                {
                    TargetBitrate = 2000,
                    VideoCodec = "libx264",
                    AudioCodec = "aac",
                    CompressionPercentage = 70
                }
            };
            
            AddFilesCommand = new RelayCommand(async _ => await AddFilesAsync());
            AddFolderCommand = new RelayCommand(async _ => await AddFolderAsync());
            RemoveSelectedCommand = new RelayCommand(RemoveSelected, _ => SelectedTask != null);
            ClearAllCommand = new RelayCommand(_ => Tasks.Clear(), _ => Tasks.Count > 0);
            ClearCompletedCommand = new RelayCommand(ClearCompleted, _ => Tasks.Any(t => t.Status == TaskStatus.Completed));
            StartAllCommand = new RelayCommand(async _ => await StartAllAsync(), _ => !IsProcessing && Tasks.Count > 0);
            PauseAllCommand = new RelayCommand(_ => PauseAll(), _ => IsProcessing);
            StartTaskCommand = new RelayCommand(async param => await StartTaskAsync(param as ProcessingTask));
            CancelTaskCommand = new RelayCommand(param => CancelTask(param as ProcessingTask));
        }

        public ObservableCollection<ProcessingTask> Tasks { get; }

        private ProcessingTask? _selectedTask;
        public ProcessingTask? SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (SetProperty(ref _selectedTask, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int ParallelTasks
        {
            get => _parallelTasks;
            set
            {
                if (SetProperty(ref _parallelTasks, value))
                {
                    _batchProcessor.MaxParallelTasks = value;
                }
            }
        }

        public double OverallProgress
        {
            get => _overallProgress;
            set => SetProperty(ref _overallProgress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ProcessingOptions ProcessingOptions
        {
            get => _processingOptions;
            set => SetProperty(ref _processingOptions, value);
        }

        public int TotalTasks => Tasks.Count;
        public int CompletedTasks => Tasks.Count(t => t.Status == TaskStatus.Completed);
        public int FailedTasks => Tasks.Count(t => t.Status == TaskStatus.Failed);
        public int PendingTasks => Tasks.Count(t => t.Status == TaskStatus.Pending);

        public ICommand AddFilesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand RemoveSelectedCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand ClearCompletedCommand { get; }
        public ICommand StartAllCommand { get; }
        public ICommand PauseAllCommand { get; }
        public ICommand StartTaskCommand { get; }
        public ICommand CancelTaskCommand { get; }

        private async Task AddFilesAsync()
        {
            // This will be called from the View with selected files
            // The View handles the file dialog
        }

        public async Task AddFiles(string[] files)
        {
            foreach (var file in files)
            {
                if (!Tasks.Any(t => t.InputFile == file))
                {
                    var task = new ProcessingTask
                    {
                        InputFile = file,
                        OutputFile = GenerateOutputPath(file),
                        Options = ProcessingOptions,
                        Status = TaskStatus.Pending
                    };
                    
                    Tasks.Add(task);
                }
            }
            
            UpdateStatistics();
        }

        private async Task AddFolderAsync()
        {
            // This will be called from the View with selected folder
            // The View handles the folder dialog
        }

        public async Task AddFolder(string folderPath)
        {
            StatusMessage = "扫描文件夹...";
            
            var extensions = ProcessingOptions.Type switch
            {
                ProcessingType.VideoCompression => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" },
                ProcessingType.AudioConversion => new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma" },
                _ => new[] { "*.*" }
            };
            
            var files = await Task.Run(() => _batchProcessor.ScanFolder(folderPath, extensions, true));
            await AddFiles(files.ToArray());
            
            StatusMessage = $"已添加 {files.Count()} 个文件";
        }

        private void RemoveSelected(object? parameter)
        {
            if (SelectedTask != null && !SelectedTask.Status.Equals(TaskStatus.Processing))
            {
                Tasks.Remove(SelectedTask);
                UpdateStatistics();
            }
        }

        private void ClearCompleted(object? parameter)
        {
            var completed = Tasks.Where(t => t.Status == TaskStatus.Completed).ToList();
            foreach (var task in completed)
            {
                Tasks.Remove(task);
            }
            UpdateStatistics();
        }

        private async Task StartAllAsync()
        {
            if (IsProcessing || Tasks.Count == 0)
                return;

            IsProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            StatusMessage = "开始批量处理...";
            
            try
            {
                var progress = new Progress<BatchProgress>(p =>
                {
                    OverallProgress = p.OverallProgress;
                    StatusMessage = $"处理中: {p.CompletedTasks}/{p.TotalTasks} - {Path.GetFileName(p.CurrentFile)}";
                    UpdateStatistics();
                });

                // Update all pending tasks to queued
                foreach (var task in Tasks.Where(t => t.Status == TaskStatus.Pending))
                {
                    task.Status = TaskStatus.Queued;
                }

                var files = Tasks.Select(t => t.InputFile).ToList();
                var result = await _batchProcessor.ProcessFileListAsync(
                    files,
                    ProcessingOptions,
                    progress,
                    _cancellationTokenSource.Token);

                StatusMessage = $"处理完成: 成功 {result.SuccessCount}, 失败 {result.FailureCount}";
                
                // Update task statuses based on results
                foreach (var task in Tasks)
                {
                    if (result.Errors.Any(e => e.FilePath == task.InputFile))
                    {
                        task.Status = TaskStatus.Failed;
                        task.ErrorMessage = result.Errors.First(e => e.FilePath == task.InputFile).ErrorMessage;
                    }
                    else if (task.Status == TaskStatus.Queued)
                    {
                        task.Status = TaskStatus.Completed;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "批量处理已取消";
                foreach (var task in Tasks.Where(t => t.Status == TaskStatus.Queued))
                {
                    task.Status = TaskStatus.Cancelled;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                UpdateStatistics();
            }
        }

        private void PauseAll()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task StartTaskAsync(ProcessingTask? task)
        {
            if (task == null || task.Status == TaskStatus.Processing)
                return;

            task.Status = TaskStatus.Processing;
            task.StartTime = DateTime.Now;
            
            // Process single task
            // Implementation would use the appropriate processor based on task type
            
            task.EndTime = DateTime.Now;
            task.Status = TaskStatus.Completed;
            UpdateStatistics();
        }

        private void CancelTask(ProcessingTask? task)
        {
            if (task != null && task.Status == TaskStatus.Processing)
            {
                task.Status = TaskStatus.Cancelled;
                UpdateStatistics();
            }
        }

        private string GenerateOutputPath(string inputFile)
        {
            var directory = string.IsNullOrEmpty(ProcessingOptions.OutputDirectory)
                ? Path.GetDirectoryName(inputFile) ?? ""
                : ProcessingOptions.OutputDirectory;

            var fileName = Path.GetFileNameWithoutExtension(inputFile);
            var extension = Path.GetExtension(inputFile);

            var outputFileName = ProcessingOptions.OutputFilePattern
                .Replace("{name}", fileName)
                .Replace("{ext}", extension);

            return Path.Combine(directory, outputFileName);
        }

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalTasks));
            OnPropertyChanged(nameof(CompletedTasks));
            OnPropertyChanged(nameof(FailedTasks));
            OnPropertyChanged(nameof(PendingTasks));
        }
    }
}