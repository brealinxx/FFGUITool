using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFGUITool.Models;
using FFGUITool.Services;

namespace FFGUITool.ViewModels;

public partial class MainWindowViewModel
{
    private readonly WorkspaceStore _workspaceStore;
    private readonly WorkspacePersistence _workspacePersistence;
    private bool _exitSaved;
    private WorkspaceDocument _savedWorkspace = new();
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _queueTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly DispatcherTimer _sliderTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private Action? _pendingSliderUpdate;
    private bool _featuresReady;
    private bool _restoringWorkspace;
    private bool _workspaceActivated;
    private bool _syncingGoal;
    private CancellationTokenSource? _scanCancellation;
    private CancellationTokenSource? _inputCancellation;
    private readonly SemaphoreSlim _inputGate = new(1, 1);
    private readonly ProcessingProgressTotals _progressTotals = new();

    public bool LimitFileSize { get => Editor.LimitFileSize; set => Editor.LimitFileSize = value; }
    public bool TwoPass { get => Editor.TwoPass; set => Editor.TwoPass = value; }
    public bool AllowImageResize { get => Editor.AllowImageResize; set => Editor.AllowImageResize = value; }
    public int PngCompressionLevel { get => Editor.PngCompressionLevel; set => Editor.PngCompressionLevel = value; }
    public bool IsPngImage => IsImageMode && (EnableFormatConversion ? SelectedImageFormatOption?.Value == "png" : Path.GetExtension(CompressionSettings.InputPath).ToLowerInvariant() is ".png" or ".bmp");
    public bool StreamCopy { get => Editor.StreamCopy; set => Editor.StreamCopy = value; }
    public string OutputNamePattern { get => Output.OutputNamePattern; set => Output.OutputNamePattern = value; }
    public bool PreserveFolderStructure { get => Output.PreserveFolderStructure; set => Output.PreserveFolderStructure = value; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasValidationMessage))] private string _validationMessage = "";
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _hasSavedQueue;
    [ObservableProperty] private bool _closeToTray;
    [ObservableProperty] private int _imageParallelism = 2;
    [ObservableProperty] private string _presetName = "";
    [ObservableProperty] private SavedPreset? _selectedSavedPreset;
    [ObservableProperty] private HistoryEntry? _selectedHistory;
    public string QueueSearch { get => Queue.Search; set => Queue.Search = value; }
    public bool FailedTasksOnly { get => Queue.FailedOnly; set => Queue.FailedOnly = value; }
    public bool SortTasksByName { get => Queue.SortByName; set => Queue.SortByName = value; }
    [ObservableProperty] private string _resultSummary = "";
    [ObservableProperty] private bool _hasResults;
    private (int Success, int Warning, int Failed, int Cancelled) _resultCounts;
    public string ResultCountsText => LocalizationService.Format("Improve.ResultCounts", _resultCounts.Success, _resultCounts.Warning, _resultCounts.Failed, _resultCounts.Cancelled);
    [ObservableProperty] private bool _isPreviewing;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasMediaNotice))] private string _mediaNotice = "";
    public bool HasMediaNotice => !string.IsNullOrWhiteSpace(MediaNotice);
    public CodecOption? SelectedGoal { get => Editor.SelectedGoal; set => Editor.SelectedGoal = value; }
    public List<CodecOption> GoalOptions { get => Editor.GoalOptions; private set => Editor.GoalOptions = value; }
    public ObservableCollection<SavedPreset> SavedPresets { get; } = new();
    public ObservableCollection<HistoryEntry> History { get; } = new();
    public ObservableCollection<ProcessingTask> VisibleTasks => Queue.Tasks;
    public bool HasQueue => SourceTabs.Count > 0 || BatchTasks.Count > 0;
    public bool IsQueueVisible => IsBatchMode ? BatchTasks.Count > 0 : SourceTabs.Count > 5;
    public bool UseSourceTabs => !IsBatchMode && SourceTabs.Count is > 0 and <= 5;
    public string ApplyToAllText => LocalizationService.Format("Improve.ApplyAllCount", Math.Max(0, SourceTabs.Count - 1));
    public string SettingsScopeText => LocalizationService.Format(IsBatchMode ? "Improve.ScopeShared" : "Improve.ScopeIndependent",
        Path.GetFileName(CompressionSettings.InputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
    private MediaImportReport? _lastImportReport;
    private int _lastImportCount;
    private bool _lastImportCancelled;
    public string ImportSummary => _lastImportReport?.Format(_lastImportCount, _lastImportCancelled) ?? "";
    public bool HasImportSummary => _lastImportReport != null;
    private void SetImportSummary(MediaImportReport? report, int count = 0, bool cancelled = false)
    {
        _lastImportReport = report; _lastImportCount = count; _lastImportCancelled = cancelled;
        OnPropertyChanged(nameof(ImportSummary)); OnPropertyChanged(nameof(HasImportSummary));
    }
    public string QualityHelp => LocalizationService.T(IsPngImage ? "Improve.PngHelp" : "Improve.QualityHelp");

    private void InitializeFeatures()
    {
        _savedWorkspace = _workspaceStore.Load();
        GoalOptions = new()
        {
            new(LocalizationService.T("Improve.SizeMode"), "size", ""),
            new(LocalizationService.T("Improve.QualityMode"), "quality", ""),
            new(LocalizationService.T("Improve.BitrateMode"), "bitrate", "")
        };
        SelectedGoal = GoalOptions[0];
        foreach (var preset in _savedWorkspace.Presets) SavedPresets.Add(preset);
        foreach (var entry in _savedWorkspace.History) History.Add(entry);
        foreach (var path in _savedWorkspace.GeneratedPaths) MediaFileSupport.GeneratedPaths.Add(path);
        HasSavedQueue = _savedWorkspace.Tasks.Any(t => t.State is not (ProcessingTaskState.Completed or ProcessingTaskState.Warning));
        CloseToTray = _appConfig.CloseToTray;
        ImageParallelism = Math.Clamp(_appConfig.ImageParallelism, 1, 4);
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); PersistWorkspace(); };
        _sliderTimer.Tick += (_, _) => FlushSliderUpdate();
        _queueTimer.Tick += (_, _) => { _queueTimer.Stop(); RefreshVisibleTasks(); };
        SourceTabs.CollectionChanged += (_, _) => { _queueTimer.Stop(); _queueTimer.Start(); ScheduleWorkspaceSave(); };
        BatchTasks.CollectionChanged += (_, _) => { if (!IsScanning) { _queueTimer.Stop(); _queueTimer.Start(); } };
        _featuresReady = true;
    }

    private bool HandleFeatureChange(string? property)
    {
        if (!_featuresReady || _isRestoringSourceTab) return false;
        switch (property)
        {
            case nameof(SelectedGoal):
                if (SelectedGoal == null || _syncingGoal) return true;
                _syncingGoal = true;
                try
                {
                    LimitFileSize = SelectedGoal.Value == "size";
                    UseCrf = !IsImageMode && SelectedGoal.Value == "quality";
                }
                finally { _syncingGoal = false; }
                if (SelectedGoal.Value != "size") IsAdvancedMode = true;
                UpdateCommand(); return true;
            case nameof(LimitFileSize): case nameof(TwoPass): case nameof(AllowImageResize): case nameof(PngCompressionLevel):
            case nameof(StreamCopy): case nameof(OutputNamePattern): case nameof(PreserveFolderStructure):
                ApplyFeatureSettings(); UpdateCommand(); return true;
            case nameof(CloseToTray): case nameof(ImageParallelism):
                var config = AppConfigService.Load();
                config.CloseToTray = CloseToTray; config.ImageParallelism = Math.Clamp(ImageParallelism, 1, 4);
                AppConfigService.Save(config); return true;
            case nameof(IsBatchMode): case nameof(InputPathText):
                OnPropertyChanged(nameof(SettingsScopeText)); break;
        }
        return false;
    }

    private void CoalesceSliderUpdate(Action update)
    {
        if (_isSyncingCompressionValues || _isRestoringSourceTab || _isLoadingSourceTab) return;
        _pendingSliderUpdate = update;
        _sliderTimer.Stop(); _sliderTimer.Start();
    }

    private void FlushSliderUpdate()
    {
        _sliderTimer.Stop();
        var update = _pendingSliderUpdate; _pendingSliderUpdate = null;
        update?.Invoke();
    }

    private void RefreshGoalOptions()
    {
        var value = SelectedGoal?.Value ?? "size";
        GoalOptions = new()
        {
            new(LocalizationService.T("Improve.SizeMode"), "size", ""),
            new(LocalizationService.T("Improve.QualityMode"), "quality", ""),
            new(LocalizationService.T("Improve.BitrateMode"), "bitrate", "")
        };
        OnPropertyChanged(nameof(GoalOptions));
        SelectedGoal = GoalOptions.Find(option => option.Value == value);
        UpdateMediaNotice();
        OnPropertyChanged(nameof(SettingsScopeText));
        OnPropertyChanged(nameof(ImportSummary));
        OnPropertyChanged(nameof(ApplyToAllText));
        OnPropertyChanged(nameof(ResultCountsText));
    }

    private void ApplyFeatureSettings()
    {
        CompressionSettings.LimitFileSize = LimitFileSize;
        CompressionSettings.TwoPass = TwoPass;
        CompressionSettings.AllowImageResize = AllowImageResize;
        CompressionSettings.PngCompressionLevel = PngCompressionLevel;
        CompressionSettings.StreamCopy = StreamCopy;
        CompressionSettings.AllowHardwareFallback = SelectedHardwareEncoderOption?.Value == "auto";
        CompressionSettings.OutputNamePattern = OutputNamePattern;
        CompressionSettings.PreserveFolderStructure = PreserveFolderStructure;
        CompressionSettings.InputRoot = IsBatchMode ? CompressionSettings.InputPath : "";
    }

    private void RestoreFeatureSettings()
    {
        LimitFileSize = CompressionSettings.LimitFileSize;
        TwoPass = CompressionSettings.TwoPass;
        AllowImageResize = CompressionSettings.AllowImageResize;
        PngCompressionLevel = CompressionSettings.PngCompressionLevel;
        StreamCopy = CompressionSettings.StreamCopy;
        OutputNamePattern = CompressionSettings.OutputNamePattern;
        PreserveFolderStructure = CompressionSettings.PreserveFolderStructure;
        SelectedGoal = GoalOptions.Find(option => option.Value == (CompressionSettings.UseCrf ? "quality" : CompressionSettings.LimitFileSize ? "size" : IsImageMode ? "quality" : "bitrate"));
    }

    private void ScheduleWorkspaceSave()
    {
        if (!_featuresReady || _restoringWorkspace || _isLoadingSourceTab) return;
        _saveTimer.Stop(); _saveTimer.Start();
    }

    private void OnWorkspaceSaveFailed(Exception error)
    {
        AppLogger.Error("Workspace save failed.", error);
        Dispatcher.UIThread.Post(() => ValidationMessage = LocalizationService.T("Improve.SaveFailed"));
    }

    private bool PersistWorkspace()
    {
        if (!_featuresReady || _restoringWorkspace || _isLoadingSourceTab) return false;
        try
        {
            if (_workspaceActivated || HasSelectedInput || HasQueue)
            {
                SaveSelectedSourceTabSettings();
                if (IsBatchMode) RefreshSharedTaskPolicy();
                _savedWorkspace.ImageMode = IsImageMode;
                _savedWorkspace.FolderMode = IsBatchMode;
                _savedWorkspace.FolderPath = IsBatchMode ? CompressionSettings.InputPath : "";
                _savedWorkspace.Tasks = (IsBatchMode ? BatchTasks : SourceTabs).ToList();
                if (IsImageMode) _savedWorkspace.RecentImageSettings = CompressionSettings.Clone();
                else _savedWorkspace.RecentVideoSettings = CompressionSettings.Clone();
            }
            _savedWorkspace.Presets = SavedPresets.ToList();
            _savedWorkspace.History = History.Take(100).ToList();
            _savedWorkspace.GeneratedPaths = MediaFileSupport.GeneratedPaths.ToList();
            _workspacePersistence.Enqueue(WorkspacePersistence.Capture(_savedWorkspace));
            return true;
        }
        catch (Exception ex) { AppLogger.Error("Workspace save failed.", ex); ValidationMessage = LocalizationService.T("Improve.SaveFailed"); return false; }
    }

    private void RefreshVisibleTasks()
    {
        _queueTimer.Stop();
        Queue.Refresh(IsBatchMode);
        OnPropertyChanged(nameof(HasQueue));
        OnPropertyChanged(nameof(IsQueueVisible));
        OnPropertyChanged(nameof(UseSourceTabs));
        OnPropertyChanged(nameof(ApplyToAllText));
        OnPropertyChanged(nameof(ResultCountsText));
    }

    [RelayCommand]
    private async Task ShowConfigCheck()
    {
        await _dialogService.ShowScrollableMessage(LocalizationService.T("Menu.ConfigCheck"),
            $"FFmpeg: {_ffmpegManager.FFmpegPath}\nExifTool: {_exifToolManager.IsExifToolAvailable}\n" +
            $"Encoders: {string.Join(", ", _availableVideoEncoders)}\nConfig: {AppConfigService.AppDataPath}");
    }

    [RelayCommand]
    private async Task ShowSourceDetails()
    {
        if (CurrentVideoInfo is not { } info) return;
        await _dialogService.ShowScrollableMessage(LocalizationService.T("SourceDetails.Title"),
            $"{info.FilePath}\n{info.FormattedFileSize} · {info.FormattedDuration}\n{info.Resolution} · {info.Framerate:0.###} fps\n" +
            $"Video: {info.VideoCodec}\nAudio: {info.AudioCodec} · {info.AudioBitrate} kb/s\nRotation: {info.Rotation}°\n" +
            $"{info.PixelFormat} · {info.ColorSpace}\n\n{info.MetadataSummary}");
    }

    [RelayCommand]
    private async Task SelectAnyFile()
    {
        if (IsProcessing || IsScanning) return;
        var files = await _dialogService.OpenFilesDialog(LocalizationService.T("Improve.SelectAny"), null);
        if (files.Count > 0) await ProcessSelectedInputs(files.Select(file => file.Path.LocalPath));
    }

    public async Task<bool> PrepareForExitAsync()
    {
        if (IsProcessing && !await _dialogService.ShowConfirmation(LocalizationService.T("Improve.Exit"), LocalizationService.T("Improve.ExitRunning"))) return false;
        _executionCancellation?.Cancel(); _scanCancellation?.Cancel(); _inputCancellation?.Cancel(); _previewCancellation?.Cancel();
        _previewWindow?.Close();
        while (IsProcessing || IsScanning || IsPreviewing || _isLoadingSourceTab) await Task.Delay(50);
        await _inputGate.WaitAsync();
        _inputGate.Release();
        FlushSliderUpdate();
        _saveTimer.Stop();
        if (!PersistWorkspace()) return false;
        if (await _workspacePersistence.FlushAsync() is { } error)
        {
            AppLogger.Error("Final workspace save failed.", error);
            ValidationMessage = LocalizationService.T("Improve.SaveFailed");
            return false;
        }
        _exitSaved = true;
        return true;
    }

    [RelayCommand]
    private void SavePreset()
    {
        FlushSliderUpdate();
        if (string.IsNullOrWhiteSpace(PresetName)) { ValidationMessage = LocalizationService.T("Improve.PresetNameRequired"); return; }
        var settings = CompressionSettings.Clone();
        settings.InputPath = ""; settings.InputRoot = ""; settings.OutputLabel = "";
        var existing = SavedPresets.FirstOrDefault(p => p.Name == PresetName.Trim());
        if (existing != null) SavedPresets.Remove(existing);
        var preset = new SavedPreset { Name = PresetName.Trim(), Settings = settings };
        SavedPresets.Add(preset); SelectedSavedPreset = preset;
        ValidationMessage = ""; PersistWorkspace();
    }

    [RelayCommand]
    private void DeletePreset()
    {
        if (SelectedSavedPreset != null) SavedPresets.Remove(SelectedSavedPreset);
        SelectedSavedPreset = null; PersistWorkspace();
    }

    [RelayCommand]
    private void ApplyPreset()
    {
        if (SelectedSavedPreset == null) return;
        ApplySavedSettings(SelectedSavedPreset.Settings);
    }

    [RelayCommand]
    private void ApplyRecentSettings()
    {
        var recent = IsImageMode ? _savedWorkspace.RecentImageSettings : _savedWorkspace.RecentVideoSettings;
        if (recent != null) ApplySavedSettings(recent);
    }

    private void ApplySavedSettings(CompressionSettings settings)
    {
        if (settings.IsImageProcessing != IsImageMode)
        { ValidationMessage = LocalizationService.T("Improve.PresetModeMismatch"); return; }
        var task = new ProcessingTask(CompressionSettings.InputPath, settings.Clone(), ProcessingSettingsScope.Independent)
        {
            HasSettings = true, EditorState = TaskEditorState.FromSettings(settings)
        };
        RestoreSourceTabSettings(task);
    }

    [RelayCommand]
    private async Task RestoreQueue()
    {
        if (IsProcessing || !HasSavedQueue) return;
        var saved = _savedWorkspace.Tasks.Where(t => t.State is not (ProcessingTaskState.Completed or ProcessingTaskState.Warning)).ToList();
        var folder = _savedWorkspace.FolderMode;
        var folderPath = _savedWorkspace.FolderPath;
        var image = _savedWorkspace.ImageMode;
        _restoringWorkspace = true;
        _workspaceActivated = true;
        try
        {
            if (image) SelectImageMode(); else SelectVideoMode();
            _processingWorkspace.Clear();
            IsBatchMode = folder;
            foreach (var task in saved)
            {
                task.State = ProcessingTaskState.Pending;
                task.Status = LocalizationService.T("SourceTabs.Pending");
                task.StatusColor = "Gray";
                task.IsFailed = !File.Exists(task.InputPath);
                task.IsIncluded = task.IsIncluded && !task.IsFailed;
                if (task.IsFailed) { task.State = ProcessingTaskState.Failed; task.Status = LocalizationService.T("SourceTabs.Failed"); task.StatusColor = "Red"; task.Message = LocalizationService.T("Improve.MissingInput"); }
                if (folder) BatchTasks.Add(task); else SourceTabs.Add(task);
            }
            if (folder)
            {
                CompressionSettings.InputPath = folderPath;
                SetInputPathText(folderPath);
                HasSelectedInput = Directory.Exists(folderPath);
                TargetSizeSliderMinimum = 1;
                TargetSizeSliderMaximum = 100;
                if (saved.Count > 0)
                {
                    var first = saved[0];
                    var sharedEditor = new ProcessingTask(folderPath, first.Settings.Clone(), ProcessingSettingsScope.Shared)
                    {
                        HasSettings = true,
                        EditorState = first.HasSettings ? first.EditorState.Clone() : TaskEditorState.FromSettings(first.Settings)
                    };
                    // Older workspaces stored the folder ratio only in the execution policy.
                    sharedEditor.TargetSizeMB = Math.Clamp(first.UsesRelativeTarget ? first.RelativeTargetPercentage : first.Settings.CompressionPercentage, 1, 100);
                    RestoreSourceTabSettings(sharedEditor);
                }
                CompressionSettings.InputPath = folderPath;
                IsBatchTaskListVisible = true;
                RefreshBatchTaskSelection();
            }
            else if ((SourceTabs.FirstOrDefault(t => t.IsSelected && !t.IsFailed) ?? SourceTabs.FirstOrDefault(t => !t.IsFailed)) is { } selected)
            {
                IsSourceTabsVisible = true;
                await SelectSourceTabCore(selected, true);
            }
            HasSavedQueue = false; RefreshVisibleTasks();
        }
        catch (Exception ex) { AppLogger.Error("Queue restore failed.", ex); ValidationMessage = ex.Message; }
        finally { _restoringWorkspace = false; }
        ScheduleWorkspaceSave();
    }

    [RelayCommand]
    private void DiscardSavedQueue()
    {
        _savedWorkspace.Tasks.Clear(); HasSavedQueue = false;
        _workspacePersistence.Enqueue(WorkspacePersistence.Capture(_savedWorkspace));
    }

    [RelayCommand]
    private void CancelScan() => _scanCancellation?.Cancel();

    [RelayCommand]
    private void OpenResult(HistoryEntry? entry)
    {
        entry ??= SelectedHistory;
        if (entry != null && File.Exists(entry.OutputPath)) OpenUrl(entry.OutputPath);
    }

    [RelayCommand]
    private void OpenResultFolder(HistoryEntry? entry)
    {
        entry ??= SelectedHistory;
        if (entry != null && Directory.Exists(Path.GetDirectoryName(entry.OutputPath))) OpenFolderInShell(Path.GetDirectoryName(entry.OutputPath)!);
    }

    [RelayCommand]
    private async Task ReuseHistory()
    {
        if (SelectedHistory == null || IsProcessing) return;
        var entry = SelectedHistory;
        if (!File.Exists(entry.InputPath)) { ValidationMessage = LocalizationService.T("Improve.MissingInput"); return; }
        if (entry.Settings.IsImageProcessing) SelectImageMode(); else SelectVideoMode();
        await ProcessSelectedInputs(new[] { entry.InputPath });
        ApplySavedSettings(entry.Settings);
    }

    private void RecordResults(ProcessingExecutionSummary summary, IReadOnlyList<ProcessingTask> tasks)
    {
        var warnings = summary.Results.Count(result => !string.IsNullOrEmpty(result.Warning));
        _resultCounts = (summary.Results.Count - warnings, warnings, summary.Failures.Count,
            summary.WasCancelled ? Math.Max(0, tasks.Count - summary.Results.Count - summary.Failures.Count) : 0);
        OnPropertyChanged(nameof(ResultCountsText));
        ResultSummary = ProcessingResultFormatter.FormatSummary(summary);
        HasResults = true;
        var settingsByPath = tasks.ToDictionary(task => task.InputPath, task => task.Settings, PathIdentity.Comparer);
        foreach (var result in summary.Results)
        {
            MediaFileSupport.GeneratedPaths.Add(Path.GetFullPath(result.OutputPath));
            History.Insert(0, new HistoryEntry
            {
                InputPath = result.InputPath, OutputPath = result.OutputPath, FinishedAt = DateTime.Now,
                BeforeBytes = result.InputInfo?.FileSize ?? 0, AfterBytes = result.OutputInfo?.FileSize ?? 0,
                Warning = result.Warning, Settings = settingsByPath[result.InputPath].Clone()
            });
        }
        while (History.Count > 100) History.RemoveAt(History.Count - 1);
        SelectedHistory = History.FirstOrDefault();
        RefreshVisibleTasks(); PersistWorkspace();
    }

    private void UpdateMediaNotice()
    {
        var notices = new List<string>();
        if (IsImageMode && (CurrentVideoInfo?.IsAnimated == true || Path.GetExtension(CompressionSettings.InputPath).Equals(".gif", StringComparison.OrdinalIgnoreCase)))
            notices.Add(LocalizationService.T("Improve.AnimationWarning"));
        if (IsImageMode && CompressionSettings.ImageOutputFormat == "jpg" && CurrentVideoInfo?.PixelFormat is { } format &&
            (format.Contains("rgba") || format.Contains("argb") || format.Contains("yuva") || format.Contains("gbrap") || format.StartsWith("ya", StringComparison.Ordinal) || format == "pal8")) notices.Add(LocalizationService.T("Improve.AlphaWarning"));
        if (CurrentVideoInfo?.ColorSpace.Contains("smpte2084", StringComparison.OrdinalIgnoreCase) == true)
            notices.Add(LocalizationService.T("Improve.HdrWarning"));
        MediaNotice = string.Join(" · ", notices);
        OnPropertyChanged(nameof(QualityHelp));
        OnPropertyChanged(nameof(IsPngImage));
    }

    private double GetEstimateDuration()
    {
        if (CurrentVideoInfo == null) return 0;
        try { return EncodingPolicy.OutputDuration(CompressionSettings, CurrentVideoInfo); }
        catch (ArgumentException) { return CurrentVideoInfo.Duration; }
    }

    private void RefreshStatusBrushes()
    {
        OnPropertyChanged(nameof(FfmpegStatusColor));
        OnPropertyChanged(nameof(EstimatedBitrateColor));
        foreach (var task in SourceTabs.Concat(BatchTasks)) task.RefreshStatusBrush();
    }

    [RelayCommand]
    private async Task PreviewSample()
    {
        FlushSliderUpdate();
        if (IsProcessing || IsPreviewing || !File.Exists(CompressionSettings.InputPath)) return;
        IsPreviewing = true;
        _previewCancellation = new CancellationTokenSource();
        try
        {
            var service = new PreviewService(_ffmpegManager, _processingExecutor, _videoAnalyzer);
            using var preview = await service.CreateAsync(CompressionSettings, _availableVideoDecoders, _previewCancellation.Token);
            var owner = _dialogService.GetMainWindow();
            if (owner != null)
            {
                _previewWindow = new Views.PreviewWindow(preview);
                await _previewWindow.ShowDialog(owner);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ValidationMessage = ex.Message; }
        finally { _previewWindow = null; _previewCancellation.Dispose(); _previewCancellation = null; IsPreviewing = false; }
    }
    private Views.PreviewWindow? _previewWindow;
    private CancellationTokenSource? _previewCancellation;
    [RelayCommand] private void CancelPreview() => _previewCancellation?.Cancel();
}
