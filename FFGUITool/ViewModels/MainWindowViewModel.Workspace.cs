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
    private readonly WorkspaceStore _workspaceStore = new();
    private WorkspaceDocument _savedWorkspace = new();
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _queueTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private string _appliedQueueSearch = "";
    private readonly HashSet<ProcessingTask> _observedQueueTasks = new();
    private readonly DispatcherTimer _sliderTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private Action? _pendingSliderUpdate;
    private bool _featuresReady;
    private bool _restoringWorkspace;
    private bool _workspaceActivated;
    private bool _syncingGoal;
    private CancellationTokenSource? _scanCancellation;
    private CancellationTokenSource? _inputCancellation;
    private readonly SemaphoreSlim _inputGate = new(1, 1);
    private readonly Dictionary<ProcessingTask, double> _taskFractions = new();

    [ObservableProperty] private bool _limitFileSize = true;
    [ObservableProperty] private bool _twoPass;
    [ObservableProperty] private bool _allowImageResize;
    [ObservableProperty] private int _pngCompressionLevel = 6;
    public bool IsPngImage => IsImageMode && (EnableFormatConversion ? SelectedImageFormatOption?.Value == "png" : Path.GetExtension(CompressionSettings.InputPath).ToLowerInvariant() is ".png" or ".bmp");
    [ObservableProperty] private bool _streamCopy;
    [ObservableProperty] private string _outputNamePattern = "{name}_FFGUIToolOutPut_{label}";
    [ObservableProperty] private bool _preserveFolderStructure = true;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasValidationMessage))] private string _validationMessage = "";
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _hasSavedQueue;
    [ObservableProperty] private bool _closeToTray;
    [ObservableProperty] private int _imageParallelism = 2;
    [ObservableProperty] private string _presetName = "";
    [ObservableProperty] private SavedPreset? _selectedSavedPreset;
    [ObservableProperty] private HistoryEntry? _selectedHistory;
    [ObservableProperty] private string _queueSearch = "";
    [ObservableProperty] private bool _failedTasksOnly;
    [ObservableProperty] private bool _sortTasksByName;
    [ObservableProperty] private string _resultSummary = "";
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _isPreviewing;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasMediaNotice))] private string _mediaNotice = "";
    public bool HasMediaNotice => !string.IsNullOrWhiteSpace(MediaNotice);
    [ObservableProperty] private CodecOption? _selectedGoal;
    public List<CodecOption> GoalOptions { get; private set; } = new();
    public ObservableCollection<SavedPreset> SavedPresets { get; } = new();
    public ObservableCollection<HistoryEntry> History { get; } = new();
    public ObservableCollection<ProcessingTask> VisibleTasks { get; } = new();
    public bool HasQueue => SourceTabs.Count > 0 || BatchTasks.Count > 0;
    public bool IsQueueVisible => IsBatchMode ? BatchTasks.Count > 0 : SourceTabs.Count > 5;
    public bool UseSourceTabs => !IsBatchMode && SourceTabs.Count is > 0 and <= 5;
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
        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); _appliedQueueSearch = QueueSearch; RefreshVisibleTasks(); };
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
            case nameof(QueueSearch):
                _searchTimer.Stop(); _searchTimer.Start(); return true;
            case nameof(FailedTasksOnly): case nameof(SortTasksByName):
                RefreshVisibleTasks(); return true;
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
        SelectedGoal = GoalOptions.Find(option => option.Value == (CompressionSettings.UseCrf ? "quality" : CompressionSettings.LimitFileSize ? "size" : "bitrate"));
    }

    private void ScheduleWorkspaceSave()
    {
        if (!_featuresReady || _restoringWorkspace || _isLoadingSourceTab) return;
        _saveTimer.Stop(); _saveTimer.Start();
    }

    private void PersistWorkspace()
    {
        if (!_featuresReady || _restoringWorkspace || _isLoadingSourceTab) return;
        try
        {
            if (_workspaceActivated || HasSelectedInput || HasQueue)
            {
                SaveSelectedSourceTabSettings();
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
            _workspaceStore.Save(_savedWorkspace);
        }
        catch (Exception ex) { AppLogger.Error("Workspace save failed.", ex); ValidationMessage = LocalizationService.T("Improve.SaveFailed"); }
    }

    private void RefreshVisibleTasks()
    {
        _queueTimer.Stop();
        var source = IsBatchMode ? BatchTasks : SourceTabs;
        var current = source.ToHashSet();
        foreach (var removed in _observedQueueTasks.Where(task => !current.Contains(task)).ToArray())
        { removed.PropertyChanged -= OnQueueTaskChanged; _observedQueueTasks.Remove(removed); }
        foreach (var task in source)
            if (_observedQueueTasks.Add(task)) task.PropertyChanged += OnQueueTaskChanged;
        IEnumerable<ProcessingTask> tasks = source;
        tasks = tasks.Where(t => (!FailedTasksOnly || t.IsFailed) &&
            (string.IsNullOrWhiteSpace(_appliedQueueSearch) || t.InputPath.Contains(_appliedQueueSearch, StringComparison.OrdinalIgnoreCase)));
        if (SortTasksByName) tasks = tasks.OrderBy(t => t.FileName, StringComparer.CurrentCultureIgnoreCase);
        QueueViewUpdater.Update(VisibleTasks, tasks.ToList());
        OnPropertyChanged(nameof(HasQueue));
        OnPropertyChanged(nameof(IsQueueVisible));
        OnPropertyChanged(nameof(UseSourceTabs));
    }

    private void OnQueueTaskChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (FailedTasksOnly && e.PropertyName == nameof(ProcessingTask.IsFailed))
        { _queueTimer.Stop(); _queueTimer.Start(); }
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
        while (IsProcessing || IsScanning || IsPreviewing) await Task.Delay(50);
        PersistWorkspace();
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
            HasSettings = true, IsAdvancedMode = true, TargetSizeMB = settings.IsImageProcessing ? settings.ImageTargetSizeKB : settings.TargetSizeMB,
            Bitrate = settings.IsImageProcessing ? settings.ImageQuality : settings.Bitrate, UseCrf = settings.UseCrf, Crf = settings.Crf,
            SelectedVideoFormatValue = settings.OutputFormat, SelectedAudioFormatValue = settings.AudioOutputFormat,
            SelectedImageFormatValue = settings.ImageOutputFormat, SelectedAudioBitrateValue = settings.AudioBitrate.ToString(),
            SelectedAudioTrackModeValue = settings.AudioTrackMode, SelectedResolutionValue = settings.ResolutionHeight.ToString(),
            SelectedCodecValue = settings.Codec, SelectedHardwareEncoderValue = settings.AllowHardwareFallback ? "auto" : settings.HardwareEncoder
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
                if (task.IsFailed) { task.Status = LocalizationService.T("SourceTabs.Failed"); task.Message = LocalizationService.T("Improve.MissingInput"); }
                if (folder) BatchTasks.Add(task); else SourceTabs.Add(task);
            }
            if (folder)
            {
                CompressionSettings.InputPath = folderPath;
                SetInputPathText(folderPath);
                HasSelectedInput = Directory.Exists(folderPath);
                if (saved.Count > 0) ApplySavedSettings(saved[0].Settings);
                CompressionSettings.InputPath = folderPath;
                IsBatchTaskListVisible = true;
                RefreshBatchTaskSelection();
            }
            else if (SourceTabs.FirstOrDefault(t => !t.IsFailed) is { } selected)
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
        _workspaceStore.Save(_savedWorkspace);
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
        ResultSummary = ProcessingResultFormatter.FormatSummary(summary);
        HasResults = true;
        foreach (var result in summary.Results)
        {
            MediaFileSupport.GeneratedPaths.Add(Path.GetFullPath(result.OutputPath));
            History.Insert(0, new HistoryEntry
            {
                InputPath = result.InputPath, OutputPath = result.OutputPath, FinishedAt = DateTime.Now,
                BeforeBytes = result.InputInfo?.FileSize ?? 0, AfterBytes = result.OutputInfo?.FileSize ?? 0,
                Warning = result.Warning, Settings = tasks.First(t => t.InputPath == result.InputPath).Settings.Clone()
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
