using System;
using System.Collections.Generic;
using Avalonia;

namespace FFGUITool.Services
{
    public sealed class LanguageOption
    {
        public string Code { get; }
        public string DisplayName { get; }

        public LanguageOption(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }

    public static class LocalizationService
    {
        private const string DefaultLanguage = "zh-CN";

        private static readonly Dictionary<string, Dictionary<string, string>> Resources = new()
        {
            ["zh-CN"] = new Dictionary<string, string>
            {
                ["App.Title"] = "FFmpeg 视频压缩工具",
                ["App.Title.Ready"] = "FFmpeg 视频压缩工具 - FFmpeg已就绪",
                ["App.Title.NotConfigured"] = "FFmpeg 视频压缩工具 - FFmpeg未配置",
                ["Menu.File"] = "文件(_F)",
                ["Menu.Exit"] = "退出(_X)",
                ["Menu.Tools"] = "工具(_T)",
                ["Menu.FFmpegSettings"] = "FFmpeg设置(_S)",
                ["Menu.RedetectFFmpeg"] = "重新检测FFmpeg(_R)",
                ["Menu.Language"] = "语言(_L)",
                ["Menu.Language.Chinese"] = "中文",
                ["Menu.Language.English"] = "English",
                ["Menu.Help"] = "帮助(_H)",
                ["Menu.About"] = "关于(_A)",
                ["Theme.Toggle"] = "切换主题",
                ["Main.InputSource"] = "输入源",
                ["Main.InputWatermark"] = "拖入文件或点击右侧按钮...",
                ["Main.SelectFile"] = "选择文件",
                ["Main.SelectFolder"] = "选择文件夹",
                ["Main.SourceInfo"] = "源文件信息",
                ["Main.Size"] = "大小",
                ["Main.Duration"] = "时长",
                ["Main.OriginalBitrate"] = "原比特率",
                ["Main.CompressionParams"] = "压缩参数",
                ["Main.CompressionStrength"] = "压缩强度",
                ["Main.CompressionHint"] = "支持手动输入 1-100 之间的数值，步进精度为 1%",
                ["Main.TargetBitrate"] = "目标码率",
                ["Main.CurrentSelection"] = "当前选择: {0}",
                ["Main.BitrateWarning"] = "警告：当前比特率高于原视频，文件可能会变大",
                ["Main.Codec"] = "编码器",
                ["Main.EstimatedResult"] = "预估结果",
                ["Main.OutputSettings"] = "输出设置",
                ["Main.OutputWatermark"] = "默认保存在源文件同目录下",
                ["Main.ChangeOutput"] = "更改保存位置",
                ["Main.CliPreview"] = "CLI COMMAND PREVIEW",
                ["Main.Copy"] = "复制",
                ["Main.CopyCommand"] = "复制命令",
                ["Main.Processing"] = "正在处理中...",
                ["Main.StartConvert"] = "开始转换",
                ["Status.Detecting"] = " - FFmpeg状态检测中...",
                ["Status.Redetecting"] = " - 重新检测中...",
                ["Status.Checking"] = " - 检测FFmpeg中...",
                ["Status.Ready"] = " - FFmpeg已就绪",
                ["Status.NotConfigured"] = " - FFmpeg未配置",
                ["Command.SelectInput"] = "请先选择输入文件或文件夹",
                ["Estimate.SelectVideo"] = "请先选择视频文件",
                ["Estimate.Analyzing"] = "分析视频中...",
                ["Estimate.NonVideo"] = "非视频文件",
                ["Estimate.Calculating"] = "计算中...",
                ["Estimate.Current"] = "{0}k (预估: {1:F1}MB, {2}: {3:F1}%)",
                ["Estimate.Increase"] = "增大",
                ["Estimate.Compress"] = "压缩",
                ["Codec.H264.Desc"] = "兼容性最好",
                ["Codec.H265.Desc"] = "压缩率更高",
                ["Codec.VP9.Desc"] = "开源编码",
                ["Dialog.Ok"] = "确定",
                ["Dialog.Cancel"] = "取消",
                ["Dialog.Done"] = "完成",
                ["Dialog.Error"] = "错误",
                ["Dialog.Success"] = "成功",
                ["Dialog.Warning"] = "警告",
                ["Dialog.Info"] = "提示",
                ["Dialog.DetectComplete"] = "检测完成",
                ["Dialog.AboutTitle"] = "关于 FFGUITool",
                ["Dialog.VideoComplete"] = "视频处理完成！",
                ["Dialog.FFmpegConfigUpdated"] = "FFmpeg配置已更新！",
                ["Dialog.FFmpegDetected"] = "FFmpeg检测成功！",
                ["Dialog.FFmpegMissing"] = "未找到FFmpeg，请通过菜单手动配置。",
                ["Dialog.FFmpegWarn"] = "FFmpeg未正确配置，某些功能可能无法使用。\n您可以通过菜单重新配置。",
                ["Dialog.ExecuteError"] = "执行FFmpeg命令时出错:\n{0}",
                ["Dialog.AboutMessage"] = "FFGUITool v{0}\nFFmpeg视频压缩工具\n\nFFmpeg版本: {1}\n\n© 2025 FFGUITool\nPowered by FFmpeg and Avalonia\nAssembled by brealin",
                ["Picker.VideoFiles"] = "视频文件",
                ["Picker.AllFiles"] = "所有文件",
                ["Picker.Executable"] = "可执行文件",
                ["Picker.Archive"] = "压缩包文件",
                ["Picker.SelectVideo"] = "选择视频文件",
                ["Picker.SelectFolder"] = "选择文件夹",
                ["Picker.SelectOutput"] = "选择输出文件夹",
                ["Picker.SelectFFmpeg"] = "选择FFmpeg可执行文件",
                ["Picker.SelectArchive"] = "选择FFmpeg压缩包",
                ["Setup.Title"] = "FFmpeg 引导设置",
                ["Setup.Language"] = "语言",
                ["Setup.Header"] = "初始化 FFmpeg",
                ["Setup.Subtitle"] = "为了正常处理视频，请选择一种方式配置核心组件",
                ["Setup.Manual"] = "手动指定路径",
                ["Setup.ManualDesc"] = "直接指向现有的 ffmpeg.exe",
                ["Setup.SelectFileWatermark"] = "请选择文件...",
                ["Setup.Browse"] = "浏览",
                ["Setup.ApplyPath"] = "应用此路径",
                ["Setup.Archive"] = "从压缩包安装",
                ["Setup.ArchiveDesc"] = "自动解压并配置环境",
                ["Setup.ArchiveWatermark"] = "请选择 .zip 或 .7z 压缩包...",
                ["Setup.InstallNow"] = "立即解压并安装",
                ["Setup.HowToGet"] = "如何获取 FFmpeg？",
                ["Setup.Visit"] = "访问 ",
                ["Setup.DownloadHint"] = " 下载 Windows builds 静态版本 (Static)。",
                ["Setup.Recommend"] = "推荐直接下载压缩包并使用上方“从压缩包安装”功能。",
                ["Setup.Footer"] = "稍后可在菜单中修改",
                ["Setup.Skip"] = "跳过",
                ["Setup.Confirm"] = "完成配置",
                ["Setup.SelectFFmpegFirst"] = "请先选择FFmpeg可执行文件路径",
                ["Setup.SelectArchiveFirst"] = "请先选择FFmpeg压缩包",
                ["Setup.SelectPathOrSkip"] = "请先选择FFmpeg路径或压缩包，或点击跳过继续使用程序",
                ["Setup.FileMissing"] = "指定的文件不存在",
                ["Setup.ArchiveMissing"] = "指定的压缩包文件不存在",
                ["Setup.Validating"] = "验证FFmpeg路径...",
                ["Setup.Installing"] = "正在安装FFmpeg...",
                ["Setup.PathSuccess"] = "FFmpeg路径设置成功！",
                ["Setup.InvalidFFmpeg"] = "指定的文件不是有效的FFmpeg可执行文件",
                ["Setup.PathError"] = "设置FFmpeg路径时出错: {0}",
                ["Setup.InstallSuccess"] = "FFmpeg安装成功！",
                ["Setup.InstallFailed"] = "FFmpeg安装失败",
                ["Setup.InstallError"] = "安装FFmpeg时出错: {0}",
                ["FFmpeg.NotFoundInArchive"] = "在压缩包中未找到ffmpeg可执行文件",
                ["FFmpeg.InstallFailed"] = "安装FFmpeg失败: {0}",
                ["FFmpeg.NotInstalled"] = "FFmpeg未安装",
                ["FFmpeg.VersionUnavailable"] = "无法获取版本信息",
                ["FFmpeg.NotConfigured"] = "FFmpeg未配置或不可用",
                ["FFmpeg.Unsupported7z"] = "暂不支持7z格式，请使用zip格式的压缩包",
                ["FFmpeg.WindowsUnsupportedArchive"] = "Windows系统暂不支持{0}格式",
                ["FFmpeg.UnsupportedArchive"] = "不支持的压缩包格式: {0}",
                ["FFmpeg.TarMissing"] = "当前系统没有可用的 tar/bsdtar，无法解压该格式压缩包。请安装 7-Zip，或改用 .zip 压缩包。",
                ["FFmpeg.ExtractFailed"] = "解压压缩包失败: {0}",
                ["FFmpeg.ExtractTarFailed"] = "解压tar文件失败"
            },
            ["en-US"] = new Dictionary<string, string>
            {
                ["App.Title"] = "FFmpeg Video Compressor",
                ["App.Title.Ready"] = "FFmpeg Video Compressor - FFmpeg Ready",
                ["App.Title.NotConfigured"] = "FFmpeg Video Compressor - FFmpeg Not Configured",
                ["Menu.File"] = "File (_F)",
                ["Menu.Exit"] = "Exit (_X)",
                ["Menu.Tools"] = "Tools (_T)",
                ["Menu.FFmpegSettings"] = "FFmpeg Settings (_S)",
                ["Menu.RedetectFFmpeg"] = "Redetect FFmpeg (_R)",
                ["Menu.Language"] = "Language (_L)",
                ["Menu.Language.Chinese"] = "中文",
                ["Menu.Language.English"] = "English",
                ["Menu.Help"] = "Help (_H)",
                ["Menu.About"] = "About (_A)",
                ["Theme.Toggle"] = "Toggle theme",
                ["Main.InputSource"] = "Input Source",
                ["Main.InputWatermark"] = "Drop a file or click a button...",
                ["Main.SelectFile"] = "Select File",
                ["Main.SelectFolder"] = "Select Folder",
                ["Main.SourceInfo"] = "Source Info",
                ["Main.Size"] = "Size",
                ["Main.Duration"] = "Duration",
                ["Main.OriginalBitrate"] = "Original Bitrate",
                ["Main.CompressionParams"] = "Compression",
                ["Main.CompressionStrength"] = "Strength",
                ["Main.CompressionHint"] = "Manual input supports values from 1 to 100 with 1% steps",
                ["Main.TargetBitrate"] = "Target Bitrate",
                ["Main.CurrentSelection"] = "Selected: {0}",
                ["Main.BitrateWarning"] = "Warning: current bitrate is higher than the source video; the file may grow",
                ["Main.Codec"] = "Codec",
                ["Main.EstimatedResult"] = "Estimate",
                ["Main.OutputSettings"] = "Output",
                ["Main.OutputWatermark"] = "Defaults to the source file folder",
                ["Main.ChangeOutput"] = "Change Save Location",
                ["Main.CliPreview"] = "CLI COMMAND PREVIEW",
                ["Main.Copy"] = "Copy",
                ["Main.CopyCommand"] = "Copy command",
                ["Main.Processing"] = "Processing...",
                ["Main.StartConvert"] = "Start",
                ["Status.Detecting"] = " - Checking FFmpeg...",
                ["Status.Redetecting"] = " - Redetecting...",
                ["Status.Checking"] = " - Checking FFmpeg...",
                ["Status.Ready"] = " - FFmpeg ready",
                ["Status.NotConfigured"] = " - FFmpeg not configured",
                ["Command.SelectInput"] = "Select an input file or folder first",
                ["Estimate.SelectVideo"] = "Select a video file first",
                ["Estimate.Analyzing"] = "Analyzing video...",
                ["Estimate.NonVideo"] = "Not a video file",
                ["Estimate.Calculating"] = "Calculating...",
                ["Estimate.Current"] = "{0}k (est: {1:F1}MB, {2}: {3:F1}%)",
                ["Estimate.Increase"] = "increase",
                ["Estimate.Compress"] = "saved",
                ["Codec.H264.Desc"] = "Best compatibility",
                ["Codec.H265.Desc"] = "Higher compression",
                ["Codec.VP9.Desc"] = "Open codec",
                ["Dialog.Ok"] = "OK",
                ["Dialog.Cancel"] = "Cancel",
                ["Dialog.Done"] = "Done",
                ["Dialog.Error"] = "Error",
                ["Dialog.Success"] = "Success",
                ["Dialog.Warning"] = "Warning",
                ["Dialog.Info"] = "Info",
                ["Dialog.DetectComplete"] = "Detection Complete",
                ["Dialog.AboutTitle"] = "About FFGUITool",
                ["Dialog.VideoComplete"] = "Video processing is complete.",
                ["Dialog.FFmpegConfigUpdated"] = "FFmpeg configuration has been updated.",
                ["Dialog.FFmpegDetected"] = "FFmpeg detected successfully.",
                ["Dialog.FFmpegMissing"] = "FFmpeg was not found. Configure it manually from the menu.",
                ["Dialog.FFmpegWarn"] = "FFmpeg is not configured correctly, so some features may not work.\nYou can configure it again from the menu.",
                ["Dialog.ExecuteError"] = "An error occurred while running FFmpeg:\n{0}",
                ["Dialog.AboutMessage"] = "FFGUITool v{0}\nFFmpeg video compressor\n\nFFmpeg version: {1}\n\n© 2025 FFGUITool\nPowered by FFmpeg and Avalonia\nAssembled by brealin",
                ["Picker.VideoFiles"] = "Video files",
                ["Picker.AllFiles"] = "All files",
                ["Picker.Executable"] = "Executable files",
                ["Picker.Archive"] = "Archive files",
                ["Picker.SelectVideo"] = "Select video file",
                ["Picker.SelectFolder"] = "Select folder",
                ["Picker.SelectOutput"] = "Select output folder",
                ["Picker.SelectFFmpeg"] = "Select FFmpeg executable",
                ["Picker.SelectArchive"] = "Select FFmpeg archive",
                ["Setup.Title"] = "FFmpeg Setup",
                ["Setup.Language"] = "Language",
                ["Setup.Header"] = "Initialize FFmpeg",
                ["Setup.Subtitle"] = "Choose a method to configure the core component",
                ["Setup.Manual"] = "Manual Path",
                ["Setup.ManualDesc"] = "Point directly to an existing ffmpeg.exe",
                ["Setup.SelectFileWatermark"] = "Select a file...",
                ["Setup.Browse"] = "Browse",
                ["Setup.ApplyPath"] = "Use This Path",
                ["Setup.Archive"] = "Install from Archive",
                ["Setup.ArchiveDesc"] = "Extract and configure automatically",
                ["Setup.ArchiveWatermark"] = "Select a .zip or .7z archive...",
                ["Setup.InstallNow"] = "Extract and Install",
                ["Setup.HowToGet"] = "How to get FFmpeg",
                ["Setup.Visit"] = "Visit ",
                ["Setup.DownloadHint"] = " and download a Windows static build.",
                ["Setup.Recommend"] = "Direct archive download is recommended, then use Install from Archive above.",
                ["Setup.Footer"] = "You can change this later from the menu",
                ["Setup.Skip"] = "Skip",
                ["Setup.Confirm"] = "Finish Setup",
                ["Setup.SelectFFmpegFirst"] = "Select the FFmpeg executable path first",
                ["Setup.SelectArchiveFirst"] = "Select an FFmpeg archive first",
                ["Setup.SelectPathOrSkip"] = "Select an FFmpeg path or archive first, or click Skip to continue",
                ["Setup.FileMissing"] = "The specified file does not exist",
                ["Setup.ArchiveMissing"] = "The specified archive does not exist",
                ["Setup.Validating"] = "Validating FFmpeg path...",
                ["Setup.Installing"] = "Installing FFmpeg...",
                ["Setup.PathSuccess"] = "FFmpeg path was set successfully.",
                ["Setup.InvalidFFmpeg"] = "The selected file is not a valid FFmpeg executable",
                ["Setup.PathError"] = "Error while setting FFmpeg path: {0}",
                ["Setup.InstallSuccess"] = "FFmpeg installed successfully.",
                ["Setup.InstallFailed"] = "FFmpeg installation failed",
                ["Setup.InstallError"] = "Error while installing FFmpeg: {0}",
                ["FFmpeg.NotFoundInArchive"] = "No ffmpeg executable was found in the archive",
                ["FFmpeg.InstallFailed"] = "FFmpeg installation failed: {0}",
                ["FFmpeg.NotInstalled"] = "FFmpeg is not installed",
                ["FFmpeg.VersionUnavailable"] = "Unable to get version information",
                ["FFmpeg.NotConfigured"] = "FFmpeg is not configured or unavailable",
                ["FFmpeg.Unsupported7z"] = "7z archives are not supported here. Please use a zip archive.",
                ["FFmpeg.WindowsUnsupportedArchive"] = "Windows does not support {0} archives here",
                ["FFmpeg.UnsupportedArchive"] = "Unsupported archive format: {0}",
                ["FFmpeg.TarMissing"] = "No usable tar/bsdtar was found on this system, so this archive cannot be extracted. Install 7-Zip or use a .zip archive.",
                ["FFmpeg.ExtractFailed"] = "Archive extraction failed: {0}",
                ["FFmpeg.ExtractTarFailed"] = "Tar extraction failed"
            }
        };

        public static IReadOnlyList<LanguageOption> AvailableLanguages { get; } = new[]
        {
            new LanguageOption("zh-CN", "中文"),
            new LanguageOption("en-US", "English")
        };

        public static string CurrentLanguage { get; private set; } = DefaultLanguage;

        public static event EventHandler? LanguageChanged;

        public static void SetLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode) || !Resources.ContainsKey(languageCode))
            {
                languageCode = DefaultLanguage;
            }

            if (CurrentLanguage == languageCode)
            {
                ApplyResources();
                return;
            }

            CurrentLanguage = languageCode;
            ApplyResources();
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string T(string key)
        {
            if (Resources.TryGetValue(CurrentLanguage, out var language) &&
                language.TryGetValue(key, out var value))
            {
                return value;
            }

            return Resources[DefaultLanguage].TryGetValue(key, out var fallback) ? fallback : key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        public static void ApplyResources()
        {
            if (Application.Current == null)
            {
                return;
            }

            foreach (var pair in Resources[CurrentLanguage])
            {
                Application.Current.Resources[pair.Key] = pair.Value;
            }
        }
    }
}
