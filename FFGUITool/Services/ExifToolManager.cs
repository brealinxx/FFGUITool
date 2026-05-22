using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace FFGUITool.Services
{
    public sealed class ExifToolManager
    {
        private readonly string _appDataPath;
        private readonly string _embeddedExifToolPath;
        private string _exifToolPath = "";

        public string ExifToolPath => _exifToolPath;
        public bool IsExifToolAvailable { get; private set; }

        public ExifToolManager()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FFGUITool");
            _embeddedExifToolPath = Path.Combine(_appDataPath, "exiftool");
            Directory.CreateDirectory(_appDataPath);
            Directory.CreateDirectory(_embeddedExifToolPath);
        }

        public async Task InitializeAsync()
        {
            var customPath = LoadCustomPath();
            if (!string.IsNullOrWhiteSpace(customPath) && await IsValidExifToolPath(customPath))
            {
                _exifToolPath = customPath;
                IsExifToolAvailable = true;
                return;
            }

            if (await IsValidExifToolPath("exiftool"))
            {
                _exifToolPath = "exiftool";
                IsExifToolAvailable = true;
                return;
            }

            var embeddedPath = FindExifToolExecutable(_embeddedExifToolPath);
            if (!string.IsNullOrWhiteSpace(embeddedPath) && await IsValidExifToolPath(embeddedPath))
            {
                _exifToolPath = embeddedPath;
                IsExifToolAvailable = true;
                return;
            }

            foreach (var path in GetCommonExifToolPaths())
            {
                if (File.Exists(path) && await IsValidExifToolPath(path))
                {
                    _exifToolPath = path;
                    IsExifToolAvailable = true;
                    return;
                }
            }

            IsExifToolAvailable = false;
            _exifToolPath = "";
        }

        public async Task<bool> SetCustomPath(string path)
        {
            if (await IsValidExifToolPath(path))
            {
                _exifToolPath = path;
                IsExifToolAvailable = true;
                SaveCustomPath(path);
                return true;
            }

            return false;
        }

        public async Task<bool> SetCustomDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return false;
            }

            var executable = FindExifToolExecutable(directory);
            return !string.IsNullOrWhiteSpace(executable) && await SetCustomPath(executable);
        }

        public async Task<bool> SetSystemExifTool()
        {
            if (await IsValidExifToolPath("exiftool"))
            {
                _exifToolPath = "exiftool";
                IsExifToolAvailable = true;
                SaveCustomPath("exiftool");
                return true;
            }

            return false;
        }

        public async Task<bool> InstallFromArchive(string archivePath)
        {
            if (!File.Exists(archivePath) || !string.Equals(Path.GetExtension(archivePath), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (Directory.Exists(_embeddedExifToolPath))
            {
                Directory.Delete(_embeddedExifToolPath, true);
            }

            Directory.CreateDirectory(_embeddedExifToolPath);
            await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, _embeddedExifToolPath));

            var executable = FindExifToolExecutable(_embeddedExifToolPath);
            executable = NormalizeWindowsExecutableName(executable);
            return !string.IsNullOrWhiteSpace(executable) && await SetCustomPath(executable);
        }

        public async Task<bool> IsValidExifToolPath(string path)
        {
            try
            {
                var result = await RunExifTool(path, "-ver");
                return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> ReadSensitiveMetadata(string filePath)
        {
            if (!IsExifToolAvailable || !File.Exists(filePath))
            {
                return "";
            }

            var result = await RunExifTool(_exifToolPath, $"-j -G1 -a -s \"{filePath}\"");
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                return "";
            }

            try
            {
                using var document = JsonDocument.Parse(result.Output);
                var first = document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0
                    ? document.RootElement[0]
                    : default;
                if (first.ValueKind != JsonValueKind.Object)
                {
                    return "";
                }

                var lines = new List<string>();
                foreach (var property in first.EnumerateObject())
                {
                    var key = NormalizeExifToolKey(property.Name);
                    if (!IsSensitiveKey(key))
                    {
                        continue;
                    }

                    var value = FormatJsonValue(property.Value);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    lines.Add($"{FormatMetadataLabel(key)}: {value}");
                }

                return string.Join("\n", lines.Distinct().Take(24));
            }
            catch
            {
                return "";
            }
        }

        public async Task<(bool Success, string Error)> ClearMetadata(string filePath)
        {
            if (!IsExifToolAvailable)
            {
                return (false, "ExifTool is not configured.");
            }

            if (!File.Exists(filePath))
            {
                return (false, "Output file does not exist.");
            }

            var result = await RunExifTool(_exifToolPath, $"-overwrite_original -all= \"{filePath}\"");
            return (result.ExitCode == 0, result.Error);
        }

        public string BuildClearMetadataCommand(string outputPath)
        {
            var executable = string.IsNullOrWhiteSpace(_exifToolPath) ? "exiftool" : _exifToolPath;
            return $"\"{executable}\" -overwrite_original -all= \"{outputPath}\"";
        }

        public async Task<string> GetExifToolVersion()
        {
            if (!IsExifToolAvailable)
            {
                return LocalizationService.T("ExifTool.NotInstalled");
            }

            try
            {
                var result = await RunExifTool(_exifToolPath, "-ver");
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                {
                    return $"ExifTool {result.Output.Trim()}";
                }
            }
            catch
            {
            }

            return LocalizationService.T("ExifTool.VersionUnavailable");
        }

        private static async Task<(int ExitCode, string Output, string Error)> RunExifTool(string fileName, string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            var waitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(10)));
            if (completed != waitTask)
            {
                try
                {
                    process.Kill(true);
                    await process.WaitForExitAsync();
                }
                catch
                {
                }

                return (-1, "", "ExifTool timed out.");
            }

            var output = await outputTask;
            var error = await errorTask;
            return (process.ExitCode, output, error);
        }

        private static string NormalizeExifToolKey(string key)
        {
            var colonIndex = key.LastIndexOf(':');
            return colonIndex >= 0 ? key[(colonIndex + 1)..] : key;
        }

        private static bool IsSensitiveKey(string key)
        {
            var normalized = key.Replace("_", "", StringComparison.OrdinalIgnoreCase);
            var keywords = new[]
            {
                "GPS",
                "Location",
                "Latitude",
                "Longitude",
                "Altitude",
                "Make",
                "Model",
                "Lens",
                "Serial",
                "Owner",
                "Artist",
                "Author",
                "Creator",
                "Byline",
                "Copyright",
                "CreateDate",
                "DateTimeOriginal",
                "ModifyDate",
                "Software",
                "HostComputer",
                "Device",
                "Camera",
                "Firmware",
                "MakerNotes"
            };

            return keywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatMetadataLabel(string key)
        {
            return key switch
            {
                "GPSPosition" => "GPS position",
                "GPSLatitude" => "GPS latitude",
                "GPSLongitude" => "GPS longitude",
                "DateTimeOriginal" => "Taken at",
                "CreateDate" => "Created at",
                "Make" => "Device maker",
                "Model" => "Device model",
                _ => string.Concat(key.Select((ch, index) =>
                    index > 0 && char.IsUpper(ch) ? " " + ch : ch.ToString()))
            };
        }

        private static string FormatJsonValue(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "",
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Array => string.Join(", ", value.EnumerateArray().Select(FormatJsonValue)),
                _ => value.ToString()
            };
        }

        private static IEnumerable<string> GetCommonExifToolPaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                yield return @"C:\exiftool\exiftool.exe";
                yield return @"C:\Program Files\ExifTool\exiftool.exe";
                yield return Path.Combine(userProfile, "exiftool", "exiftool.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                yield return "/usr/local/bin/exiftool";
                yield return "/opt/homebrew/bin/exiftool";
            }
            else
            {
                yield return "/usr/bin/exiftool";
                yield return "/usr/local/bin/exiftool";
            }
        }

        private static string FindExifToolExecutable(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return "";
            }

            var names = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { "exiftool.exe", "exiftool(-k).exe" }
                : new[] { "exiftool" };

            foreach (var name in names)
            {
                var direct = Path.Combine(directory, name);
                if (File.Exists(direct))
                {
                    return direct;
                }
            }

            try
            {
                return Directory
                    .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .FirstOrDefault(file => names.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
                    ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string NormalizeWindowsExecutableName(string executable)
        {
            if (string.IsNullOrWhiteSpace(executable) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return executable;
            }

            if (!string.Equals(Path.GetFileName(executable), "exiftool(-k).exe", StringComparison.OrdinalIgnoreCase))
            {
                return executable;
            }

            var normalizedPath = Path.Combine(Path.GetDirectoryName(executable) ?? "", "exiftool.exe");
            File.Copy(executable, normalizedPath, overwrite: true);
            return normalizedPath;
        }

        private string LoadCustomPath()
        {
            try
            {
                var configFile = Path.Combine(_appDataPath, "exiftool_path.config");
                return File.Exists(configFile) ? File.ReadAllText(configFile).Trim() : "";
            }
            catch
            {
                return "";
            }
        }

        private void SaveCustomPath(string path)
        {
            try
            {
                var configFile = Path.Combine(_appDataPath, "exiftool_path.config");
                File.WriteAllText(configFile, path);
            }
            catch
            {
            }
        }
    }
}
