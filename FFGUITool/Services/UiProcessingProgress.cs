using System;
using Avalonia.Threading;
using FFGUITool.Models;

namespace FFGUITool.Services;

/// <summary>Preserves terminal event ordering before the execution summary is saved.</summary>
public sealed class UiProcessingProgress(Action<ProcessingProgress> apply) : IProgress<ProcessingProgress>
{
    public void Report(ProcessingProgress value)
    {
        if (Dispatcher.UIThread.CheckAccess()) apply(value);
        else Dispatcher.UIThread.Invoke(() => apply(value));
    }
}
