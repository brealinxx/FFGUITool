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

        public async Task<string> ReadAllMetadataDetails(string filePath, bool localizeLabels = false)
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
                    var value = FormatJsonValue(property.Value);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    var label = localizeLabels ? FormatDetailedMetadataLabel(property.Name) : property.Name;
                    lines.Add($"{label}: {value}");
                }

                return string.Join(Environment.NewLine, lines.Distinct().Take(240));
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

        private static string FormatDetailedMetadataLabel(string key)
        {
            var normalized = NormalizeExifToolKey(key);
            return normalized switch
            {
                "SourceFile" => "文件路径",
                "FileName" => "文件名",
                "Directory" => "所在文件夹",
                "FileSize" => "文件大小",
                "FileModifyDate" => "文件修改时间",
                "FileAccessDate" => "文件访问时间",
                "FileCreateDate" => "文件创建时间",
                "FilePermissions" => "文件权限",
                "FileType" => "文件类型",
                "FileTypeExtension" => "文件扩展名",
                "MIMEType" => "MIME 类型",
                "ExifByteOrder" => "Exif 字节序",
                "ImageWidth" => "图片宽度",
                "ImageHeight" => "图片高度",
                "ImageSize" => "图片尺寸",
                "Megapixels" => "像素总量",
                "Make" => "设备厂商",
                "Model" => "设备型号",
                "LensModel" => "镜头型号",
                "LensMake" => "镜头厂商",
                "LensInfo" => "镜头信息",
                "SerialNumber" => "序列号",
                "Software" => "软件",
                "Artist" => "作者",
                "Author" => "作者",
                "Creator" => "创建者",
                "Copyright" => "版权",
                "Description" => "描述",
                "Comment" => "注释",
                "Title" => "标题",
                "Subject" => "主题",
                "Keywords" => "关键词",
                "CreateDate" => "创建时间",
                "DateTimeOriginal" => "拍摄时间",
                "ModifyDate" => "修改时间",
                "TrackCreateDate" => "轨道创建时间",
                "TrackModifyDate" => "轨道修改时间",
                "MediaCreateDate" => "媒体创建时间",
                "MediaModifyDate" => "媒体修改时间",
                "Duration" => "时长",
                "MediaDuration" => "媒体时长",
                "TrackDuration" => "轨道时长",
                "AvgBitrate" => "平均码率",
                "Bitrate" => "码率",
                "VideoFrameRate" => "视频帧率",
                "FrameRate" => "帧率",
                "CompressorID" => "压缩器 ID",
                "CompressorName" => "压缩器名称",
                "HandlerDescription" => "处理器描述",
                "HandlerType" => "处理器类型",
                "HandlerVendorID" => "处理器厂商 ID",
                "MajorBrand" => "主要品牌",
                "CompatibleBrands" => "兼容品牌",
                "MovieHeaderVersion" => "视频头版本",
                "TimeScale" => "时间刻度",
                "PreferredRate" => "首选播放速率",
                "PreferredVolume" => "首选音量",
                "Rotation" => "旋转角度",
                "MatrixStructure" => "矩阵结构",
                "AudioFormat" => "音频格式",
                "AudioChannels" => "音频声道",
                "AudioBitsPerSample" => "音频采样位深",
                "AudioSampleRate" => "音频采样率",
                "Balance" => "声道平衡",
                "GPSLatitude" => "GPS 纬度",
                "GPSLongitude" => "GPS 经度",
                "GPSAltitude" => "GPS 高度",
                "GPSPosition" => "GPS 位置",
                "GPSCoordinates" => "GPS 坐标",
                "Location" => "位置",
                "LocationName" => "位置名称",
                "UserDataGPSCoordinates" => "用户 GPS 坐标",
                "ExposureTime" => "曝光时间",
                "FNumber" => "光圈",
                "ISO" => "ISO 感光度",
                "FocalLength" => "焦距",
                "FocalLengthIn35mmFormat" => "35mm 等效焦距",
                "ExposureProgram" => "曝光程序",
                "ExposureMode" => "曝光模式",
                "MeteringMode" => "测光模式",
                "WhiteBalance" => "白平衡",
                "Flash" => "闪光灯",
                "Orientation" => "方向",
                "ColorSpace" => "色彩空间",
                "ColorComponents" => "颜色通道数",
                "YCbCrSubSampling" => "YCbCr 采样",
                _ => key
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
