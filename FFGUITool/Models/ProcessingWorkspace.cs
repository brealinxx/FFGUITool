using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FFGUITool.Models
{
    public enum ProcessingTaskSelection
    {
        Current,
        All
    }

    /// <summary>
    /// Owns the two task views shown by the UI while exposing one execution model.
    /// </summary>
    public sealed class ProcessingWorkspace
    {
        public ObservableCollection<ProcessingTask> IndependentTasks { get; } = new();
        public ObservableCollection<ProcessingTask> SharedTasks { get; } = new();

        public ProcessingTask? SelectedIndependentTask => IndependentTasks.FirstOrDefault(task => task.IsSelected);

        public ProcessingTask? AddIndependentTasks(
            IEnumerable<string> paths,
            Func<string, ProcessingTask> taskFactory)
        {
            ProcessingTask? first = null;
            foreach (var path in paths)
            {
                var task = IndependentTasks.FirstOrDefault(item =>
                    string.Equals(item.InputPath, path, StringComparison.OrdinalIgnoreCase));
                if (task == null)
                {
                    task = taskFactory(path);
                    IndependentTasks.Add(task);
                }

                first ??= task;
            }

            return first;
        }

        public ProcessingTask? RemoveIndependentTask(ProcessingTask task)
        {
            var index = IndependentTasks.IndexOf(task);
            var wasSelected = task.IsSelected;
            IndependentTasks.Remove(task);
            if (!wasSelected || IndependentTasks.Count == 0)
            {
                return null;
            }

            return IndependentTasks[Math.Min(Math.Max(index, 0), IndependentTasks.Count - 1)];
        }

        public void SelectIndependentTask(string inputPath)
        {
            foreach (var task in IndependentTasks)
            {
                task.IsSelected = string.Equals(task.InputPath, inputPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        public void ReplaceSharedTasks(
            IEnumerable<string> paths,
            Func<string, ProcessingTask> taskFactory)
        {
            var existing = SharedTasks.ToDictionary(task => task.InputPath, StringComparer.OrdinalIgnoreCase);
            SharedTasks.Clear();
            foreach (var path in paths)
            {
                SharedTasks.Add(existing.TryGetValue(path, out var task) ? task : taskFactory(path));
            }
        }

        public IReadOnlyList<ProcessingTask> GetExecutionTasks(
            bool sharedMode,
            ProcessingTaskSelection selection,
            bool failedOnly)
        {
            if (sharedMode)
            {
                return SharedTasks
                    .Where(task => task.IsIncluded && (!failedOnly || task.IsFailed))
                    .ToList();
            }

            var tasks = selection == ProcessingTaskSelection.Current
                ? IndependentTasks.Where(task => task.IsSelected)
                : IndependentTasks.Where(task => !failedOnly || task.IsFailed);
            return tasks.ToList();
        }

        public void ClearIndependentTasks()
        {
            IndependentTasks.Clear();
        }

        public void ClearSharedTasks()
        {
            SharedTasks.Clear();
        }

        public void Clear()
        {
            IndependentTasks.Clear();
            SharedTasks.Clear();
        }
    }
}
