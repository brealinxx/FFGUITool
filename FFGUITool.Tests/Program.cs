using System.Xml.Linq;
using FFGUITool.Models;
using FFGUITool.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGUITool.Tests;

[TestClass]
public sealed class CommandBuilderTests
{
    [TestMethod]
    public void BuildsBasicVideoCommand()
    {
        using var workspace = TestWorkspace.Create();
        var input = workspace.File("clip.mp4");
        var output = workspace.Directory("out");

        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = input,
            OutputPath = output,
            Bitrate = 1200,
            AudioBitrate = 96,
            Codec = "libx265",
            EnableResolutionConversion = true,
            ResolutionHeight = 720,
            MaxFramerate = 30,
            TargetSizeMB = 25
        });

        var text = command.BuildCommand();
        StringAssert.Contains(text, $"-i \"{input}\"");
        StringAssert.Contains(text, "-vf \"scale=-2:min(720\\,ih),fps=30\"");
        StringAssert.Contains(text, "-c:v libx265");
        StringAssert.Contains(text, "-b:v 1200k");
        StringAssert.Contains(text, "-c:a aac");
        StringAssert.Contains(text, "-b:a 96k");
        Assert.AreEqual(Path.Combine(output, "clip_FFGUIToolOutPut_25MB.mp4"), command.OutputPath);
    }

    [TestMethod]
    public void BuildsAudioExtractionCommand()
    {
        using var workspace = TestWorkspace.Create();
        var input = workspace.File("song.wav");

        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = input,
            EnableAudioConversion = true,
            AudioOutputFormat = "flac",
            AudioBitrate = 320,
            TargetSizeMB = 10
        });

        var text = command.BuildCommand();
        StringAssert.Contains(text, "-vn");
        StringAssert.Contains(text, "-c:a flac");
        StringAssert.Contains(text, "-b:a 320k");
        StringAssert.EndsWith(command.OutputPath, "song_FFGUIToolOutPut_10MB.flac");
    }

    [TestMethod]
    public void BuildsTrimmedVideoCommand()
    {
        using var workspace = TestWorkspace.Create();
        var input = workspace.File("clip.mp4");

        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = input,
            Bitrate = 1200,
            EnableTrim = true,
            TrimStart = "00:00:05",
            TrimEnd = "00:00:12.5",
            OutputLabel = "clip_00_00_05-00_00_12_5_25MB"
        });

        var text = command.BuildCommand();
        StringAssert.StartsWith(text, "ffmpeg -ss 00:00:05 -to 00:00:12.5 ");
        StringAssert.Contains(text, $"-i \"{input}\"");
        StringAssert.EndsWith(command.OutputPath, "clip_FFGUIToolOutPut_clip_00_00_05-00_00_12_5_25MB.mp4");
    }

    [TestMethod]
    public void BuildsVideoAudioTrackModes()
    {
        using var workspace = TestWorkspace.Create();
        var input = workspace.File("clip.mp4");

        var muted = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = input,
            AudioTrackMode = "remove"
        }).BuildCommand();

        StringAssert.Contains(muted, "-an");

        var copied = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = input,
            AudioTrackMode = "copy"
        }).BuildCommand();

        StringAssert.Contains(copied, "-c:a copy");
        Assert.IsFalse(copied.Contains("-b:a"));
    }

    [TestMethod]
    public void BuildsImageCommand()
    {
        using var workspace = TestWorkspace.Create();
        var input = workspace.File("photo.png");

        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = input,
            IsImageProcessing = true,
            EnableFormatConversion = true,
            ImageOutputFormat = "webp",
            ImageQuality = 75,
            ImageTargetSizeKB = 512,
            EnableResolutionConversion = true,
            ResolutionHeight = 512
        });

        var text = command.BuildCommand();
        StringAssert.Contains(text, "-vf \"scale=-2:min(512\\,ih)\"");
        StringAssert.Contains(text, "-frames:v 1 -c:v libwebp");
        StringAssert.Contains(text, "-quality 75");
        StringAssert.EndsWith(command.OutputPath, "photo_FFGUIToolOutPut_512KB.webp");
    }

    [TestMethod]
    public void BuildsOutputPaths()
    {
        using var workspace = TestWorkspace.Create();
        var input = workspace.File("my clip.mov");

        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = input,
            OutputLabel = "crf 23",
            EnableFormatConversion = true,
            OutputFormat = "mkv"
        });

        Assert.AreEqual(
            Path.Combine(workspace.Root, "my clip_FFGUIToolOutPut_crf_23.mkv"),
            command.OutputPath);
    }

    [TestMethod]
    public void MapsImageInputFormats()
    {
        using var workspace = TestWorkspace.Create();

        AssertImageOutputExtension(workspace.File("photo.jpeg"), "jpg");
        AssertImageOutputExtension(workspace.File("diagram.bmp"), "png");
        AssertImageOutputExtension(workspace.File("icon.ico"), "jpg");
        AssertImageOutputExtension(workspace.File("picture.avif"), "jpg");
        AssertImageOutputExtension(workspace.File("source.webp"), "webp");
    }

    private static void AssertImageOutputExtension(string inputPath, string expectedExtension)
    {
        var command = new CommandBuilder().BuildCommand(new CompressionSettings
        {
            InputPath = inputPath,
            IsImageProcessing = true,
            EnableFormatConversion = false,
            ImageTargetSizeKB = 128
        });

        StringAssert.EndsWith(command.OutputPath, $".{expectedExtension}");
    }
}

[TestClass]
public sealed class MediaFileSupportTests
{
    [TestMethod]
    public void FiltersBatchFiles()
    {
        using var workspace = TestWorkspace.Create();
        workspace.File("a.mp4");
        workspace.File("b.mp3");
        workspace.File("c.ico");
        workspace.File("d.txt");
        workspace.File("e.avif");

        var videoOnly = MediaFileSupport.GetBatchInputFiles(workspace.Root, imageMode: false, enableAudioConversion: false)
            .Select(Path.GetFileName)
            .ToArray();
        CollectionAssert.AreEqual(new[] { "a.mp4" }, videoOnly);

        var videoAndAudio = MediaFileSupport.GetBatchInputFiles(workspace.Root, imageMode: false, enableAudioConversion: true)
            .Select(Path.GetFileName)
            .ToArray();
        CollectionAssert.AreEqual(new[] { "a.mp4", "b.mp3" }, videoAndAudio);

        var images = MediaFileSupport.GetBatchInputFiles(workspace.Root, imageMode: true, enableAudioConversion: false)
            .Select(Path.GetFileName)
            .ToArray();
        CollectionAssert.AreEqual(new[] { "c.ico", "e.avif" }, images);
    }
}

[TestClass]
public sealed class ReleaseMetadataTests
{
    [TestMethod]
    public void VersionMetadataIsConsistent()
    {
        var root = RepositoryRoot.Find();
        var version = ProjectVersion(root);
        var assemblyVersion = ProjectProperty(root, "AssemblyVersion");
        var fileVersion = ProjectProperty(root, "FileVersion");
        var informationalVersion = ProjectProperty(root, "InformationalVersion");

        Assert.AreEqual($"{version}.0", assemblyVersion);
        Assert.AreEqual($"{version}.0", fileVersion);
        Assert.AreEqual(version, informationalVersion);
    }

    [TestMethod]
    public void PackageNamesUseProjectVersion()
    {
        var root = RepositoryRoot.Find();
        var version = ProjectVersion(root);

        Assert.AreEqual($"FFGUITool-v{version}-windows-x64-Portable", PackageName(version, "win-x64", "Portable"));
        Assert.AreEqual($"FFGUITool-v{version}-windows-arm64-Installer", PackageName(version, "win-arm64", "Installer"));
        Assert.AreEqual($"FFGUITool-v{version}-macos-intel-Portable", PackageName(version, "osx-x64", "Portable"));
        Assert.AreEqual($"FFGUITool-v{version}-macos-arm64-Installer", PackageName(version, "osx-arm64", "Installer"));
        Assert.AreEqual($"FFGUITool-v{version}-linux-x64-Portable", PackageName(version, "linux-x64", "Portable"));
    }

    [TestMethod]
    public void DocumentationReferencesCurrentPackage()
    {
        var root = RepositoryRoot.Find();
        var version = ProjectVersion(root);
        var expectedPortable = $"FFGUITool-v{version}-<platform>-Portable.zip";

        AssertFileContains(Path.Combine(root, "README.md"), expectedPortable);
        AssertFileContains(Path.Combine(root, "README.zh-CN.md"), expectedPortable);
        AssertFileContains(Path.Combine(root, "CHANGELOG.md"), $"## v{version}");
    }

    [TestMethod]
    public void ReleaseIconsExist()
    {
        var root = RepositoryRoot.Find();
        var icons = new[]
        {
            Path.Combine(root, "FFGUITool", "Resources", "icon.ico"),
            Path.Combine(root, "FFGUITool", "Resources", "icon.png"),
            Path.Combine(root, "FFGUITool", "Resources", "AppIcon.icns"),
            Path.Combine(root, "FFGUITool", "Resources", "AppIcon.png")
        };

        foreach (var icon in icons)
        {
            Assert.IsTrue(File.Exists(icon), $"Missing icon file: {icon}");
        }
    }

    private static string ProjectVersion(string root)
    {
        return ProjectProperty(root, "Version");
    }

    private static string ProjectProperty(string root, string propertyName)
    {
        var projectPath = Path.Combine(root, "FFGUITool", "FFGUITool.csproj");
        var document = XDocument.Load(projectPath);
        return document.Descendants(propertyName).FirstOrDefault()?.Value
            ?? throw new InvalidOperationException($"Missing {propertyName} in {projectPath}");
    }

    private static void AssertFileContains(string path, string expected)
    {
        StringAssert.Contains(File.ReadAllText(path), expected);
    }

    private static string PackageName(string version, string runtimeId, string packageKind)
    {
        return $"FFGUITool-v{version}-{PackagePlatformName(runtimeId)}-{packageKind}";
    }

    private static string PackagePlatformName(string runtimeId)
    {
        return runtimeId switch
        {
            "win-x64" => "windows-x64",
            "win-x86" => "windows-x86",
            "win-arm64" => "windows-arm64",
            "osx-x64" => "macos-intel",
            "osx-arm64" => "macos-arm64",
            _ => runtimeId
        };
    }
}

internal sealed class TestWorkspace : IDisposable
{
    private TestWorkspace(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public static TestWorkspace Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "FFGUITool.Tests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(root);
        return new TestWorkspace(root);
    }

    public string Directory(string name)
    {
        var path = Path.Combine(Root, name);
        System.IO.Directory.CreateDirectory(path);
        return path;
    }

    public string File(string name)
    {
        var path = Path.Combine(Root, name);
        System.IO.File.WriteAllText(path, "test");
        return path;
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Root))
        {
            System.IO.Directory.Delete(Root, recursive: true);
        }
    }
}

internal static class RepositoryRoot
{
    public static string Find()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (System.IO.File.Exists(Path.Combine(directory, "FFGUIToolAvalonia.sln")))
            {
                return directory;
            }

            directory = System.IO.Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
