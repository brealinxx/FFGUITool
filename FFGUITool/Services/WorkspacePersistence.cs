using System;
using System.Linq;
using System.Threading.Tasks;

namespace FFGUITool.Services;

/// <summary>A single background writer; a pending snapshot is replaced by the newest request.</summary>
public sealed class WorkspacePersistence
{
    private readonly object _gate = new();
    private readonly Action<WorkspaceDocument> _save;
    private WorkspaceDocument? _pending;
    private Task _worker = Task.CompletedTask;
    private Exception? _error;

    public WorkspacePersistence(Action<WorkspaceDocument> save) => _save = save;
    public event Action<Exception>? SaveFailed;

    // Must run on the owner thread. The writer never reads live UI collections or settings.
    public static WorkspaceDocument Capture(WorkspaceDocument source) => new()
    {
        Version = source.Version, ImageMode = source.ImageMode, FolderMode = source.FolderMode,
        FolderPath = source.FolderPath,
        Tasks = source.Tasks.Select(t => t.CreateSnapshot()).ToList(),
        Presets = source.Presets.Select(p => new SavedPreset { Name = p.Name, Settings = p.Settings.Clone() }).ToList(),
        History = source.History.Take(100).Select(h => new HistoryEntry
        {
            InputPath = h.InputPath, OutputPath = h.OutputPath, BeforeBytes = h.BeforeBytes,
            AfterBytes = h.AfterBytes, Warning = h.Warning, FinishedAt = h.FinishedAt, Settings = h.Settings.Clone()
        }).ToList(),
        GeneratedPaths = source.GeneratedPaths.ToList(),
        RecentImageSettings = source.RecentImageSettings?.Clone(), RecentVideoSettings = source.RecentVideoSettings?.Clone()
    };

    // Ownership of the detached snapshot is transferred to this writer.
    public void Enqueue(WorkspaceDocument snapshot)
    {
        lock (_gate)
        {
            _pending = snapshot;
            if (_worker.IsCompleted) _worker = Task.Run(WritePending);
        }
    }

    public async Task<Exception?> FlushAsync()
    {
        while (true)
        {
            Task worker;
            lock (_gate) worker = _worker;
            await worker.ConfigureAwait(false);
            lock (_gate)
                if (ReferenceEquals(worker, _worker) && _pending == null) return _error;
        }
    }

    private void WritePending()
    {
        while (true)
        {
            WorkspaceDocument snapshot;
            lock (_gate)
            {
                if (_pending == null)
                {
                    // Mark idle under the same lock as Enqueue, avoiding a lost last request.
                    _worker = Task.CompletedTask;
                    return;
                }
                snapshot = _pending;
                _pending = null;
            }
            try
            {
                _save(snapshot);
                lock (_gate) _error = null;
            }
            catch (Exception ex)
            {
                lock (_gate) _error = ex;
                SaveFailed?.Invoke(ex);
            }
        }
    }
}
