using System;
using Avalonia.Threading;
using FFGUITool.Models;

namespace FFGUITool.Services;

/// <summary>Coalesces running events; terminal events are applied before Report returns.</summary>
public sealed class UiProcessingProgress : IProgress<ProcessingProgress>, IDisposable
{
    private readonly Action<ProcessingProgress> _apply;
    private readonly ProcessingProgressBuffer _buffer = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    public UiProcessingProgress(Action<ProcessingProgress> apply)
    {
        _apply = apply;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Report(ProcessingProgress value)
    {
        if (value.State == ProcessingTaskState.Running) { _buffer.ReportRunning(value); return; }
        // Complete and Drain both run on the UI thread, so a drained running value
        // cannot overtake a terminal event. Late running reports are rejected too.
        void Complete() { _buffer.Complete(value.Task); _apply(value); }
        if (Dispatcher.UIThread.CheckAccess()) Complete();
        else Dispatcher.UIThread.Invoke(Complete);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        foreach (var value in _buffer.Drain()) _apply(value);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        OnTick(null, EventArgs.Empty);
    }
}
