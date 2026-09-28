using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using FFGUITool.Models;

namespace FFGUITool.Services;

/// <summary>Supports one range replacement for a large permutation, without a Reset.</summary>
public sealed class QueueTaskCollection : ObservableCollection<ProcessingTask>
{
    public QueueTaskCollection() { }
    public QueueTaskCollection(IEnumerable<ProcessingTask> tasks) : base(tasks) { }

    internal void RemoveRange(int index, int count)
    {
        CheckReentrancy();
        var removed = this.Skip(index).Take(count).ToArray();
        if (Items is List<ProcessingTask> list) list.RemoveRange(index, count);
        else for (var i = index + count - 1; i >= index; i--) Items.RemoveAt(i);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
    }

    internal void InsertRange(int index, ProcessingTask[] added)
    {
        CheckReentrancy();
        if (Items is List<ProcessingTask> list) list.InsertRange(index, added);
        else for (var i = 0; i < added.Length; i++) Items.Insert(index + i, added[i]);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, index));
    }

    internal void ReplaceOrder(IReadOnlyList<ProcessingTask> desired, int first, int last)
    {
        CheckReentrancy();
        var oldItems = this.Skip(first).Take(last - first + 1).ToArray();
        var newItems = desired.Skip(first).Take(last - first + 1).ToArray();
        for (var i = first; i <= last; i++) Items[i] = desired[i];
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, first));
    }
}
