using System.Collections.Generic;
using System.Linq;
using FFGUITool.Models;

namespace FFGUITool.Services;

public sealed class ProcessingProgressBuffer
{
    private readonly object _gate = new();
    private readonly Dictionary<ProcessingTask, ProcessingProgress> _pending = new();
    private readonly HashSet<ProcessingTask> _terminal = new();

    public void ReportRunning(ProcessingProgress progress)
    {
        lock (_gate)
            if (!_terminal.Contains(progress.Task)) _pending[progress.Task] = progress;
    }

    public void Complete(ProcessingTask task)
    {
        lock (_gate) { _terminal.Add(task); _pending.Remove(task); }
    }

    public ProcessingProgress[] Drain()
    {
        lock (_gate)
        {
            var values = _pending.Values.ToArray();
            _pending.Clear();
            return values;
        }
    }
}
