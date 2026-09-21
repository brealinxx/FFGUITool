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
using FFGUITool.ViewModels;
using FFGUITool.Views;

internal static class Program
{
    private static string _artifacts = "";
    private static readonly Dictionary<string, object> Metrics = new();
    private static int _exitCode;

    [STAThread]
    public static int Main(string[] args)
    {
        _artifacts = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine("TestResults", "ui-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));
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
            await Task.Delay(2000);
            var desktop = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
            window = (MainWindow)desktop.MainWindow!;
            var vm = (MainWindowViewModel)window.DataContext!;
            var ffmpeg = new FFmpegManager();
            await ffmpeg.InitializeAsync();
            Require(ffmpeg.IsFFmpegAvailable, "FFmpeg required for UI samples");
            var media = Path.Combine(_artifacts, "media");
            Directory.CreateDirectory(media);
            var seed = Path.Combine(_artifacts, "seed.png");
            var result = await ProcessRunner.RunAsync(ffmpeg.FFmpegPath,
                new[] { "-v", "error", "-f", "lavfi", "-i", "color=c=blue:size=64x64", "-frames:v", "1", "-y", seed });
            Require(result.ExitCode == 0, result.Error);
            var files = Enumerable.Range(0, 1000).Select(i => Path.Combine(media, $"sample-{i:0000}.png")).ToArray();
            await Task.Run(() => { foreach (var file in files) File.Copy(seed, file, true); });
            vm.SelectImageModeCommand.Execute(null);
            await Task.Delay(300);
            var resets = 0;
            vm.VisibleTasks.CollectionChanged += (_, e) => { if (e.Action == NotifyCollectionChangedAction.Reset) resets++; };
            var tick = Stopwatch.StartNew();
            double maxUiGap = 0;
            var heartbeat = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            heartbeat.Tick += (_, _) => { maxUiGap = Math.Max(maxUiGap, tick.Elapsed.TotalMilliseconds); tick.Restart(); };
            heartbeat.Start();
            var elapsed = Stopwatch.StartNew();
            await vm.ProcessSelectedInputs(files.Reverse());
            await Frame(window);
            Metrics["independent_import_1000_ms"] = elapsed.Elapsed.TotalMilliseconds;
            Require(vm.SourceTabs.Count == 1000 && vm.VisibleTasks.Count == 1000, "Independent import count");
            var list = window.GetVisualDescendants().OfType<ListBox>().First(l => ReferenceEquals(l.ItemsSource, vm.VisibleTasks));
            Metrics["realized_queue_items"] = list.GetVisualDescendants().OfType<ListBoxItem>().Count();
            Require((int)Metrics["realized_queue_items"] > 0 && (int)Metrics["realized_queue_items"] < 100, "Queue virtualization");
            var first = vm.SourceTabs[0];
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
            vm.SortTasksByName = true;
            await Frame(window);
            Metrics["sort_1000_ms"] = elapsed.Elapsed.TotalMilliseconds;
            vm.FailedTasksOnly = true;
            Require(vm.VisibleTasks.Count == 0, "Empty failure filter");
            first.IsFailed = true;
            await WaitFor(() => vm.VisibleTasks.Count == 1);
            Require(ReferenceEquals(first, vm.VisibleTasks[0]), "Failure filter reacts to task state");
            vm.FailedTasksOnly = false;
            first.IsFailed = false;
            list.ScrollIntoView(vm.VisibleTasks[999]);
            await Frame(window);
            Metrics["realized_items_after_scroll"] = list.GetVisualDescendants().OfType<ListBoxItem>().Count();
            list.ScrollIntoView(first);

            var wrongType = Path.Combine(_artifacts, "video.mp4");
            File.WriteAllText(wrongType, "not analyzed in image mode");
            await vm.ProcessSelectedInputs(new[] { first.InputPath, wrongType, Path.Combine(_artifacts, "missing.png") });
            Require(vm.SourceTabs.Count == 1000 && vm.ImportSummary.Contains("3"), "Mixed import skips remain visible");

            heartbeat.Stop();
            Metrics["queue_operations_max_ui_heartbeat_gap_ms"] = maxUiGap;
            vm.IsAdvancedMode = true;
            vm.EnableFormatConversion = true;
            foreach (var language in new[] { "zh-CN", "en-US" })
            foreach (var dark in new[] { false, true })
            {
                LocalizationService.SetLanguage(language, false);
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
            Metrics["passed"] = true;
        }
        catch (Exception ex)
        {
            _exitCode = 1;
            Metrics["error"] = ex.ToString();
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

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static async Task WaitFor(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition()) { Require(elapsed.Elapsed < TimeSpan.FromSeconds(10), "UI condition timed out"); await Task.Delay(10); }
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
