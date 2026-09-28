using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFGUITool.Models;
using FFGUITool.Services;

namespace FFGUITool.ViewModels;

public partial class QueuePanelViewModel : ObservableObject, IDisposable
{
    private readonly ProcessingWorkspace _workspace;
    private readonly Func<ProcessingTask, Task> _select;
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer _stateTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly HashSet<ProcessingTask> _observed = new();
    private string _appliedSearch = "";
    private bool _shared;
    private bool _selecting;
    private bool _updating;

    public QueuePanelViewModel(ProcessingWorkspace workspace, Func<ProcessingTask, Task> select,
        IRelayCommand<ProcessingTask?> remove, IRelayCommand refreshInclusion)
    {
        _workspace = workspace; _select = select;
        RemoveCommand = remove; RefreshInclusionCommand = refreshInclusion;
        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); _appliedSearch = Search; Refresh(_shared); };
        _stateTimer.Tick += (_, _) => { _stateTimer.Stop(); Refresh(_shared); };
    }

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private bool _failedOnly;
    [ObservableProperty] private bool _sortByName;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private bool _isBusy;
    public QueueTaskCollection Tasks { get; } = new();
    public IRelayCommand<ProcessingTask?> RemoveCommand { get; }
    public IRelayCommand RefreshInclusionCommand { get; }
    public ProcessingTask? SelectedTask
    {
        get => _shared ? null : _workspace.SelectedIndependentTask;
        set
        {
            // Move/remove notifications can temporarily change ListBox selection.
            // Only user selection should launch media analysis and restore editor state.
            if (value != null && !_shared && !_updating && !value.IsSelected) _ = SelectAsync(value);
        }
    }

    private async Task SelectAsync(ProcessingTask task)
    {
        if (_selecting || IsBusy) { NotifySelectionChanged(); return; }
        _selecting = true;
        try { await _select(task); }
        finally { _selecting = false; NotifySelectionChanged(); }
    }

    public void NotifySelectionChanged() => OnPropertyChanged(nameof(SelectedTask));
    partial void OnSearchChanged(string value) { _searchTimer.Stop(); _searchTimer.Start(); }
    partial void OnFailedOnlyChanged(bool value) => Refresh(_shared);
    partial void OnSortByNameChanged(bool value) => Refresh(_shared);

    public void Refresh(bool shared)
    {
        _stateTimer.Stop(); _shared = shared;
        var source = shared ? _workspace.SharedTasks : _workspace.IndependentTasks;
        var current = source.ToHashSet();
        foreach (var task in _observed.Where(t => !current.Contains(t)).ToArray())
        { task.PropertyChanged -= OnTaskChanged; _observed.Remove(task); }
        foreach (var task in source)
            if (_observed.Add(task)) task.PropertyChanged += OnTaskChanged;
        IEnumerable<ProcessingTask> desired = source.Where(task => (!FailedOnly || task.IsFailed) &&
            (string.IsNullOrWhiteSpace(_appliedSearch) || task.InputPath.Contains(_appliedSearch, StringComparison.OrdinalIgnoreCase)));
        if (SortByName) desired = desired.OrderBy(t => t.FileName, StringComparer.CurrentCultureIgnoreCase);
        _updating = true;
        try { QueueViewUpdater.Update(Tasks, desired.ToArray()); }
        finally { _updating = false; }
        IsVisible = shared ? source.Count > 0 : source.Count > 5;
        NotifySelectionChanged();
    }

    private void OnTaskChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (FailedOnly && e.PropertyName == nameof(ProcessingTask.IsFailed))
        { _stateTimer.Stop(); _stateTimer.Start(); }
    }

    public void Dispose()
    {
        _searchTimer.Stop(); _stateTimer.Stop();
        foreach (var task in _observed) task.PropertyChanged -= OnTaskChanged;
        _observed.Clear();
    }
}
