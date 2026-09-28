using System.Diagnostics;
using System.Collections.Specialized;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FFGUITool.Services;
using FFGUITool.Models;
using System.Reflection;
using FFGUITool.ViewModels;
using FFGUITool.Views;

internal static class Program
{
    private static string _artifacts = "";
    private static readonly Dictionary<string, object> Metrics = new();
    private static int _exitCode;
    private static bool _recoveryOnly;

    [STAThread]
    public static int Main(string[] args)
    {
        _artifacts = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine("TestResults", "ui-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));
        _recoveryOnly = args.Contains("--recovery-only");
        Directory.CreateDirectory(_artifacts);
        Environment.SetEnvironmentVariable("FFGUITOOL_APP_DATA", Path.Combine(_artifacts, "config"));
        AppBuilder.Configure<FFGUITool.App>().UsePlatformDetect().AfterSetup(_ =>
            Dispatcher.UIThread.Post(Run)).StartWithClassicDesktopLifetime(Array.Empty<string>());
        return _exitCode;
    }

    private static async void Run()
    {
        MainWindow? window = null;
        try
        {
            var desktop = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
            await WaitFor(() => desktop.MainWindow is MainWindow { IsVisible: true }, TimeSpan.FromSeconds(60));
            window = (MainWindow)desktop.MainWindow!;
            var vm = (MainWindowViewModel)window.DataContext!;
            var ffmpeg = new FFmpegManager();
            await ffmpeg.InitializeAsync();
            Require(ffmpeg.IsFFmpegAvailable, "FFmpeg required for UI samples");
            await WaitFor(() => vm.IsInitialized, TimeSpan.FromSeconds(60));
            Metrics["scenario"] = _recoveryOnly ? "recovery" : "full";
            Metrics["render_scaling"] = window.RenderScaling;
            var media = Path.Combine(_artifacts, "media");
            Directory.CreateDirectory(media);
            var seed = Path.Combine(_artifacts, "seed.png");
            var result = await ProcessRunner.RunAsync(ffmpeg.FFmpegPath,
                new[] { "-v", "error", "-f", "lavfi", "-i", "color=c=blue:size=64x64", "-frames:v", "1", "-y", seed });
            Require(result.ExitCode == 0, result.Error);
            var files = Enumerable.Range(0, _recoveryOnly ? 2 : 1000).Select(i => Path.Combine(media, $"sample-{i:0000}.png")).ToArray();
            await Task.Run(() => { foreach (var file in files) File.Copy(seed, file, true); });
            if (_recoveryOnly)
            {
                await CheckWorkspaceRecovery(ffmpeg, files);
                Metrics["passed"] = true;
                return;
            }
            MeasureQueueCollections();
            await Measure(window, "cache_hot_after_513", async () =>
            {
                var analyzer = new VideoAnalyzer(ffmpeg);
                for (var i = 0; i < 512; i++) await analyzer.AnalyzeVideo(files[i]);
                var hot = await analyzer.AnalyzeVideo(files[0]);
                await analyzer.AnalyzeVideo(files[512]);
                var start = Stopwatch.StartNew();
                var again = await analyzer.AnalyzeVideo(files[0]);
                Metrics["cache_hot_retained"] = ReferenceEquals(hot, again);
                Metrics["cache_hot_lookup_ms"] = start.Elapsed.TotalMilliseconds;
            });
            vm.SelectImageModeCommand.Execute(null);
            await Task.Delay(300);
            var resets = 0;
            vm.VisibleTasks.CollectionChanged += (_, e) => { if (e.Action == NotifyCollectionChangedAction.Reset) resets++; };
            var tick = Stopwatch.StartNew();
            double maxUiGap = 0;
            string queueStage = "import", maxQueueGapStage = "";
            var heartbeat = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            heartbeat.Tick += (_, _) => { if (tick.Elapsed.TotalMilliseconds > maxUiGap) { maxUiGap = tick.Elapsed.TotalMilliseconds; maxQueueGapStage = queueStage; } tick.Restart(); };
            heartbeat.Start();
            var elapsed = Stopwatch.StartNew();
            await vm.ProcessSelectedInputs(files.Reverse());
            await Frame(window);
            Metrics["independent_import_1000_ms"] = elapsed.Elapsed.TotalMilliseconds;
            Require(vm.SourceTabs.Count == 1000 && vm.VisibleTasks.Count == 1000, "Independent import count");
            var list = window.GetVisualDescendants().OfType<ListBox>().First(l => ReferenceEquals(l.ItemsSource, vm.VisibleTasks));
            Metrics["realized_queue_items"] = list.GetVisualDescendants().OfType<ListBoxItem>().Count();
            Require((int)Metrics["realized_queue_items"] > 0 && (int)Metrics["realized_queue_items"] < 100, "Queue virtualization");
            queueStage = "selection";
            var unchangedContainer = list.GetVisualDescendants().OfType<ListBoxItem>().First();
            vm.Queue.Refresh(vm.IsBatchMode);
            await Frame(window);
            Require(list.GetVisualDescendants().OfType<ListBoxItem>().Contains(unchangedContainer), "No-op refresh retains realized containers");
            var first = vm.SourceTabs[0];
            list.SelectedItem = vm.SourceTabs[1];
            await WaitFor(() => vm.SourceTabs[1].IsSelected);
            Require(ReferenceEquals(list.SelectedItem, vm.SourceTabs.Single(t => t.IsSelected)), "List and editor select the same task");
            await vm.SelectSourceTabCommand.ExecuteAsync(first);
            Require(ReferenceEquals(list.SelectedItem, first), "Programmatic selection updates list");
            vm.Bitrate = 43;
            Require(!ReferenceEquals(first.Settings, vm.SourceTabs[1].Settings), "Independent settings identity");
            await vm.SelectSourceTabCommand.ExecuteAsync(vm.SourceTabs[1]);
            await vm.SelectSourceTabCommand.ExecuteAsync(first);
            Require(first.Settings.ImageQuality == 43, "Independent settings survive switching");
            Require(vm.SettingsScopeText.Contains(Path.GetFileName(first.InputPath)), "Scope names current file");
            vm.CloseToTray = true; vm.ImageParallelism = 3;
            vm.PresetName = "UI regression preset";
            vm.SavePresetCommand.Execute(null);
            vm.ApplyPresetCommand.Execute(null);
            Require(vm.CloseToTray && vm.ImageParallelism == 3, "Presets preserve app settings");
            var savedConfig = AppConfigService.Load();
            Require(savedConfig.CloseToTray && savedConfig.ImageParallelism == 3, "App preferences persisted to isolated config");

            queueStage = "search";
            var search = window.GetVisualDescendants().OfType<TextBox>().First(t => t.Watermark?.ToString() == LocalizationService.T("Improve.Search"));
            elapsed.Restart();
            search.Text = "s"; search.Text = "sample-09"; search.Text = "sample-099";
            Require(vm.VisibleTasks.Count == 1000, "Search waits for debounce");
            await WaitFor(() => vm.VisibleTasks.Count == 10);
            await Frame(window);
            Metrics["search_1000_to_10_including_debounce_ms"] = elapsed.Elapsed.TotalMilliseconds;
            elapsed.Restart();
            search.Text = "";
            await WaitFor(() => vm.VisibleTasks.Count == 1000);
            await Frame(window);
            Metrics["clear_search_ms"] = elapsed.Elapsed.TotalMilliseconds;
            elapsed.Restart();
            queueStage = "sort";
            vm.SortTasksByName = true;
            await Frame(window);
            Require(ReferenceEquals(list.SelectedItem, first) && first.IsSelected, "Sort preserves editor and list selection");
            Metrics["sort_1000_ms"] = elapsed.Elapsed.TotalMilliseconds;
            queueStage = "continuous_edit";
            await Measure(window, "independent_edit_and_autosave_1000", async () =>
            {
                for (var i = 0; i < 40; i++) { vm.BitrateSliderValue = 35 + i; await Task.Delay(25); }
                await Task.Delay(1200);
            });
            queueStage = "save";
            await Measure(window, "preset_save_1000", async () =>
            {
                var saveWatch = Stopwatch.StartNew();
                vm.SavePresetCommand.Execute(null);
                Metrics["preset_save_ui_call_ms"] = saveWatch.Elapsed.TotalMilliseconds;
                await Task.Delay(700);
            });
            queueStage = "progress";
            await Measure(window, "progress_1000", async () =>
            {
                var apply = typeof(MainWindowViewModel).GetMethod("ApplyProcessingProgress", BindingFlags.Instance | BindingFlags.NonPublic)!;
                using var progress = new UiProcessingProgress(p => apply.Invoke(vm, new object[] { p }));
                await Task.Run(() =>
                {
                    for (var i = 0; i < files.Length; i++)
                    {
                        for (var step = 0; step < 10; step++)
                            progress.Report(new ProcessingProgress(vm.SourceTabs[i], ProcessingTaskState.Running, i, files.Length, Fraction: step / 10.0));
                        progress.Report(new ProcessingProgress(vm.SourceTabs[i], ProcessingTaskState.Completed, i + 1, files.Length));
                    }
                });
                Require(vm.SourceTabs.All(t => t.State == ProcessingTaskState.Completed), "All terminal progress applied before summary");
            });
            queueStage = "failure_filter";
            vm.FailedTasksOnly = true;
            Require(vm.VisibleTasks.Count == 0, "Empty failure filter");
            first.IsFailed = true;
            await WaitFor(() => vm.VisibleTasks.Count == 1);
            Require(ReferenceEquals(first, vm.VisibleTasks[0]), "Failure filter reacts to task state");
            vm.FailedTasksOnly = false;
            first.IsFailed = false;
            queueStage = "scroll";
            list.ScrollIntoView(vm.VisibleTasks[999]);
            await Frame(window);
            Metrics["realized_items_after_scroll"] = list.GetVisualDescendants().OfType<ListBoxItem>().Count();
            list.ScrollIntoView(first);

            queueStage = "mixed_import";
            var wrongType = Path.Combine(_artifacts, "video.mp4");
            File.WriteAllText(wrongType, "not analyzed in image mode");
            await vm.ProcessSelectedInputs(new[] { first.InputPath, wrongType, Path.Combine(_artifacts, "missing.png") });
            Require(vm.SourceTabs.Count == 1000 && vm.ImportSummary.Contains("3"), "Mixed import skips remain visible");

            heartbeat.Stop();
            Metrics["queue_operations_max_ui_heartbeat_gap_ms"] = maxUiGap;
            Metrics["queue_operations_max_ui_gap_stage"] = maxQueueGapStage;
            vm.IsAdvancedMode = true;
            vm.EnableFormatConversion = true;
            foreach (var language in new[] { "zh-CN", "en-US" })
            foreach (var dark in new[] { false, true })
            {
                LocalizationService.SetLanguage(language, false);
                Require(!vm.IsBitrateWarningVisible, "Image quality must not show a video bitrate warning");
                Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
                window.Width = 760; window.Height = 560;
                await Task.Delay(300);
                await Frame(window);
                SaveWindow(window, $"{language}-{(dark ? "dark" : "light")}-760x560-input.png");
                var scroll = window.FindControl<ScrollViewer>("WorkspaceScroll")!;
                scroll.Offset = new Vector(0, window.FindControl<Border>("ParametersCard")!.Bounds.Y + 100);
                await Frame(window);
                SaveWindow(window, $"{language}-{(dark ? "dark" : "light")}-760x560-settings.png");
                scroll.Offset = new Vector(0, window.FindControl<Border>("OutputSettingsCard")!.Bounds.Y);
                await Frame(window);
                SaveWindow(window, $"{language}-{(dark ? "dark" : "light")}-760x560-output.png");
                scroll.Offset = default;
            }
            foreach (var size in new[] { (Width: 960, Height: 800), (Width: 1360, Height: 900) })
            foreach (var language in new[] { "zh-CN", "en-US" })
            foreach (var dark in new[] { false, true })
            {
                LocalizationService.SetLanguage(language, false);
                Require(!vm.IsBitrateWarningVisible, "Image quality must not show a video bitrate warning");
                Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
                window.Width = size.Width; window.Height = size.Height;
                await Task.Delay(200);
                await Frame(window);
                SaveWindow(window, $"{language}-{(dark ? "dark" : "light")}-{size.Width}x{size.Height}.png");
            }
            window.Width = 960; window.Height = 800;
            await Frame(window);
            maxUiGap = 0; tick.Restart(); heartbeat.Start();
            elapsed.Restart();
            await vm.ProcessSelectedInputs(new[] { media });
            await Frame(window);
            Metrics["folder_import_1000_ms"] = elapsed.Elapsed.TotalMilliseconds;
            Require(vm.BatchTasks.Count == 1000 && vm.VisibleTasks.Count == 1000, "Folder count");
            Require(vm.BatchTasks.All(t => ReferenceEquals(t.Settings, vm.CompressionSettings)), "Shared folder settings identity");
            Require(vm.SettingsScopeText.Contains(Path.GetFileName(media)), "Scope names folder");
            SaveWindow(window, "folder-1000-960x800.png");
            heartbeat.Stop();
            Metrics["folder_import_max_ui_heartbeat_gap_ms"] = maxUiGap;
            await Measure(window, "folder_edit_and_autosave_1000", async () =>
            {
                for (var i = 0; i < 40; i++) { vm.BitrateSliderValue = 35 + i; await Task.Delay(25); }
                await Task.Delay(1200);
            });
            var cancelledScan = false;
            NotifyCollectionChangedEventHandler cancel = (_, _) =>
            {
                if (!cancelledScan && vm.IsScanning && vm.VisibleTasks.Count <= 100)
                { cancelledScan = true; vm.CancelScanCommand.Execute(null); }
            };
            vm.VisibleTasks.CollectionChanged += cancel;
            await vm.ProcessSelectedInputs(new[] { media });
            vm.VisibleTasks.CollectionChanged -= cancel;
            Require(cancelledScan && !vm.IsScanning && vm.BatchTasks.Count is > 0 and < 1000, "Scan cancellation retains partial queue");
            await vm.ProcessSelectedInputs(new[] { media });
            Require(vm.BatchTasks.Count == 1000 && vm.VisibleTasks.Count == 1000, "Rescan restores complete queue");
            Require(vm.ImportSummary == LocalizationService.Format("Improve.ImportSummary", 1000, 0), "Rescan clears stale cancellation and skip counts");
            Metrics["queue_reset_notifications"] = resets;
            Require(resets == 0, "No queue Reset notifications");
            vm.ExcludeAllBatchTasksCommand.Execute(null);
            foreach (var task in vm.BatchTasks.Take(32)) task.IsIncluded = true;
            vm.RefreshBatchTaskSelectionCommand.Execute(null);
            vm.SelectedGoal = vm.GoalOptions.First(g => g.Value == "quality");
            vm.OutputPathText = Path.Combine(_artifacts, "output");
            Directory.CreateDirectory(vm.OutputPathText);
            await Measure(window, "short_images_32", () => vm.ExecuteCommand.ExecuteAsync(null));
            Require(vm.BatchTasks.Take(32).All(t => t.State is ProcessingTaskState.Completed or ProcessingTaskState.Warning), "Real short image batch completes");
            window.Width = 760; window.Height = 560;
            await Frame(window);
            var resultScroll = window.FindControl<ScrollViewer>("WorkspaceScroll")!;
            resultScroll.Offset = new Vector(0, window.FindControl<Border>("ResultsCard")!.Bounds.Y);
            await Frame(window);
            SaveWindow(window, "results-en-US-dark-760x560.png");
            vm.ValidationMessage = "Long path validation: " + string.Join("/", Enumerable.Repeat("very-long-synthetic-test-directory", 12));
            vm.IsFailureActionsVisible = true;
            await Frame(window);
            SaveWindow(window, "error-en-US-dark-760x560.png");
            Require(resultScroll.Viewport.Height >= 100, "Error details leave usable editing space");
            vm.ValidationMessage = ""; vm.IsFailureActionsVisible = false;
            resultScroll.Offset = default;
            var preferences = new AppPreferencesWindow { DataContext = vm };
            preferences.Show(window);
            await Frame(preferences);
            SaveWindow(preferences, "application-settings.png");
            preferences.Close();
            var videoResult = await ProcessRunner.RunAsync(ffmpeg.FFmpegPath,
                new[] { "-v", "error", "-f", "lavfi", "-i", "color=c=blue:size=128x96:rate=24", "-t", "1", "-c:v", "libx264", "-y", wrongType });
            Require(videoResult.ExitCode == 0, videoResult.Error);
            vm.SelectVideoModeCommand.Execute(null);
            await vm.ProcessSelectedInputs(new[] { wrongType });
            vm.IsAdvancedMode = true; vm.EnableFormatConversion = true;
            window.Width = 760; window.Height = 560;
            await Task.Delay(300);
            await Frame(window);
            var editorScroll = window.FindControl<ScrollViewer>("WorkspaceScroll")!;
            var codec = window.GetVisualDescendants().OfType<ComboBox>().First(c => ReferenceEquals(c.ItemsSource, vm.CodecOptions));
            var point = codec.TranslatePoint(default, (Visual)editorScroll.Content!);
            editorScroll.Offset = new Vector(0, point!.Value.Y - 30);
            await Frame(window);
            Require(codec.Bounds.Width > 0 && codec.Bounds.Width < 650, "Video selector fits narrow editor");
            SaveWindow(window, "video-codec-en-US-dark-760x560.png");
            await CheckWorkspaceRecovery(ffmpeg, files);
            Require(await vm.PrepareForExitAsync(), "Final workspace save succeeds");
            Metrics["render_scaling"] = window.RenderScaling;
            Metrics["passed"] = true;
        }
        catch (Exception ex)
        {
            _exitCode = 1;
            Metrics["error"] = ex.ToString();
            if (window != null)
            {
                try { SaveWindow(window, "failure.png"); }
                catch (Exception screenshotError) { Metrics["screenshot_error"] = screenshotError.Message; }
            }
        }
        finally
        {
            var json = JsonSerializer.Serialize(Metrics, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(_artifacts, "metrics.json"), json);
            Console.WriteLine(json);
            if (window != null) { window.AllowClose = true; window.Close(); }
            ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).Shutdown(_exitCode);
        }
    }

    private static async Task CheckWorkspaceRecovery(FFmpegManager ffmpeg, string[] files)
    {
        var path = Path.Combine(_artifacts, "recovery-workspace.json");
        var store = new WorkspaceStore(path);
        var settings = new CompressionSettings { IsImageProcessing = true, LimitFileSize = false, ImageQuality = 67 };
        var doc = new WorkspaceDocument { ImageMode = true, Tasks = new()
        {
            new ProcessingTask(files[0], settings.Clone(), ProcessingSettingsScope.Independent) { HasSettings = true, Bitrate = 67, IsSelected = true },
            new ProcessingTask(Path.Combine(_artifacts, "missing-recovery.png"), settings.Clone(), ProcessingSettingsScope.Independent)
        }};
        // A non-first selection exercises restoration without overwriting either editor.
        doc.Tasks.Insert(0, new ProcessingTask(files[1], settings.Clone(), ProcessingSettingsScope.Independent)
        { HasSettings = true, Bitrate = 42 });
        doc.Tasks[0].Settings.ImageQuality = 42;
        store.Save(doc);
        var unopened = new MainWindowViewModel(ffmpeg, new DialogService(), store);
        Require(unopened.HasSavedQueue, "Old queue is offered before activation");
        Require(await unopened.PrepareForExitAsync(), "Unopened queue save");
        unopened.Dispose();
        Require(store.Load().Tasks.Count == 3, "Startup save preserves unopened old queue");
        var restored = new MainWindowViewModel(ffmpeg, new DialogService(), store);
        await restored.RestoreQueueCommand.ExecuteAsync(null);
        Require(restored.SourceTabs.Count == 3 && restored.SourceTabs[2].IsFailed && !restored.SourceTabs[2].IsIncluded && restored.SourceTabs[2].StatusColor == "Red", "Missing input is excluded");
        Require(restored.SourceTabs[1].IsSelected && restored.SourceTabs[0].Settings.ImageQuality == 42, "Restoration preserves selected task and other task settings");
        Require(restored.Bitrate == 67 && restored.CompressionSettings.ImageQuality == 67, "Selected task retains saved quality on restore");
        Require(!restored.LimitFileSize && !restored.UseCrf && restored.SelectedGoal?.Value == "quality", "Restored image quality goal agrees with settings");
        restored.SelectedGoal = restored.GoalOptions.First(g => g.Value == "quality");
        restored.PresetName = "quality recovery";
        restored.SavePresetCommand.Execute(null);
        restored.SelectedGoal = restored.GoalOptions.First(g => g.Value == "size");
        restored.ApplyPresetCommand.Execute(null);
        Require(!restored.LimitFileSize && !restored.CompressionSettings.LimitFileSize && restored.SelectedGoal?.Value == "quality", "Preset restores the processing goal");
        restored.BitrateSliderValue = 51;
        restored.ApplyCurrentSettingsToAllCommand.Execute(null);
        Require(restored.SourceTabs.All(t => t.Settings.ImageQuality == 51 && t.Bitrate == 51), "Apply to all flushes the pending slider value");
        Require(restored.SourceTabs[2].IsFailed && !restored.SourceTabs[2].IsIncluded && restored.SourceTabs[2].State == ProcessingTaskState.Failed,
            "Applying parameters preserves the missing-input failure");
        restored.SourceTabs[1].State = ProcessingTaskState.Completed;
        restored.SourceTabs[1].Status = LocalizationService.T("SourceTabs.Completed");
        var previousLanguage = LocalizationService.CurrentLanguage;
        LocalizationService.SetLanguage(previousLanguage == "en-US" ? "zh-CN" : "en-US", false);
        Require(restored.SourceTabs[1].State == ProcessingTaskState.Completed && restored.Bitrate == 51 && restored.SelectedGoal?.Value == "quality",
            "Changing language preserves task state and image quality");
        LocalizationService.SetLanguage(previousLanguage, false);
        foreach (var task in restored.SourceTabs.ToArray()) await restored.CloseSourceTabCommand.ExecuteAsync(task);
        Require(await restored.PrepareForExitAsync(), "Cleared queue save");
        restored.Dispose();
        var restarted = new MainWindowViewModel(ffmpeg, new DialogService(), store);
        Require(!restarted.HasSavedQueue && store.Load().Tasks.Count == 0, "Cleared queue stays empty after restart");
        restarted.Dispose();
        // Version 1 shared tasks persisted relative targets without an editor snapshot.
        var folderSettings = settings.Clone();
        folderSettings.CompressionPercentage = 37;
        folderSettings.TargetSizeMB = 37;
        folderSettings.ImageTargetSizeKB = 0;
        store.Save(new WorkspaceDocument
        {
            ImageMode = true, FolderMode = true, FolderPath = Path.GetDirectoryName(files[0])!,
            Tasks = new() { new ProcessingTask(files[0], folderSettings, ProcessingSettingsScope.Shared)
                { UsesRelativeTarget = true, RelativeTargetPercentage = 37 } }
        });
        var folderRestored = new MainWindowViewModel(ffmpeg, new DialogService(), store);
        await folderRestored.RestoreQueueCommand.ExecuteAsync(null);
        Require(folderRestored.IsBatchMode && folderRestored.TargetSizeMB == 37 && folderRestored.Bitrate == 67,
            "Shared recovery preserves relative target even when size limiting is disabled");
        Require(folderRestored.TargetSizeSliderMinimum == 1 && folderRestored.TargetSizeSliderMaximum == 100 && !folderRestored.IsImageTargetUnitSelectorVisible,
            "Shared recovery configures percentage controls");
        Require(await folderRestored.PrepareForExitAsync(), "Shared recovery save");
        folderRestored.Dispose();
        var folderSaved = store.Load().Tasks.Single();
        Require(folderSaved.HasSettings && folderSaved.Bitrate == 67 && folderSaved.RelativeTargetPercentage == 37,
            "Shared policy saves its editor snapshot");
        var folderRestarted = new MainWindowViewModel(ffmpeg, new DialogService(), store);
        await folderRestarted.RestoreQueueCommand.ExecuteAsync(null);
        Require(folderRestarted.TargetSizeMB == 37 && folderRestarted.Bitrate == 67 && folderRestarted.SelectedGoal?.Value == "quality",
            "Shared editor survives a second save and restore");
        folderRestarted.Dispose();
        Metrics["workspace_recovery_checks"] = true;
    }

    private static void MeasureQueueCollections()
    {
        foreach (var count in new[] { 1000, 10000 })
        {
            var tasks = Enumerable.Range(0, count).Select(i => new ProcessingTask($"{i}.png", new(), ProcessingSettingsScope.Independent)).ToArray();
            var visible = new QueueTaskCollection(tasks);
            var moves = 0;
            visible.CollectionChanged += (_, e) => { if (e.Action == NotifyCollectionChangedAction.Move) moves++; };
            var watch = Stopwatch.StartNew();
            QueueViewUpdater.Update(visible, tasks.Reverse().ToArray());
            Metrics[$"collection_reverse_{count}"] = new { elapsed_ms = watch.Elapsed.TotalMilliseconds, moves };
            watch.Restart();
            QueueViewUpdater.Update(visible, tasks.Take(10).ToArray());
            QueueViewUpdater.Update(visible, tasks);
            Metrics[$"collection_filter_restore_{count}_ms"] = watch.Elapsed.TotalMilliseconds;
        }
    }

    private static async Task Measure(Window window, string name, Func<Task> action)
    {
        var tick = Stopwatch.StartNew();
        double maxGap = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) => { maxGap = Math.Max(maxGap, tick.Elapsed.TotalMilliseconds); tick.Restart(); };
        var watch = Stopwatch.StartNew();
        timer.Start();
        try { await action(); await Frame(window); }
        finally
        {
            timer.Stop();
            Metrics[name] = new { elapsed_ms = watch.Elapsed.TotalMilliseconds, max_ui_gap_ms = Math.Max(maxGap, tick.Elapsed.TotalMilliseconds),
                managed_bytes = GC.GetTotalMemory(false), working_set_bytes = Process.GetCurrentProcess().WorkingSet64 };
        }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static async Task WaitFor(Func<bool> condition, TimeSpan? timeout = null)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition()) { Require(elapsed.Elapsed < (timeout ?? TimeSpan.FromSeconds(10)), "UI condition timed out"); await Task.Delay(10); }
    }
    private static async Task Frame(Window window)
    {
        window.UpdateLayout();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        await Task.Delay(40);
        window.UpdateLayout();
    }
    private static void SaveWindow(Window window, string name)
    {
        var workspace = (window as MainWindow)?.FindControl<Grid>("WorkspaceRoot");
        if (workspace != null)
            Require(workspace.Bounds.Right <= window.ClientSize.Width && workspace.Bounds.Bottom <= window.ClientSize.Height,
                "Workspace fits the resized window: " + name + " " + workspace.Bounds);
        Metrics["layout_" + name] = new { client = window.ClientSize.ToString(), bounds = window.Bounds.ToString(), workspace = workspace?.Bounds.ToString(), desired = workspace?.DesiredSize.ToString() };
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height), new Vector(96, 96));
        bitmap.Render(window);
        bitmap.Save(Path.Combine(_artifacts, name));
    }
}
