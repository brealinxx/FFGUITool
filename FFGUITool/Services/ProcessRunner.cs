using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FFGUITool.Services;

public sealed record ProcessResult(int ExitCode, string Output, string Error);

/// <summary>Drains both pipes, bounds diagnostic memory, and reaps the process before cancellation returns.</summary>
public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string executable, IEnumerable<string> arguments,
        CancellationToken cancellationToken = default, TimeSpan? timeout = null, Action<string>? onOutput = null)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout.HasValue) deadline.CancelAfter(timeout.Value);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        deadline.Token.ThrowIfCancellationRequested();
        process.Start();
        process.StandardInput.Close();
        var stdout = DrainAsync(process.StandardOutput, onOutput);
        var stderr = DrainAsync(process.StandardError, null);
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            return new ProcessResult(process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested) throw new TimeoutException($"{executable}: process timed out.");
            throw;
        }
    }

    private static async Task<string> DrainAsync(System.IO.StreamReader reader, Action<string>? onLine)
    {
        const int limit = 256 * 1024;
        var buffer = new StringBuilder();
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            onLine?.Invoke(line);
            buffer.AppendLine(line);
            if (buffer.Length > limit) buffer.Remove(0, buffer.Length - limit);
        }
        return buffer.ToString();
    }
}
