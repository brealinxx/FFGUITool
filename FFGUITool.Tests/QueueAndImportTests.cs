using System.Collections.ObjectModel;
using System.Collections.Specialized;
using FFGUITool.Models;
using FFGUITool.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGUITool.Tests;

[TestClass]
public sealed class QueueAndImportTests
{
    private static ProcessingTask Job(int i) => new($"{i:0000}.mp4", new(), ProcessingSettingsScope.Independent);

    [TestMethod]
    public void NoOpRefreshDoesNotNotifyAndAppendingOnlyAddsNewTask()
    {
        var tasks = Enumerable.Range(0, 1000).Select(Job).ToList();
        var visible = new ObservableCollection<ProcessingTask>(tasks);
        var events = new List<NotifyCollectionChangedEventArgs>();
        visible.CollectionChanged += (_, e) => events.Add(e);
        QueueViewUpdater.Update(visible, tasks);
        Assert.AreEqual(0, events.Count);
        tasks.Add(Job(1000));
        QueueViewUpdater.Update(visible, tasks);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Add, events[0].Action);
        Assert.AreSame(tasks[0], visible[0]);
    }

    [TestMethod]
    public void FilteringSortingAndRestoringKeepTaskIdentityAndStateWithoutReset()
    {
        var tasks = Enumerable.Range(0, 1000).Select(Job).ToArray();
        tasks[25].IsSelected = true; tasks[25].IsFailed = true;
        var visible = new ObservableCollection<ProcessingTask>(tasks);
        visible.CollectionChanged += (_, e) => Assert.AreNotEqual(NotifyCollectionChangedAction.Reset, e.Action);
        var filtered = tasks.Where(t => t.IsFailed).ToArray();
        QueueViewUpdater.Update(visible, filtered);
        Assert.AreSame(tasks[25], visible.Single());
        var descending = tasks.Reverse().ToArray();
        QueueViewUpdater.Update(visible, descending);
        CollectionAssert.AreEqual(descending, visible.ToArray());
        Assert.IsTrue(visible.Single(t => t.IsSelected).IsFailed);
        QueueViewUpdater.Update(visible, Array.Empty<ProcessingTask>());
        Assert.AreEqual(0, visible.Count);
    }

    [TestMethod]
    public void MixedImportReportsEverySkipAndDoesNotCountExistingFilesAsImported()
    {
        using var workspace = TestWorkspace.Create();
        var video = workspace.File("video.mp4");
        var image = workspace.File("image.png");
        var audio = workspace.File("audio.wav");
        var text = workspace.File("notes.txt");
        var folder = workspace.Directory("folder");
        var missing = Path.Combine(workspace.Root, "missing.mp4");
        var report = MediaImportReport.SelectFiles(new[] { video, image, audio, text, folder, missing, audio }, false, new[] { video });
        CollectionAssert.AreEqual(new[] { audio }, report.Files);
        Assert.AreEqual(6, report.SkippedCount);
        Assert.AreEqual(2, report.Skipped[ImportSkipReason.Duplicate]);
        Assert.AreEqual(1, report.Skipped[ImportSkipReason.DifferentMode]);
        Assert.AreEqual(1, report.Skipped[ImportSkipReason.Unsupported]);
        Assert.AreEqual(1, report.Skipped[ImportSkipReason.FolderCombination]);
        Assert.AreEqual(1, report.Skipped[ImportSkipReason.Missing]);
    }

    [TestMethod]
    public void FolderReportHonorsModeAudioAndGeneratedOutputExclusions()
    {
        using var workspace = TestWorkspace.Create();
        var video = workspace.File("video.mp4");
        workspace.File("image.png"); workspace.File("audio.wav"); workspace.File("notes.txt");
        workspace.File(".ffguitool-test.mp4");
        var report = new MediaImportReport();
        var files = MediaFileSupport.GetBatchInputFiles(workspace.Root, false, false, report: report).ToArray();
        CollectionAssert.AreEqual(new[] { video }, files);
        Assert.AreEqual(4, report.SkippedCount);
        Assert.AreEqual(2, report.Skipped[ImportSkipReason.DifferentMode]);
        Assert.AreEqual(1, report.Skipped[ImportSkipReason.Generated]);
    }

    [TestMethod]
    public void ExplicitFileSelectionStillAllowsReprocessingAnOutput()
    {
        using var workspace = TestWorkspace.Create();
        var file = workspace.File("sample_FFGUIToolOutPut_1MB.png");
        var report = MediaImportReport.SelectFiles(new[] { file }, true, Array.Empty<string>());
        CollectionAssert.AreEqual(new[] { file }, report.Files);
        Assert.AreEqual(0, report.SkippedCount);
    }

    [TestMethod]
    public void CancelledImportDoesNotContinueEnumeratingInputs()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() =>
            MediaImportReport.SelectFiles(new[] { "sample.png" }, true, Array.Empty<string>(), cancellation.Token));
    }
}
