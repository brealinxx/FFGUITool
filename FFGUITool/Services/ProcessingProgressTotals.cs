using System;
using System.Collections.Generic;
using FFGUITool.Models;

namespace FFGUITool.Services;

/// <summary>Constant-time progress accounting, independent of mutable task state.</summary>
public sealed class ProcessingProgressTotals
{
    private readonly Dictionary<ProcessingTask, double> _contributions = new();
    private double _total;
    public void Clear() { _contributions.Clear(); _total = 0; }
    public double Apply(ProcessingProgress progress)
    {
        var contribution = progress.State switch
        {
            ProcessingTaskState.Completed or ProcessingTaskState.Warning or ProcessingTaskState.Failed => 1,
            ProcessingTaskState.Running => Math.Clamp(progress.Fraction, 0, 1),
            _ => 0
        };
        _total += contribution - _contributions.GetValueOrDefault(progress.Task);
        _contributions[progress.Task] = contribution;
        return progress.TotalCount > 0 ? Math.Clamp(_total * 100 / progress.TotalCount, 0, 100) : 0;
    }
}
