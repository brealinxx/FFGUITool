using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FFGUITool.Models;

namespace FFGUITool.Services;

public sealed class SavedPreset
{
    public string Name { get; set; } = "";
    public CompressionSettings Settings { get; set; } = new();
    public override string ToString() => Name;
}

public sealed class HistoryEntry
{
    public string InputPath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public long BeforeBytes { get; set; }
    public long AfterBytes { get; set; }
    public string Warning { get; set; } = "";
    public DateTime FinishedAt { get; set; }
    public CompressionSettings Settings { get; set; } = new();
    public string Summary => $"{Path.GetFileName(InputPath)}  ·  {VideoInfo.FormatFileSize(BeforeBytes)} → {VideoInfo.FormatFileSize(AfterBytes)}" +
        (BeforeBytes > 0 ? $"  ({(AfterBytes - BeforeBytes) * 100.0 / BeforeBytes:+0.0;-0.0;0}%)" : "");
}

public sealed class WorkspaceDocument
{
    public int Version { get; set; } = 1;
    public bool ImageMode { get; set; }
    public bool FolderMode { get; set; }
    public string FolderPath { get; set; } = "";
    public List<ProcessingTask> Tasks { get; set; } = new();
    public List<SavedPreset> Presets { get; set; } = new();
    public List<HistoryEntry> History { get; set; } = new();
    public List<string> GeneratedPaths { get; set; } = new();
    public CompressionSettings? RecentVideoSettings { get; set; }
    public CompressionSettings? RecentImageSettings { get; set; }
}

public sealed class WorkspaceStore
{
    private readonly string _path;
    public WorkspaceStore(string? path = null) => _path = path ?? Path.Combine(AppConfigService.AppDataPath, "workspace.json");
    public WorkspaceDocument Load()
    {
        if (!File.Exists(_path)) return new();
        try { return JsonSerializer.Deserialize<WorkspaceDocument>(File.ReadAllText(_path)) ?? new(); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        { AppLogger.Warn($"Workspace could not be read: {ex.Message}"); return new(); }
    }
    public void Save(WorkspaceDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        using var transaction = new OutputTransaction(_path, true);
        File.WriteAllText(transaction.TemporaryPath, JsonSerializer.Serialize(document));
        transaction.Commit();
    }
}
