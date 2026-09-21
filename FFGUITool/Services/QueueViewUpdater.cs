using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FFGUITool.Models;

namespace FFGUITool.Services;

public static class QueueViewUpdater
{
    // Preserve task identity and avoid Reset notifications, including on no-op refreshes.
    public static void Update(ObservableCollection<ProcessingTask> visible, IReadOnlyList<ProcessingTask> desired)
    {
        var retained = desired.ToHashSet();
        for (var i = visible.Count - 1; i >= 0; i--)
            if (!retained.Contains(visible[i])) visible.RemoveAt(i);

        var present = visible.ToHashSet();
        for (var i = 0; i < desired.Count; i++)
        {
            var task = desired[i];
            if (i < visible.Count && ReferenceEquals(visible[i], task)) continue;
            if (present.Add(task)) visible.Insert(i, task);
            else visible.Move(visible.IndexOf(task), i);
        }
    }
}
