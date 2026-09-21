using System;
using System.IO;

namespace FFGUITool.Services;

/// <summary>Only a validated output is published. The old destination survives failure and cancellation.</summary>
public sealed class OutputTransaction : IDisposable
{
    public string Destination { get; }
    public string TemporaryPath { get; }
    private readonly bool _overwrite;

    public OutputTransaction(string destination, bool overwrite)
    {
        Destination = Path.GetFullPath(destination);
        _overwrite = overwrite;
        var directory = Path.GetDirectoryName(Destination)!;
        Directory.CreateDirectory(directory);
        TemporaryPath = Path.Combine(directory, $".ffguitool-{Guid.NewGuid():N}{Path.GetExtension(destination)}");
    }

    public void Commit()
    {
        if (!File.Exists(TemporaryPath) || new FileInfo(TemporaryPath).Length == 0)
            throw new IOException("No valid output was generated.");
        // Same-directory rename keeps publication on one filesystem; overwrite is explicit.
        File.Move(TemporaryPath, Destination, _overwrite);
    }

    public void Dispose()
    {
        try { File.Delete(TemporaryPath); }
        catch (IOException ex) { AppLogger.Warn(ex.Message); }
        catch (UnauthorizedAccessException ex) { AppLogger.Warn(ex.Message); }
    }
}
