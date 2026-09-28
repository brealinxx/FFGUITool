using System.Collections.Generic;
using FFGUITool.Models;

namespace FFGUITool.Services;

/// <summary>Bounded LRU; all index and recency changes share one short lock.</summary>
public sealed class MediaInfoCache
{
    private sealed record Entry(string Path, long Size, long Modified, VideoInfo Info);
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _index = new(PathIdentity.Comparer);
    private readonly LinkedList<Entry> _recency = new();
    private const int Capacity = 512;

    public VideoInfo? Get(string path, long size, long modified)
    {
        lock (_gate)
        {
            if (!_index.TryGetValue(path, out var node)) return null;
            _recency.Remove(node);
            if (node.Value.Size != size || node.Value.Modified != modified)
            {
                _index.Remove(path);
                return null;
            }
            _recency.AddFirst(node);
            return node.Value.Info;
        }
    }

    public void Put(string path, long size, long modified, VideoInfo info)
    {
        lock (_gate)
        {
            if (_index.Remove(path, out var old)) _recency.Remove(old);
            _index[path] = _recency.AddFirst(new Entry(path, size, modified, info));
            if (_index.Count > Capacity)
            {
                _index.Remove(_recency.Last!.Value.Path);
                _recency.RemoveLast();
            }
        }
    }
}
