using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FFGUITool.Services
{
    public static class SystemNotificationService
    {
        public static void Show(string title, string message, bool isError = false)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ShowWindowsNotification(title, message, isError);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    StartDetached("osascript", $"-e \"display notification {QuoteAppleScript(message)} with title {QuoteAppleScript(title)}\"");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var urgency = isError ? "critical" : "normal";
                    StartDetached("notify-send", $"-u {urgency} \"{EscapeArgument(title)}\" \"{EscapeArgument(message)}\"");
                }
            }
            catch
            {
                // System notifications are best-effort; the in-app dialog still reports the result.
            }
        }

        private static void ShowWindowsNotification(string title, string message, bool isError)
        {
            var icon = isError ? "Error" : "Info";
            var systemIcon = isError ? "Error" : "Information";
            var script = $@"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$notification = New-Object System.Windows.Forms.NotifyIcon
$notification.Icon = [System.Drawing.SystemIcons]::{systemIcon}
$notification.Visible = $true
$notification.ShowBalloonTip(8000, @'
{title}
'@, @'
{message}
'@, [System.Windows.Forms.ToolTipIcon]::{icon})
Start-Sleep -Seconds 9
$notification.Dispose()
";
            var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            StartDetached("powershell", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedScript}");
        }

        private static void StartDetached(string fileName, string arguments)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        private static string QuoteAppleScript(string value)
        {
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }

        private static string EscapeArgument(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
