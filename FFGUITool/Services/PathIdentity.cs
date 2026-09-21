using System;
using System.IO;

namespace FFGUITool.Services;

public static class PathIdentity
{
    public static StringComparer Comparer { get; } = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    public static bool Equals(string left, string right) => Comparer.Equals(Path.GetFullPath(left), Path.GetFullPath(right));
    public static bool IsGenerated(string path) => Path.GetFileName(path).Contains("_FFGUIToolOutPut_", StringComparison.OrdinalIgnoreCase) ||
        Path.GetFileName(path).StartsWith(".ffguitool-", StringComparison.OrdinalIgnoreCase);
}
