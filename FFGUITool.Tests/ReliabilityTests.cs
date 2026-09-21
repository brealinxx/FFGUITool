using FFGUITool.Models;
using FFGUITool.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGUITool.Tests;

[TestClass]
public sealed class ReliabilityTests
{
    [TestMethod]
    public void TargetBudgetUsesActualAudioAndDecimalBitrate()
    {
        Assert.AreEqual(39, EncodingPolicy.TargetBitrate(10, 600, 96));
        Assert.AreEqual(135, EncodingPolicy.TargetBitrate(10, 600, 0));
        Assert.ThrowsException<ArgumentException>(() => EncodingPolicy.TargetBitrate(1, 600, 96));
    }

    [TestMethod]
    public void TrimBudgetUsesOutputDurationAndRejectsReversedRange()
    {
        var settings = new CompressionSettings { EnableTrim = true, TrimStart = "00:00:10", TrimEnd = "20" };
        Assert.AreEqual(10, EncodingPolicy.OutputDuration(settings, new VideoInfo { Duration = 100 }));
        settings.TrimEnd = "5";
        Assert.ThrowsException<ArgumentException>(() => EncodingPolicy.OutputDuration(settings, new VideoInfo { Duration = 100 }));
    }

    [TestMethod]
    public void ArgumentRoundTripPreservesPathsWithoutAShell()
    {
        foreach (var path in new[] { @"C:\Media Files\测试.mp4", "/tmp/a\"b.mp4", "/tmp/$(echo secret);video.mp4" })
            Assert.AreEqual(path, CommandArguments.Parse(CommandArguments.Quote(path)).Single());
    }

    [TestMethod]
    public void ArgumentRoundTripPreservesTrailingSlashesAndEmbeddedQuotes()
    {
        foreach (var value in new[] { "", @"C:\folder\", "a\\\"b", "a\\\\\"b", "\\", "a b\\" })
            Assert.AreEqual(value, CommandArguments.Parse(CommandArguments.Quote(value)).Single());
    }

    [TestMethod]
    public void FailedOutputTransactionPreservesExistingDestination()
    {
        using var workspace = TestWorkspace.Create();
        var destination = workspace.File("existing.mp4");
        string temporary;
        using (var transaction = new OutputTransaction(destination, true))
        {
            temporary = transaction.TemporaryPath;
            File.WriteAllText(temporary, "partial");
        }
        Assert.AreEqual("test", File.ReadAllText(destination));
        Assert.IsFalse(File.Exists(temporary));
    }

    [TestMethod]
    public void PublicationRefusesRaceWithoutOverwriteAuthorization()
    {
        using var workspace = TestWorkspace.Create();
        var destination = Path.Combine(workspace.Root, "result.mp4");
        using var transaction = new OutputTransaction(destination, false);
        File.WriteAllText(transaction.TemporaryPath, "new");
        File.WriteAllText(destination, "other application");
        Assert.ThrowsException<IOException>(transaction.Commit);
        Assert.AreEqual("other application", File.ReadAllText(destination));
    }

    [TestMethod]
    public void WebmCannotBeOverriddenByAnH264HardwareEncoder()
    {
        using var workspace = TestWorkspace.Create();
        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        { InputPath = workspace.File("source.mp4"), EnableFormatConversion = true, OutputFormat = "webm", HardwareEncoder = "h264_nvenc", UseCrf = true });
        StringAssert.Contains(command.BuildCommand(), "-c:v libvpx-vp9");
        Assert.AreEqual("", command.HardwareEncoder);
        Assert.IsFalse(EncodingPolicy.QualityArguments("h264_nvenc", 23).Contains("-crf"));
        StringAssert.Contains(EncodingPolicy.QualityArguments("h264_qsv", 23), "global_quality");
    }

    [TestMethod]
    public void ImageEncoderAndExtensionUseTheSameResolvedFormat()
    {
        using var workspace = TestWorkspace.Create();
        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        { InputPath = workspace.File("source.png"), IsImageProcessing = true, EnableFormatConversion = false, ImageOutputFormat = "webp" });
        Assert.AreEqual("png", command.ImageFormat);
        StringAssert.EndsWith(command.OutputPath, ".png");
        StringAssert.Contains(command.BuildCommand(), "compression_level");
    }

    [TestMethod]
    public void OutputNamesPreserveSubfoldersAndStayInOutputDirectory()
    {
        using var workspace = TestWorkspace.Create();
        var inputDirectory = workspace.Directory("input");
        var subDirectory = Directory.CreateDirectory(Path.Combine(inputDirectory, "nested")).FullName;
        var input = Path.Combine(subDirectory, "clip.mp4"); File.WriteAllText(input, "test");
        var output = workspace.Directory("output");
        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        { InputPath = input, InputRoot = inputDirectory, OutputPath = output, OutputNamePattern = "{name}_small", PreserveFolderStructure = true });
        Assert.AreEqual(Path.Combine(output, "nested", "clip_small.mp4"), command.OutputPath);
    }

    [TestMethod]
    public void WorkspaceRoundTripPreservesIndependentAndSharedSettings()
    {
        using var workspace = TestWorkspace.Create();
        var store = new WorkspaceStore(Path.Combine(workspace.Root, "workspace.json"));
        var task = new ProcessingTask(workspace.File("source.png"), new CompressionSettings { IsImageProcessing = true, ImageQuality = 42 }, ProcessingSettingsScope.Independent)
        { HasSettings = true, Bitrate = 42, State = ProcessingTaskState.Failed, IsFailed = true };
        store.Save(new WorkspaceDocument { Tasks = new() { task }, Presets = new() { new SavedPreset { Name = "Test", Settings = task.Settings.Clone() } } });
        var loaded = store.Load();
        Assert.AreEqual(task.InputPath, loaded.Tasks.Single().InputPath);
        Assert.AreEqual(42, loaded.Tasks.Single().Settings.ImageQuality);
        Assert.AreEqual(ProcessingTaskState.Failed, loaded.Tasks.Single().State);
        Assert.AreEqual("Test", loaded.Presets.Single().Name);
    }

    [TestMethod]
    public void ProbeReadsStreamsRotationAndAudioBudget()
    {
        var info = MediaProbe.Parse("""
            {"format":{"duration":"60.5","bit_rate":"1000000"},"streams":[
            {"codec_type":"video","codec_name":"h264","width":1920,"height":1080,"avg_frame_rate":"30000/1001","pix_fmt":"yuv420p","side_data_list":[{"rotation":90}]},
            {"codec_type":"audio","codec_name":"aac","bit_rate":"128000"}]}
            """, "video.mp4", 12345);
        Assert.AreEqual(60.5, info.Duration);
        Assert.AreEqual(128, info.AudioBitrate);
        Assert.AreEqual(90, info.Rotation);
        Assert.AreEqual("1920x1080", info.Resolution);
        Assert.IsTrue(info.HasAudio);
        Assert.AreEqual(128, EncodingPolicy.AudioBudget(new CompressionSettings { AudioTrackMode = "copy" }, info));
    }

    [TestMethod]
    public void StreamCopyRejectsFilters()
    {
        using var workspace = TestWorkspace.Create();
        Assert.ThrowsException<ArgumentException>(() => new CommandBuilder().BuildCommand(new CompressionSettings
        { InputPath = workspace.File("source.mp4"), StreamCopy = true, EnableResolutionConversion = true }));
        var command = new CommandBuilder().BuildCommand(new CompressionSettings { InputPath = workspace.File("copy.mp4"), StreamCopy = true }).BuildCommand();
        StringAssert.Contains(command, "-c:v copy");
        Assert.IsFalse(command.Contains("-b:v"));
    }

    [TestMethod]
    public void GeneratedFilesAreExcludedFromRecursiveScan()
    {
        using var workspace = TestWorkspace.Create();
        workspace.File("photo.png"); workspace.File("photo_FFGUIToolOutPut_10KB.png"); workspace.File(".ffguitool-test.png");
        Assert.AreEqual(1, MediaFileSupport.GetBatchInputFiles(workspace.Root, true, false, true).Count());
    }

    [TestMethod]
    public void CancelledFolderDiscoveryStopsBeforeEnumeration()
    {
        using var workspace = TestWorkspace.Create();
        workspace.File("image.png");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => MediaFileSupport.GetBatchInputFiles(workspace.Root, true, false, true, cancellation.Token).ToList());
    }

    [TestMethod]
    public void ThousandTasksRemainUniqueAndKeepIndependentSettings()
    {
        var workspace = new ProcessingWorkspace();
        var paths = Enumerable.Range(0, 1000).Select(index => Path.Combine(Path.GetTempPath(), $"input-{index}.mp4")).ToArray();
        workspace.AddIndependentTasks(paths.Concat(paths), path => new ProcessingTask(path, new CompressionSettings(), ProcessingSettingsScope.Independent));
        Assert.AreEqual(1000, workspace.IndependentTasks.Count);
        workspace.IndependentTasks[0].Settings.Crf = 42;
        Assert.AreEqual(23, workspace.IndependentTasks[999].Settings.Crf);
    }

    [TestMethod]
    public void CopyingAacIntoWebmIsRejectedBeforeEncoding()
    {
        using var workspace = TestWorkspace.Create();
        Assert.ThrowsException<ArgumentException>(() => new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = workspace.File("source.mp4"), EnableFormatConversion = true, OutputFormat = "webm", AudioTrackMode = "copy"
        }, new VideoInfo { HasAudio = true, AudioCodec = "aac" }));
    }

    [TestMethod]
    public void PngUsesCompressionLevelRatherThanLossyQuality()
    {
        using var workspace = TestWorkspace.Create();
        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = workspace.File("source.png"), IsImageProcessing = true, ImageQuality = 80, PngCompressionLevel = 9
        });
        StringAssert.Contains(command.BuildCommand(), "-compression_level 9");
    }
}

[TestClass]
public sealed class MediaIntegrationTests
{
    private TestWorkspace _workspace = null!;
    private FFmpegManager _ffmpeg = null!;
    private VideoAnalyzer _analyzer = null!;
    private ProcessingExecutor _executor = null!;
    private string _input = "";

    [TestInitialize]
    public async Task Setup()
    {
        _workspace = TestWorkspace.Create();
        _ffmpeg = new FFmpegManager(_workspace.Directory("config"));
        await _ffmpeg.InitializeAsync();
        if (!_ffmpeg.IsFFmpegAvailable)
        {
            if (Environment.GetEnvironmentVariable("FFGUITOOL_REQUIRE_FFMPEG") == "1") Assert.Fail("FFmpeg must be installed for CI integration tests.");
            Assert.Inconclusive("FFmpeg is required for media integration tests.");
        }
        _analyzer = new VideoAnalyzer(_ffmpeg);
        _executor = new ProcessingExecutor(_ffmpeg, new ExifToolManager(_workspace.Directory("exif")), _analyzer, new CommandBuilder());
        _input = Path.Combine(_workspace.Root, "测试 source.mp4");
        var result = await ProcessRunner.RunAsync(_ffmpeg.FFmpegPath, new[] { "-v", "error", "-y", "-f", "lavfi", "-i", "testsrc2=size=128x96:rate=24", "-t", "2", "-c:v", "libx264", _input }, timeout: TimeSpan.FromSeconds(15));
        Assert.AreEqual(0, result.ExitCode, result.Error);
    }

    [TestCleanup] public void Cleanup() => _workspace?.Dispose();
    private ProcessingTask Job(string? path = null) => new(path ?? _input,
        new CompressionSettings { InputPath = path ?? _input, OutputPath = _workspace.Directory("out"), TargetSizeMB = 0.08 }, ProcessingSettingsScope.Independent);
    private static ProcessingExecutionOptions Options => new();

    [TestMethod]
    public async Task RealEncodeProducesReadableOutputAndIntermediateProgress()
    {
        var observed = new List<ProcessingProgress>();
        var summary = await _executor.ExecuteAsync(new[] { Job() }, Options, new InlineProgress<ProcessingProgress>(observed.Add), default);
        Assert.AreEqual(0, summary.Failures.Count, string.Join("\n", summary.Failures.Select(f => f.Exception.ToString())));
        Assert.AreEqual(1, summary.Results.Count);
        Assert.IsTrue(summary.Results[0].OutputInfo!.FileSize > 0);
        Assert.IsTrue(observed.Any(p => p.State == ProcessingTaskState.Running && p.Fraction > 0 && p.Fraction < 1));
        Assert.AreEqual("h264", (await _analyzer.AnalyzeVideo(summary.Results[0].OutputPath))!.VideoCodec);
    }

    [TestMethod]
    public async Task EncodingFailureDoesNotDeleteOldOutputAndQueueContinues()
    {
        var bad = Job(); bad.Settings.Codec = "does_not_exist";
        var oldOutput = new CommandBuilder().BuildCommand(bad.Settings).OutputPath;
        File.WriteAllText(oldOutput, "previous successful result");
        var good = Job(); good.Settings.OutputNamePattern = "{name}_good";
        var summary = await _executor.ExecuteAsync(new[] { bad, good }, new ProcessingExecutionOptions { OutputConflictPolicy = OutputConflictPolicy.Overwrite }, null, default);
        Assert.AreEqual(1, summary.Failures.Count);
        Assert.AreEqual(1, summary.Results.Count);
        Assert.AreEqual("previous successful result", File.ReadAllText(oldOutput));
        Assert.IsFalse(Directory.EnumerateFiles(_workspace.Root, ".ffguitool-*", SearchOption.AllDirectories).Any());
    }

    [TestMethod]
    public async Task CancellationPreservesExistingOutputAndCleansTemporaryFiles()
    {
        var job = Job();
        var destination = new CommandBuilder().BuildCommand(job.Settings).OutputPath;
        File.WriteAllText(destination, "old");
        using var cancellation = new CancellationTokenSource();
        var progress = new InlineProgress<ProcessingProgress>(p => { if (p.State == ProcessingTaskState.Running && p.Fraction > 0) cancellation.Cancel(); });
        var summary = await _executor.ExecuteAsync(new[] { job }, new ProcessingExecutionOptions { OutputConflictPolicy = OutputConflictPolicy.Overwrite }, progress, cancellation.Token);
        Assert.IsTrue(summary.WasCancelled);
        Assert.AreEqual("old", File.ReadAllText(destination));
        Assert.IsFalse(Directory.EnumerateFiles(_workspace.Root, ".ffguitool-*", SearchOption.AllDirectories).Any());
    }

    [TestMethod]
    public async Task TwoPassEncodingCompletesAndRemovesPassLogs()
    {
        var job = Job(); job.Settings.TwoPass = true;
        var summary = await _executor.ExecuteAsync(new[] { job }, Options, null, default);
        Assert.AreEqual(0, summary.Failures.Count, string.Join("\n", summary.Failures.Select(f => f.Exception.ToString())));
        Assert.IsTrue(summary.Results[0].OutputInfo!.FileSize <= 0.08 * 1024 * 1024 || !string.IsNullOrEmpty(summary.Results[0].Warning));
        Assert.IsFalse(Directory.EnumerateFiles(_workspace.Root, "*.pass*", SearchOption.AllDirectories).Any());
    }

    [TestMethod]
    public async Task UnattainableImageTargetReturnsWarningWithoutSilentResize()
    {
        var image = Path.Combine(_workspace.Root, "photo.png");
        var create = await ProcessRunner.RunAsync(_ffmpeg.FFmpegPath, new[] { "-v", "error", "-y", "-i", _input, "-frames:v", "1", image });
        Assert.AreEqual(0, create.ExitCode, create.Error);
        var job = Job(image); job.Settings.IsImageProcessing = true; job.Settings.EnableFormatConversion = true;
        job.Settings.ImageOutputFormat = "jpg"; job.Settings.ImageTargetSizeKB = 0.1; job.Settings.AllowImageResize = false;
        var summary = await _executor.ExecuteAsync(new[] { job }, Options, null, default);
        Assert.AreEqual(0, summary.Failures.Count, string.Join("\n", summary.Failures.Select(f => f.Exception.ToString())));
        Assert.IsFalse(string.IsNullOrWhiteSpace(summary.Results[0].Warning));
        Assert.AreEqual("128x96", summary.Results[0].OutputInfo!.Resolution);
    }

    [TestMethod]
    public async Task SourceCollisionIsRejectedEvenWhenOverwriteIsSelected()
    {
        var job = Job(); job.Settings.OutputPath = _workspace.Root; job.Settings.OutputNamePattern = "{name}";
        var original = File.ReadAllBytes(_input);
        var summary = await _executor.ExecuteAsync(new[] { job }, new ProcessingExecutionOptions { OutputConflictPolicy = OutputConflictPolicy.Overwrite }, null, default);
        Assert.AreEqual(1, summary.Failures.Count);
        CollectionAssert.AreEqual(original, File.ReadAllBytes(_input));
    }

    [TestMethod]
    public async Task StreamCopyPreservesVideoCodec()
    {
        var job = Job(); job.Settings.StreamCopy = true; job.Settings.EnableFormatConversion = true; job.Settings.OutputFormat = "mkv";
        var summary = await _executor.ExecuteAsync(new[] { job }, Options, null, default);
        Assert.AreEqual(0, summary.Failures.Count);
        Assert.AreEqual("h264", summary.Results[0].OutputInfo!.VideoCodec);
    }

    [TestMethod]
    public async Task PreviewCreatesBeforeAndAfterImagesAndCleansItsDirectory()
    {
        var service = new PreviewService(_ffmpeg, _executor, _analyzer);
        var preview = await service.CreateAsync(Job().Settings, new HashSet<string>(), default);
        var path = preview.DirectoryPath;
        Assert.IsTrue(File.Exists(preview.BeforeImage)); Assert.IsTrue(File.Exists(preview.AfterImage));
        preview.Dispose(); Assert.IsFalse(Directory.Exists(path));
    }

    [TestMethod]
    public async Task ProcessCancellationTerminatesAnUnboundedEncoder()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        try
        {
            await ProcessRunner.RunAsync(_ffmpeg.FFmpegPath, new[] { "-v", "error", "-f", "lavfi", "-i", "color=size=32x32", "-f", "null", "-" }, cancellation.Token);
            Assert.Fail("Cancellation did not interrupt the encoder.");
        }
        catch (OperationCanceledException) { Assert.IsTrue(cancellation.IsCancellationRequested); }
    }

    [TestMethod]
    public async Task DuplicateOutputsWithinAQueueNeverOverwriteEachOther()
    {
        var first = Job();
        var second = Job();
        var summary = await _executor.ExecuteAsync(new[] { first, second }, new ProcessingExecutionOptions { OutputConflictPolicy = OutputConflictPolicy.Overwrite }, null, default);
        Assert.AreEqual(0, summary.Failures.Count);
        Assert.AreEqual(2, summary.Results.Select(result => result.OutputPath).Distinct().Count());
        Assert.IsTrue(summary.Results.All(result => File.Exists(result.OutputPath)));
    }

    [TestMethod]
    public async Task RuntimeSettingsAreFrozenAtQueueStart()
    {
        var job = Job();
        var progress = new InlineProgress<ProcessingProgress>(value =>
        {
            if (value.State == ProcessingTaskState.Running) job.Settings.Codec = "invalid-after-start";
        });
        var summary = await _executor.ExecuteAsync(new[] { job }, Options, progress, default);
        Assert.AreEqual(0, summary.Failures.Count);
        Assert.AreEqual("h264", summary.Results.Single().OutputInfo!.VideoCodec);
    }

    [TestMethod]
    public async Task CachedAnalysisInvalidatesWhenAFileChanges()
    {
        var first = await _analyzer.AnalyzeVideo(_input);
        Assert.IsNotNull(first);
        File.WriteAllText(_input, "not a media file anymore");
        Assert.IsNull(await _analyzer.AnalyzeVideo(_input));
    }
}

internal sealed class InlineProgress<T>(Action<T> action) : IProgress<T>
{ public void Report(T value) => action(value); }
