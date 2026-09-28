using System.Text.Json;
using FFGUITool.Models;
using FFGUITool.Services;
using FFGUITool.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGUITool.Tests;

[TestClass]
public sealed class WorkspaceAndProgressTests
{
    [TestMethod]
    public void LargeReorderUsesOneRangeNotificationAndKeepsTaskIdentityAndState()
    {
        var tasks = Enumerable.Range(0, 10000).Select(i => new ProcessingTask($"{i}.png", new(), ProcessingSettingsScope.Independent)).ToArray();
        tasks[500].IsSelected = true; tasks[500].IsFailed = true;
        var visible = new QueueTaskCollection(tasks);
        var events = new List<System.Collections.Specialized.NotifyCollectionChangedEventArgs>();
        visible.CollectionChanged += (_, e) => events.Add(e);
        var reversed = tasks.Reverse().ToArray();
        QueueViewUpdater.Update(visible, reversed);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(System.Collections.Specialized.NotifyCollectionChangedAction.Replace, events[0].Action);
        CollectionAssert.AreEqual(reversed, visible.ToArray());
        Assert.IsTrue(visible.Single(t => t.IsSelected).IsFailed);
        events.Clear();
        QueueViewUpdater.Update(visible, reversed);
        Assert.AreEqual(0, events.Count);
        QueueViewUpdater.Update(visible, new[] { tasks[500] });
        Assert.IsTrue(events.Count <= 2);
        Assert.AreSame(tasks[500], visible.Single());
        events.Clear();
        QueueViewUpdater.Update(visible, tasks);
        Assert.IsTrue(events.Count <= 2, "Restoring a filter must not notify once per insertion");
        CollectionAssert.AreEqual(tasks, visible.ToArray());
        Assert.IsTrue(tasks[500].IsSelected && tasks[500].IsFailed);
    }

    [TestMethod]
    public void EditorCopyAndLegacyWorkspaceRoundTripPreserveIndependentDisplayState()
    {
        var source = new ProcessingTask("a.mp4", new(), ProcessingSettingsScope.Independent);
        var editor = new ParameterEditorViewModel
        {
            CompressionSettings = new() { InputPath = "a.mp4", Bitrate = 765, UseCrf = true, Crf = 21, TwoPass = true },
            Bitrate = 1200, UseCrf = true, Crf = 21, TargetSizeMB = 17.5,
            SelectedImageTargetSizeUnit = "MB", SelectedVideoFormatOption = new("WebM", "webm", "")
        };
        editor.SaveTo(source);
        var target = new ProcessingTask("b.mp4", new(), ProcessingSettingsScope.Independent);
        ParameterEditorViewModel.CopyTo(source, target);
        Assert.AreEqual("b.mp4", target.Settings.InputPath);
        Assert.AreEqual(765, target.Settings.Bitrate);
        Assert.AreEqual(1200, target.Bitrate); // display choice and calculated encode bitrate have different meanings
        var json = JsonSerializer.Serialize(new WorkspaceDocument { Tasks = new() { target } });
        Assert.IsFalse(json.Contains("EditorState")); // version 1 field layout remains compatible
        var loaded = JsonSerializer.Deserialize<WorkspaceDocument>(json)!.Tasks.Single();
        Assert.AreEqual("MB", loaded.SelectedImageTargetSizeUnit);
        Assert.AreEqual("webm", loaded.SelectedVideoFormatValue);
        Assert.AreEqual(17.5, loaded.TargetSizeMB);
        source.EditorState.Bitrate = 2000; source.Settings.Crf = 50;
        Assert.AreEqual(1200, target.Bitrate);
        Assert.AreEqual(21, target.Settings.Crf);
    }

    [TestMethod]
    public void ExcludedMissingIndependentInputsAreNotExecutedOrRetried()
    {
        var workspace = new ProcessingWorkspace();
        var missing = new ProcessingTask("missing.mp4", new(), ProcessingSettingsScope.Independent)
        { IsIncluded = false, IsSelected = true, IsFailed = true };
        workspace.IndependentTasks.Add(missing);
        Assert.AreEqual(0, workspace.GetExecutionTasks(false, ProcessingTaskSelection.All, false).Count);
        Assert.AreEqual(0, workspace.GetExecutionTasks(false, ProcessingTaskSelection.Current, false).Count);
        Assert.AreEqual(0, workspace.GetExecutionTasks(false, ProcessingTaskSelection.All, true).Count);
    }

    [TestMethod]
    public void BatchSummaryInvalidatesOnlyRelevantFactsAndPreservesPerFileRounding()
    {
        var tasks = new System.Collections.ObjectModel.ObservableCollection<ProcessingTask>();
        using var summary = new BatchQueueSummary(tasks);
        var a = new ProcessingTask("a.png", new(), ProcessingSettingsScope.Shared) { SourceBytes = 3 };
        var b = new ProcessingTask("b.png", new(), ProcessingSettingsScope.Shared) { SourceBytes = 3 };
        tasks.Add(a); tasks.Add(b);
        Assert.AreEqual(2, summary.IncludedCount);
        Assert.AreEqual(4L, summary.EstimateBytes(.5)); // round each file, not the total
        a.SourceBytes = 10;
        Assert.AreEqual(7L, summary.EstimateBytes(.5));
        a.IsIncluded = false;
        a.IsFailed = true;
        Assert.AreEqual(1, summary.IncludedCount);
        Assert.AreSame(b, summary.FirstIncluded);
        Assert.IsFalse(summary.HasFailures);
        b.IsFailed = true;
        Assert.IsTrue(summary.HasFailures);
        tasks.Clear();
        Assert.AreEqual(0, summary.IncludedCount);
        Assert.AreEqual(0L, summary.EstimateBytes(.5));
        Assert.IsNull(summary.FirstIncluded);
    }

    [TestMethod]
    public void CacheRetainsHotEntriesEvictsOneColdEntryAndInvalidatesMetadata()
    {
        var cache = new MediaInfoCache();
        var info = new VideoInfo();
        for (var i = 0; i < 512; i++) cache.Put($"{i}.png", 10, 100, info);
        Assert.AreSame(info, cache.Get("0.png", 10, 100));
        cache.Put("512.png", 10, 100, info);
        Assert.AreSame(info, cache.Get("0.png", 10, 100));
        Assert.IsNull(cache.Get("1.png", 10, 100));
        Assert.AreSame(info, cache.Get("2.png", 10, 100));
        Assert.IsNull(cache.Get("2.png", 11, 100));
        Assert.IsNull(cache.Get("3.png", 10, 101));
        Parallel.For(0, 2000, i => cache.Put($"concurrent-{i}.png", 10, 100, info));
        Assert.AreEqual(512, Enumerable.Range(0, 2000).Count(i => cache.Get($"concurrent-{i}.png", 10, 100) != null));
    }

    [TestMethod]
    public void WorkspaceSnapshotDetachesTasksSettingsPresetsHistoryAndCollections()
    {
        var settings = new CompressionSettings { Crf = 19, OutputNamePattern = "{name}_snapshot" };
        var task = new ProcessingTask("测试.mp4", settings, ProcessingSettingsScope.Shared);
        foreach (var property in typeof(ProcessingTask).GetProperties().Where(p => p.CanWrite))
        {
            if (property.PropertyType == typeof(string)) property.SetValue(task, "custom-" + property.Name);
            if (property.PropertyType == typeof(bool)) property.SetValue(task, true);
            if (property.PropertyType == typeof(int)) property.SetValue(task, 27);
            if (property.PropertyType == typeof(double)) property.SetValue(task, 42.5);
            if (property.PropertyType == typeof(long)) property.SetValue(task, 9876L);
        }
        task.State = ProcessingTaskState.Warning;
        var source = new WorkspaceDocument
        {
            Tasks = new() { task }, RecentImageSettings = settings, RecentVideoSettings = settings,
            Presets = new() { new() { Name = "preset", Settings = settings } },
            History = new() { new() { OutputPath = "old", Settings = settings } }, GeneratedPaths = new() { "old" }
        };
        var snapshot = WorkspacePersistence.Capture(source);
        var expected = JsonSerializer.Serialize(snapshot);
        Assert.AreEqual(JsonSerializer.Serialize(task), JsonSerializer.Serialize(snapshot.Tasks[0]));
        settings.Crf = 40;
        task.Status = "changed";
        source.Tasks.Clear(); source.Presets[0].Name = "changed";
        source.History[0].OutputPath = "changed"; source.GeneratedPaths.Clear();
        Assert.AreEqual(expected, JsonSerializer.Serialize(snapshot));
        var notifications = 0;
        task.PropertyChanged += (_, _) => notifications++;
        snapshot.Tasks[0].IsFailed = false;
        Assert.AreEqual(0, notifications);
    }

    [TestMethod]
    public async Task WriterCoalescesPendingRequestsAndFlushWaitsForLatestCommit()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var workspace = TestWorkspace.Create();
        var store = new WorkspaceStore(Path.Combine(workspace.Root, "workspace.json"));
        var versions = new List<int>();
        var writer = new WorkspacePersistence(document =>
        {
            if (document.Version == 1)
            {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            }
            versions.Add(document.Version);
            store.Save(document);
        });
        writer.Enqueue(new() { Version = 1 });
        try
        {
            Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(10)));
            writer.Enqueue(new() { Version = 2 });
            writer.Enqueue(new() { Version = 3 });
            Assert.IsFalse(writer.FlushAsync().IsCompleted);
        }
        finally { release.Set(); }
        Assert.IsNull(await writer.FlushAsync().WaitAsync(TimeSpan.FromSeconds(10)));
        CollectionAssert.AreEqual(new[] { 1, 3 }, versions);
        Assert.AreEqual(3, store.Load().Version);
        for (var i = 4; i < 100; i++)
        {
            writer.Enqueue(new() { Version = i });
            Assert.IsNull(await writer.FlushAsync().WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.AreEqual(i, store.Load().Version, "An enqueue at the idle boundary must not be lost");
        }
    }

    [TestMethod]
    public async Task FailedSavePreservesOldDocumentAndNextSaveCanRecover()
    {
        using var workspace = TestWorkspace.Create();
        var path = Path.Combine(workspace.Root, "workspace.json");
        var store = new WorkspaceStore(path);
        store.Save(new() { Version = 7 });
        var fail = true;
        var writer = new WorkspacePersistence(document =>
        {
            if (fail) throw new IOException("simulated unavailable disk");
            store.Save(document);
        });
        writer.Enqueue(new() { Version = 8 });
        Assert.IsInstanceOfType<IOException>(await writer.FlushAsync());
        Assert.AreEqual(7, store.Load().Version);
        fail = false;
        writer.Enqueue(new() { Version = 9 });
        Assert.IsNull(await writer.FlushAsync());
        Assert.AreEqual(9, store.Load().Version);
    }

    [TestMethod]
    public void RunningProgressIsCoalescedAndCannotOverwriteTerminalState()
    {
        var buffer = new ProcessingProgressBuffer();
        var task = new ProcessingTask("file.png", new(), ProcessingSettingsScope.Independent);
        for (var i = 0; i < 100; i++)
            buffer.ReportRunning(new(task, ProcessingTaskState.Running, 0, 1, Fraction: i / 100.0));
        Assert.AreEqual(.99, buffer.Drain().Single().Fraction);
        buffer.ReportRunning(new(task, ProcessingTaskState.Running, 0, 1));
        buffer.Complete(task);
        buffer.ReportRunning(new(task, ProcessingTaskState.Running, 0, 1));
        Assert.AreEqual(0, buffer.Drain().Length);
    }

    [TestMethod]
    public void IncrementalTotalsMatchReferenceAcrossConcurrentTasksAndRetries()
    {
        var totals = new ProcessingProgressTotals();
        var tasks = Enumerable.Range(0, 1000).Select(i => new ProcessingTask($"{i}.png", new(), ProcessingSettingsScope.Independent)).ToArray();
        var reference = new Dictionary<ProcessingTask, (ProcessingTaskState State, double Fraction)>();
        var random = new Random(123);
        for (var i = 0; i < 5000; i++)
        {
            var task = tasks[random.Next(tasks.Length)];
            var state = (ProcessingTaskState)random.Next(6);
            var fraction = random.NextDouble();
            reference[task] = (state, fraction);
            var expected = reference.Values.Sum(p => p.State == ProcessingTaskState.Running ? p.Fraction :
                p.State is ProcessingTaskState.Completed or ProcessingTaskState.Warning or ProcessingTaskState.Failed ? 1 : 0) / 10;
            Assert.AreEqual(expected, totals.Apply(new(task, state, 0, 1000, Fraction: fraction)), 1e-8);
        }
        totals.Clear();
        Assert.AreEqual(0, totals.Apply(new(tasks[0], ProcessingTaskState.Running, 0, 1000)));
    }
}
