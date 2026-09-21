using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace FFGUITool.Services
{
    /// <summary>
    /// FFmpeg管理服务
    /// </summary>
    public class FFmpegManager
    {
        private string _ffmpegPath = "";
        private readonly string _appDataPath;
        private readonly string _embeddedFFmpegPath;

        /// <summary>
        /// FFmpeg可执行文件路径
        /// </summary>
        public string FFmpegPath => _ffmpegPath;

        /// <summary>
        /// FFmpeg是否可用
        /// </summary>
        public bool IsFFmpegAvailable { get; private set; }

        public FFmpegManager(string? appDataPath = null)
        {
            // 创建应用数据目录
            _appDataPath = appDataPath ?? AppConfigService.AppDataPath;
            _embeddedFFmpegPath = Path.Combine(_appDataPath, "ffmpeg");
            
            Directory.CreateDirectory(_appDataPath);
            Directory.CreateDirectory(_embeddedFFmpegPath);
        }

        /// <summary>
        /// 初始化FFmpeg路径检测
        /// </summary>
        public async Task InitializeAsync()
        {
            AppLogger.Info("Starting FFmpeg detection.");
            // 1. 检查用户设置的自定义路径
            var customPath = LoadCustomPath();
            if (!string.IsNullOrEmpty(customPath) && await IsValidFFmpegPath(customPath))
            {
                _ffmpegPath = customPath;
                IsFFmpegAvailable = true;
                AppLogger.Info($"FFmpeg detected from custom path: {_ffmpegPath}");
                return;
            }

            // 2. 检查内置FFmpeg
            var embeddedPath = GetEmbeddedFFmpegExecutable();
            if (File.Exists(embeddedPath) && await IsValidFFmpegPath(embeddedPath))
            {
                _ffmpegPath = embeddedPath;
                IsFFmpegAvailable = true;
                AppLogger.Info($"FFmpeg detected from embedded path: {_ffmpegPath}");
                return;
            }

            // 3. 检查系统PATH中的FFmpeg
            if (await IsValidFFmpegPath("ffmpeg"))
            {
                _ffmpegPath = "ffmpeg";
                IsFFmpegAvailable = true;
                AppLogger.Info("FFmpeg detected from system PATH.");
                return;
            }

            // 4. 检查常见安装位置
            var commonPaths = GetCommonFFmpegPaths();
            foreach (var path in commonPaths)
            {
                if (File.Exists(path) && await IsValidFFmpegPath(path))
                {
                    _ffmpegPath = path;
                    IsFFmpegAvailable = true;
                    AppLogger.Info($"FFmpeg detected from common path: {_ffmpegPath}");
                    return;
                }
            }

            // 5. 都没找到
            IsFFmpegAvailable = false;
            AppLogger.Warn("FFmpeg detection completed: no usable FFmpeg was found.");
        }

        /// <summary>
        /// 验证FFmpeg路径是否有效
        /// </summary>
        public async Task<bool> IsValidFFmpegPath(string path)
        {
            try
            {
                var result = await ProcessRunner.RunAsync(path, new[] { "-version" }, timeout: TimeSpan.FromSeconds(8));
                return result.ExitCode == 0 && result.Output.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) { AppLogger.Warn($"FFmpeg detection: {ex.Message}"); return false; }
        }

        /// <summary>
        /// 安装FFmpeg到应用目录
        /// </summary>
        public async Task<bool> InstallFFmpegFromArchive(string archivePath)
        {
            try
            {
                // 清理旧的安装
                if (Directory.Exists(_embeddedFFmpegPath))
                {
                    Directory.Delete(_embeddedFFmpegPath, true);
                    Directory.CreateDirectory(_embeddedFFmpegPath);
                }

                // 解压缩文件
                await ExtractArchive(archivePath, _embeddedFFmpegPath);

                // 查找ffmpeg可执行文件
                var ffmpegExe = FindFFmpegExecutable(_embeddedFFmpegPath);
                if (string.IsNullOrEmpty(ffmpegExe))
                {
                    throw new Exception(LocalizationService.T("FFmpeg.NotFoundInArchive"));
                }

                // 在Unix系统上设置执行权限
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await SetExecutablePermission(ffmpegExe);
                }

                // 验证安装
                if (await IsValidFFmpegPath(ffmpegExe))
                {
                    _ffmpegPath = ffmpegExe;
                    IsFFmpegAvailable = true;
                    SaveCustomPath(ffmpegExe);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(LocalizationService.Format("FFmpeg.InstallFailed", ex.Message));
            }
        }

        /// <summary>
        /// 设置自定义FFmpeg路径
        /// </summary>
        public async Task<bool> SetCustomPath(string path)
        {
            if (await IsValidFFmpegPath(path))
            {
                _ffmpegPath = path;
                IsFFmpegAvailable = true;
                SaveCustomPath(path);
                return true;
            }
            return false;
        }

        public async Task<bool> SetSystemFFmpeg()
        {
            if (await IsValidFFmpegPath("ffmpeg"))
            {
                _ffmpegPath = "ffmpeg";
                IsFFmpegAvailable = true;
                SaveCustomPath("ffmpeg");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取FFmpeg版本信息
        /// </summary>
        public async Task<string> GetFFmpegVersion()
        {
            if (!IsFFmpegAvailable) return LocalizationService.T("FFmpeg.NotInstalled");
            try
            {
                var result = await ProcessRunner.RunAsync(_ffmpegPath, new[] { "-version" }, timeout: TimeSpan.FromSeconds(8));
                return result.ExitCode == 0 ? result.Output.Split('\n')[0].Trim() : LocalizationService.T("FFmpeg.VersionUnavailable");
            }
            catch (Exception ex) { AppLogger.Warn(ex.Message); return LocalizationService.T("FFmpeg.VersionUnavailable"); }
        }

        public Task<IReadOnlySet<string>> GetAvailableVideoEncoders() => GetVideoCapabilities("-encoders");
        public Task<IReadOnlySet<string>> GetAvailableVideoDecoders() => GetVideoCapabilities("-decoders");

        private async Task<IReadOnlySet<string>> GetVideoCapabilities(string option)
        {
            var capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!IsFFmpegAvailable) return capabilities;
            try
            {
                var result = await ProcessRunner.RunAsync(_ffmpegPath, new[] { "-hide_banner", option }, timeout: TimeSpan.FromSeconds(10));
                foreach (var line in (result.Output + "\n" + result.Error).Split('\n'))
                {
                    var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[0].StartsWith("V", StringComparison.Ordinal) && parts[1] != "=")
                        capabilities.Add(parts[1]);
                }
            }
            catch (Exception ex) { AppLogger.Warn($"Capability detection: {ex.Message}"); }
            return capabilities;
        }

        #region 私有辅助方法

        private string GetEmbeddedFFmpegExecutable()
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? "ffmpeg.exe" : "ffmpeg";
            return Path.Combine(_embeddedFFmpegPath, "bin", fileName);
        }

        private string[] GetCommonFFmpegPaths()
        {
            var paths = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows常见路径
                paths.Add(@"C:\ffmpeg\bin\ffmpeg.exe");
                paths.Add(@"C:\Program Files\ffmpeg\bin\ffmpeg.exe");
                paths.Add(@"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe");
                
                // 检查用户目录
                var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                paths.Add(Path.Combine(userPath, "ffmpeg", "bin", "ffmpeg.exe"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux常见路径
                paths.Add("/usr/bin/ffmpeg");
                paths.Add("/usr/local/bin/ffmpeg");
                paths.Add("/opt/ffmpeg/bin/ffmpeg");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS常见路径
                paths.Add("/usr/local/bin/ffmpeg");
                paths.Add("/opt/homebrew/bin/ffmpeg");
                paths.Add("/usr/bin/ffmpeg");
            }

            return paths.ToArray();
        }

        private string FindFFmpegExecutable(string directory)
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? "ffmpeg.exe" : "ffmpeg";
            
            // 常见的路径模式
            var possiblePaths = new[]
            {
                Path.Combine(directory, fileName),
                Path.Combine(directory, "bin", fileName),
                Path.Combine(directory, "ffmpeg", fileName),
                Path.Combine(directory, "ffmpeg", "bin", fileName),
                // 处理带版本号的目录
                Path.Combine(directory, "ffmpeg-*", "bin", fileName)
            };

            foreach (var pattern in possiblePaths)
            {
                // 处理通配符
                if (pattern.Contains("*"))
                {
                    var dir = Path.GetDirectoryName(pattern) ?? directory;
                    var searchPattern = Path.GetFileName(pattern);
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir, searchPattern, SearchOption.AllDirectories);
                        if (files.Length > 0)
                            return files[0];
                    }
                }
                else if (File.Exists(pattern))
                {
                    return pattern;
                }
            }

            // 递归搜索（限制深度避免性能问题）
            try
            {
                var files = SearchFiles(directory, fileName, maxDepth: 3);
                if (files.Any())
                    return files.First();
            }
            catch { }

            return "";
        }

        private IEnumerable<string> SearchFiles(string directory, string fileName, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth >= maxDepth)
                yield break;

            var file = Path.Combine(directory, fileName);
            if (File.Exists(file))
                yield return file;

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                // 跳过隐藏目录和系统目录
                var dirInfo = new DirectoryInfo(subDir);
                if ((dirInfo.Attributes & FileAttributes.Hidden) != 0 ||
                    (dirInfo.Attributes & FileAttributes.System) != 0)
                    continue;

                foreach (var foundFile in SearchFiles(subDir, fileName, maxDepth, currentDepth + 1))
                {
                    yield return foundFile;
                }
            }
        }

        private async Task ExtractArchive(string archivePath, string extractPath)
        {
            var extension = Path.GetExtension(archivePath).ToLower();

            if (extension == ".7z")
            {
                await ExtractTarArchive(archivePath, extractPath);
                return;
            }
            
            switch (extension)
            {
                case ".zip":
                    await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, extractPath));
                    break;
                    
                case ".7z":
                    // 如果需要支持7z，可以使用第三方库如SharpCompress
                    throw new NotSupportedException(LocalizationService.T("FFmpeg.Unsupported7z"));
                    
                case ".tar":
                case ".gz":
                case ".bz2":
                    // Unix系统可以使用tar命令
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        await ExtractTarArchive(archivePath, extractPath);
                    }
                    else
                    {
                        throw new NotSupportedException(LocalizationService.Format("FFmpeg.WindowsUnsupportedArchive", extension));
                    }
                    break;
                    
                default:
                    throw new NotSupportedException(LocalizationService.Format("FFmpeg.UnsupportedArchive", extension));
            }
        }

        private async Task ExtractTarArchive(string archivePath, string extractPath)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xf \"{archivePath}\" -C \"{extractPath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(LocalizationService.T("FFmpeg.TarMissing"), ex);
            }

            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new Exception(LocalizationService.Format("FFmpeg.ExtractFailed", error));
                }
                throw new Exception(LocalizationService.T("FFmpeg.ExtractTarFailed"));
            }
        }

        private async Task SetExecutablePermission(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var processInfo = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();
        }

        private string LoadCustomPath()
        {
            try
            {
                var configFile = Path.Combine(_appDataPath, "ffmpeg_path.config");
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
                var configFile = Path.Combine(_appDataPath, "ffmpeg_path.config");
                File.WriteAllText(configFile, path);
            }
            catch { }
        }

        #endregion
    }
}
