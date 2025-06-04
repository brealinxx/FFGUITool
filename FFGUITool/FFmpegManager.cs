using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace FFGUITool.Services
{
    public class FFmpegManager
    {
        private string _ffmpegPath = "";
        private readonly string _appDataPath;
        private readonly string _embeddedFFmpegPath;

        public string FFmpegPath => _ffmpegPath;
        public bool IsFFmpegAvailable { get; private set; }

        public FFmpegManager()
        {
            // 创建应用数据目录
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FFmpegGUI");
            _embeddedFFmpegPath = Path.Combine(_appDataPath, "ffmpeg");
            
            Directory.CreateDirectory(_appDataPath);
            Directory.CreateDirectory(_embeddedFFmpegPath);
        }

        /// <summary>
        /// 初始化FFmpeg路径检测
        /// </summary>
        public async Task InitializeAsync()
        {
            // 1. 检查用户设置的自定义路径
            var customPath = LoadCustomPath();
            if (!string.IsNullOrEmpty(customPath) && await IsValidFFmpegPath(customPath))
            {
                _ffmpegPath = customPath;
                IsFFmpegAvailable = true;
                return;
            }

            // 2. 检查内置FFmpeg
            var embeddedPath = GetEmbeddedFFmpegExecutable();
            if (File.Exists(embeddedPath) && await IsValidFFmpegPath(embeddedPath))
            {
                _ffmpegPath = embeddedPath;
                IsFFmpegAvailable = true;
                return;
            }

            // 3. 检查系统PATH中的FFmpeg
            if (await IsValidFFmpegPath("ffmpeg"))
            {
                _ffmpegPath = "ffmpeg";
                IsFFmpegAvailable = true;
                return;
            }

            // 4. 都没找到
            IsFFmpegAvailable = false;
        }

        /// <summary>
        /// 验证FFmpeg路径是否有效
        /// </summary>
        public async Task<bool> IsValidFFmpegPath(string path)
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
                    throw new Exception("在压缩包中未找到ffmpeg可执行文件");
                }

                // 验证安装
                if (await IsValidFFmpegPath(ffmpegExe))
                {
                    _ffmpegPath = ffmpegExe;
                    IsFFmpegAvailable = true;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"安装FFmpeg失败: {ex.Message}");
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

        /// <summary>
        /// 获取FFmpeg版本信息
        /// </summary>
        public async Task<string> GetFFmpegVersion()
        {
            if (!IsFFmpegAvailable) return "FFmpeg未安装";

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
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

                if (process.ExitCode == 0)
                {
                    var lines = output.Split('\n');
                    return lines.Length > 0 ? lines[0].Trim() : "未知版本";
                }
            }
            catch { }

            return "无法获取版本信息";
        }

        private string GetEmbeddedFFmpegExecutable()
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            return Path.Combine(_embeddedFFmpegPath, "bin", fileName);
        }

        private string FindFFmpegExecutable(string directory)
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            
            // 常见的路径
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

            // 递归搜索
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
                System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, extractPath);
            }
            else if (extension == ".7z" || extension == ".tar" || extension == ".gz")
            {
                // 对于其他格式，可以使用第三方库或系统命令
                throw new NotSupportedException($"暂不支持 {extension} 格式的压缩包");
            }
            else
            {
                throw new NotSupportedException("不支持的压缩包格式");
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