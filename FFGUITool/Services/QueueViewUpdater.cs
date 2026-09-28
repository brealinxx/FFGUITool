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
        {
            if (retained.Contains(visible[i])) continue;
            if (visible is QueueTaskCollection removing)
            {
                var end = i;
                while (i > 0 && !retained.Contains(visible[i - 1])) i--;
                removing.RemoveRange(i, end - i + 1);
            }
            else visible.RemoveAt(i);
        }

        // A large permutation otherwise generates hundreds of selection/layout updates.
        // Preserve unchanged edge containers; small edits retain the Move/Insert path.
        if (visible is QueueTaskCollection range && visible.Count == desired.Count)
        {
            var first = -1;
            var last = -1;
            var changed = 0;
            for (var i = 0; i < desired.Count; i++)
                if (!ReferenceEquals(visible[i], desired[i]))
                { if (first < 0) first = i; last = i; changed++; }
            if (changed > 128 && changed > desired.Count / 3)
            { range.ReplaceOrder(desired, first, last); return; }
        }

        var present = visible.ToHashSet();
        for (var i = 0; i < desired.Count; i++)
        {
            var task = desired[i];
            if (i < visible.Count && ReferenceEquals(visible[i], task)) continue;
            if (present.Add(task))
            {
                if (visible is QueueTaskCollection inserting)
                {
                    var added = new List<ProcessingTask> { task };
                    while (i + added.Count < desired.Count && present.Add(desired[i + added.Count]))
                        added.Add(desired[i + added.Count]);
                    inserting.InsertRange(i, added.ToArray());
                    i += added.Count - 1;
                }
                else visible.Insert(i, task);
            }
            else visible.Move(visible.IndexOf(task), i);
        }
    }
}
