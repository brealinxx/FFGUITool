using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using FFGUITool.Models;

namespace FFGUITool.Services;

/// <summary>Recomputes collection facts only after membership, inclusion or failure changes.</summary>
public sealed class BatchQueueSummary : IDisposable
{
    private readonly ObservableCollection<ProcessingTask> _tasks;
    private readonly HashSet<ProcessingTask> _observed = new();
    private bool _dirty = true;
    private int _included;
    private bool _failed;
    private ProcessingTask? _first;
    private readonly Dictionary<long, int> _sizes = new();
    private double _ratio = double.NaN;
    private long _estimate;

    public BatchQueueSummary(ObservableCollection<ProcessingTask> tasks)
    {
        _tasks = tasks;
        foreach (var task in tasks) Observe(task);
        tasks.CollectionChanged += OnCollectionChanged;
    }

    public int IncludedCount { get { Refresh(); return _included; } }
    public bool HasFailures { get { Refresh(); return _failed; } }
    public ProcessingTask? FirstIncluded { get { Refresh(); return _first; } }

    public long EstimateBytes(double ratio)
    {
        Refresh();
        if (_ratio != ratio)
        {
            _ratio = ratio;
            _estimate = _sizes.Sum(pair => (long)Math.Max(1, Math.Round(pair.Key * ratio)) * pair.Value);
        }
        return _estimate;
    }

    private void Observe(ProcessingTask task)
    {
        if (_observed.Add(task)) task.PropertyChanged += OnTaskChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _dirty = true;
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var task in _observed) task.PropertyChanged -= OnTaskChanged;
            _observed.Clear();
            foreach (var task in _tasks) Observe(task);
            return;
        }
        if (e.OldItems != null)
            foreach (ProcessingTask task in e.OldItems) { task.PropertyChanged -= OnTaskChanged; _observed.Remove(task); }
        if (e.NewItems != null)
            foreach (ProcessingTask task in e.NewItems) Observe(task);
    }

    private void OnTaskChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProcessingTask.IsIncluded) or nameof(ProcessingTask.IsFailed) or nameof(ProcessingTask.SourceBytes)) _dirty = true;
    }

    private void Refresh()
    {
        if (!_dirty) return;
        _dirty = false;
        _included = 0; _failed = false; _first = null; _sizes.Clear(); _ratio = double.NaN;
        foreach (var task in _tasks)
        {
            if (!task.IsIncluded) continue;
            _included++; _failed |= task.IsFailed; _first ??= task;
            _sizes[task.SourceBytes] = _sizes.GetValueOrDefault(task.SourceBytes) + 1;
        }
    }

    public void Dispose()
    {
        _tasks.CollectionChanged -= OnCollectionChanged;
        foreach (var task in _observed) task.PropertyChanged -= OnTaskChanged;
        _observed.Clear();
    }
}
