using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FFGUITool.Services;

public enum ImportSkipReason { DifferentMode, Unsupported, Missing, Duplicate, FolderCombination, Generated }

public sealed class MediaImportReport
{
    public List<string> Files { get; } = new();
    public Dictionary<ImportSkipReason, int> Skipped { get; } = new();
    public int SkippedCount => Skipped.Values.Sum();
    public void Skip(ImportSkipReason reason) => Skipped[reason] = Skipped.GetValueOrDefault(reason) + 1;

    public string Format(int imported, bool cancelled = false)
    {
        var summary = LocalizationService.Format(cancelled ? "Improve.ImportCancelled" : "Improve.ImportSummary", imported, SkippedCount);
        var reasons = Skipped.Select(pair => LocalizationService.Format("Improve.Skip" + pair.Key, pair.Value));
        return SkippedCount == 0 ? summary : summary + " · " + string.Join("；", reasons);
    }

    public static ImportSkipReason? ClassifyFile(string path, bool imageMode, bool allowAudio, bool skipGenerated = true)
    {
        if (!File.Exists(path)) return ImportSkipReason.Missing;
        if (skipGenerated && (PathIdentity.IsGenerated(path) || MediaFileSupport.GeneratedPaths.Contains(Path.GetFullPath(path)))) return ImportSkipReason.Generated;
        var ext = Path.GetExtension(path);
        if (imageMode ? MediaFileSupport.IsImageExtension(ext) : MediaFileSupport.IsVideoExtension(ext) || allowAudio && MediaFileSupport.IsAudioExtension(ext)) return null;
        return MediaFileSupport.IsImageExtension(ext) || MediaFileSupport.IsVideoExtension(ext) || MediaFileSupport.IsAudioExtension(ext)
            ? ImportSkipReason.DifferentMode : ImportSkipReason.Unsupported;
    }

    // A sole folder is handled by the folder workflow; combinations remain explicit skips.
    public static MediaImportReport SelectFiles(IEnumerable<string> paths, bool imageMode, IEnumerable<string> existing, CancellationToken token = default)
    {
        var report = new MediaImportReport();
        var seen = new HashSet<string>(existing.Select(Path.GetFullPath), PathIdentity.Comparer);
        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(path)) { report.Skip(ImportSkipReason.Missing); continue; }
            if (!seen.Add(Path.GetFullPath(path))) { report.Skip(ImportSkipReason.Duplicate); continue; }
            var reason = Directory.Exists(path) ? ImportSkipReason.FolderCombination : ClassifyFile(path, imageMode, true, skipGenerated: false);
            if (reason is { } skip) report.Skip(skip);
            else report.Files.Add(path);
        }
        return report;
    }
}
