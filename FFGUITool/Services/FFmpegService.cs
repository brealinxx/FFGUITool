using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.Services
{
    public class FFmpegService : IFFmpegService
    {
        private string _ffmpegPath = "";
        private readonly string _appDataPath;
        private readonly string _embeddedFFmpegPath;

        public string FFmpegPath => _ffmpegPath;
        public bool IsAvailable { get; private set; }

        public FFmpegService()
        {
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FFGUITool");
            _embeddedFFmpegPath = Path.Combine(_appDataPath, "ffmpeg");
            
            Directory.CreateDirectory(_appDataPath);
            Directory.CreateDirectory(_embeddedFFmpegPath);
        }

        public async Task<bool> InitializeAsync()
        {
            // Check custom path
            var customPath = LoadCustomPath();
            if (!string.IsNullOrEmpty(customPath) && await IsValidFFmpegPath(customPath))
            {
                _ffmpegPath = customPath;
                IsAvailable = true;
                return true;
            }

            // Check embedded FFmpeg
            var embeddedPath = GetEmbeddedFFmpegExecutable();
            if (File.Exists(embeddedPath) && await IsValidFFmpegPath(embeddedPath))
            {
                _ffmpegPath = embeddedPath;
                IsAvailable = true;
                return true;
            }

            // Check system PATH
            if (await IsValidFFmpegPath("ffmpeg"))
            {
                _ffmpegPath = "ffmpeg";
                IsAvailable = true;
                return true;
            }

            IsAvailable = false;
            return false;
        }

        public async Task<string> GetVersionAsync()
        {
            if (!IsAvailable) return "FFmpeg not available";

            try
            {
                var result = await ExecuteAsync("-version");
                if (result.Success)
                {
                    var lines = result.Output.Split('\n');
                    return lines.Length > 0 ? lines[0].Trim() : "Unknown version";
                }
            }
            catch { }

            return "Unable to get version info";
        }

        public async Task<bool> SetCustomPathAsync(string path)
        {
            if (await IsValidFFmpegPath(path))
            {
                _ffmpegPath = path;
                IsAvailable = true;
                SaveCustomPath(path);
                return true;
            }
            return false;
        }

        public async Task<bool> InstallFromArchiveAsync(string archivePath)
        {
            try
            {
                if (Directory.Exists(_embeddedFFmpegPath))
                {
                    Directory.Delete(_embeddedFFmpegPath, true);
                    Directory.CreateDirectory(_embeddedFFmpegPath);
                }

                await ExtractArchive(archivePath, _embeddedFFmpegPath);

                var ffmpegExe = FindFFmpegExecutable(_embeddedFFmpegPath);
                if (string.IsNullOrEmpty(ffmpegExe))
                {
                    throw new Exception("FFmpeg executable not found in archive");
                }

                if (await IsValidFFmpegPath(ffmpegExe))
                {
                    _ffmpegPath = ffmpegExe;
                    IsAvailable = true;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to install FFmpeg: {ex.Message}");
            }
        }

        public async Task<ProcessResult> ExecuteAsync(string arguments, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
                throw new InvalidOperationException("FFmpeg is not available");

            var result = new ProcessResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                        ParseProgress(e.Data, progress);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);

                result.ExitCode = process.ExitCode;
                result.Success = process.ExitCode == 0;
                result.Output = outputBuilder.ToString();
                result.Error = errorBuilder.ToString();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        private void ParseProgress(string line, IProgress<double>? progress)
        {
            if (progress == null) return;

            // Parse FFmpeg progress output
            var timeMatch = Regex.Match(line, @"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})");
            if (timeMatch.Success)
            {
                // Calculate progress based on time
                // This is a simplified implementation
                // In production, you'd need to know the total duration
                var hours = int.Parse(timeMatch.Groups[1].Value);
                var minutes = int.Parse(timeMatch.Groups[2].Value);
                var seconds = double.Parse(timeMatch.Groups[3].Value);
                var currentTime = hours * 3600 + minutes * 60 + seconds;
                
                // Report progress (this would need total duration for accurate percentage)
                progress.Report(currentTime);
            }
        }

        private async Task<bool> IsValidFFmpegPath(string path)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return process.ExitCode == 0 && output.Contains("ffmpeg version");
            }
            catch
            {
                return false;
            }
        }

        private string GetEmbeddedFFmpegExecutable()
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            return Path.Combine(_embeddedFFmpegPath, "bin", fileName);
        }

        private string FindFFmpegExecutable(string directory)
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            
            var possiblePaths = new[]
            {
                Path.Combine(directory, fileName),
                Path.Combine(directory, "bin", fileName),
                Path.Combine(directory, "ffmpeg", fileName),
                Path.Combine(directory, "ffmpeg", "bin", fileName)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            try
            {
                var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
            }
            catch { }

            return "";
        }

        private async Task ExtractArchive(string archivePath, string extractPath)
        {
            var extension = Path.GetExtension(archivePath).ToLower();
            
            if (extension == ".zip")
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, extractPath));
            }
            else
            {
                throw new NotSupportedException($"Archive format {extension} is not supported");
            }
        }

        private string LoadCustomPath()
        {
            try
            {
                var configFile = Path.Combine(_appDataPath, "config.txt");
                if (File.Exists(configFile))
                {
                    return File.ReadAllText(configFile).Trim();
                }
            }
            catch { }
            return "";
        }

        private void SaveCustomPath(string path)
        {
            try
            {
                var configFile = Path.Combine(_appDataPath, "config.txt");
                File.WriteAllText(configFile, path);
            }
            catch { }
        }
    }
}
