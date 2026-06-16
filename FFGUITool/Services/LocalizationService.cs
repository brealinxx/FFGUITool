using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
                ["App.Title"] = "FFGUITool",
                ["App.Title.Ready"] = "FFGUITool - FFmpeg已就绪",
                ["App.Title.NotConfigured"] = "FFGUITool - FFmpeg未配置",
                ["Menu.File"] = "文件(_F)",
                ["Menu.Exit"] = "退出(_X)",
                ["Menu.Tools"] = "工具(_T)",
                ["Menu.Preferences"] = "偏好设置(_P)",
                ["Menu.FFmpegSettings"] = "FFmpeg设置(_S)",
                ["Menu.ExifToolSettings"] = "ExifTool设置(_E)",
                ["Menu.RedetectFFmpeg"] = "重新检测(_R)",
                ["Menu.ConfigCheck"] = "配置检查(_C)",
                ["Menu.OpenLogFolder"] = "打开日志目录(_L)",
                ["Menu.CheckUpdates"] = "检查更新(_U)",
                ["Menu.GitHubReleases"] = "GitHub Releases(_G)",
                ["Menu.Language"] = "语言(_L)",
                ["Menu.Language.Chinese"] = "中文",
                ["Menu.Language.English"] = "English",
                ["Menu.Help"] = "帮助(_H)",
                ["Menu.About"] = "关于(_A)",
                ["Menu.Theme"] = "主题(_T)",
                ["Menu.OpenConfigFolder"] = "打开本地配置目录(_O)",
                ["Menu.CleanupLocalData"] = "删除本地配置和注册表信息(_C)",
                ["Theme.Toggle"] = "切换主题",
                ["Theme.System"] = "跟随系统",
                ["Theme.Light"] = "浅色",
                ["Theme.Dark"] = "深色",
                ["Main.InputSource"] = "输入源",
                ["Main.InputWatermark"] = "拖入文件或点击右侧按钮...",
                ["Main.SelectFile"] = "选择文件",
                ["Main.SelectFolder"] = "选择文件夹",
                ["Main.SourceInfo"] = "源文件信息",
                ["Main.SourceDetails"] = "详细信息",
                ["SourceDetails.Title"] = "源文件详细信息",
                ["SourceDetails.Basic"] = "基础信息",
                ["SourceDetails.Metadata"] = "完整元数据",
                ["SourceDetails.NoExtra"] = "未读取到更多元数据。配置 ExifTool 后可以看到更完整的照片/视频信息。",
                ["SourceDetails.ConfigureToolsFirst"] = "请先配置 FFmpeg 或 ExifTool。",
                ["Estimate.ConfigureToolsFirst"] = "请先配置 FFmpeg 或 ExifTool",
                ["Main.Size"] = "大小",
                ["Main.Duration"] = "时长",
                ["Main.OriginalBitrate"] = "原比特率",
                ["Main.CompressionParams"] = "压缩参数",
                ["Main.CompressionStrength"] = "压缩强度",
                ["Main.CompressionHint"] = "支持手动输入 1-100 之间的数值，步进精度为 1%",
                ["Main.TargetSize"] = "目标大小",
                ["Main.CompressionPreset"] = "预设模式",
                ["Main.AdvancedMode"] = "高级模式",
                ["Main.ConversionTools"] = "转换工具",
                ["Main.FormatConversion"] = "格式转换",
                ["Main.VideoFormat"] = "视频格式",
                ["Main.AudioConversion"] = "音频转换",
                ["Main.AudioFormat"] = "音频格式",
                ["Main.AudioBitrate"] = "音频码率",
                ["Main.AudioTrackMode"] = "音轨处理",
                ["Main.TrimSegment"] = "截取片段",
                ["Main.TrimStart"] = "开始时间",
                ["Main.TrimEnd"] = "结束时间",
                ["Main.TrimWatermark"] = "如 00:00:05 或 5",
                ["Main.ResolutionConversion"] = "分辨率转换",
                ["Main.TargetResolution"] = "目标分辨率",
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
                ["Main.Privacy"] = "隐私",
                ["Metadata.Clear"] = "清除元数据",
                ["Metadata.ClearPreview"] = "将清除的敏感信息",
                ["ExifTool.Ready"] = "ExifTool已就绪，将在输出后清除元数据",
                ["ExifTool.NotConfigured"] = "未检测到ExifTool，无法清除元数据",
                ["ExifTool.PathSuccess"] = "ExifTool路径设置成功。",
                ["ExifTool.Invalid"] = "指定的文件不是有效的ExifTool可执行文件。",
                ["ExifTool.SetupTitle"] = "ExifTool隐私清理（可选）",
                ["ExifTool.SetupDesc"] = "用于读取并清除照片/视频中的GPS、设备、镜头、作者等元数据；不配置也可正常压缩。",
                ["ExifTool.DetectSystem"] = "从系统命令检测 exiftool",
                ["ExifTool.SelectExecutableWatermark"] = "请选择 exiftool.exe 或 exiftool(-k).exe...",
                ["ExifTool.SelectFolderWatermark"] = "请选择包含 ExifTool 的文件夹...",
                ["ExifTool.ArchiveWatermark"] = "请选择 ExifTool .zip 压缩包...",
                ["ExifTool.ApplyExecutable"] = "应用此 ExifTool 程序",
                ["ExifTool.ApplyFolder"] = "从文件夹查找并应用",
                ["ExifTool.InstallArchive"] = "从压缩包安装 ExifTool",
                ["ExifTool.Validating"] = "正在验证ExifTool...",
                ["ExifTool.SystemMissing"] = "系统命令中未检测到ExifTool。",
                ["ExifTool.OptionalNotConfigured"] = "ExifTool未配置；这是可选项，仅影响元数据读取和清除。",
                ["ExifTool.SelectExecutableFirst"] = "请先选择ExifTool可执行文件。",
                ["ExifTool.SelectFolderFirst"] = "请先选择ExifTool文件夹。",
                ["ExifTool.SelectArchiveFirst"] = "请先选择ExifTool压缩包。",
                ["ExifTool.FolderMissing"] = "指定的ExifTool文件夹不存在。",
                ["ExifTool.InvalidFolder"] = "未在该文件夹中找到有效的ExifTool。",
                ["ExifTool.ArchiveMissing"] = "指定的ExifTool压缩包不存在。",
                ["ExifTool.Installing"] = "正在安装ExifTool...",
                ["ExifTool.InstallSuccess"] = "ExifTool安装成功。",
                ["ExifTool.InstallFailed"] = "ExifTool安装失败，请确认压缩包中包含exiftool.exe。",
                ["ExifTool.InstallError"] = "安装ExifTool时出错: {0}",
                ["Mode.Choose"] = "选择处理模式",
                ["Mode.Video"] = "视频处理模式",
                ["Mode.VideoDesc"] = "压缩、转码、提取音频",
                ["Mode.Image"] = "图片处理模式",
                ["Mode.ImageDesc"] = "压缩、格式转换、尺寸调整",
                ["Mode.Switch"] = "切换模式",
                ["Image.InputSource"] = "图片来源",
                ["Image.SelectFile"] = "选择图片",
                ["Image.SelectFolder"] = "选择图片文件夹",
                ["Image.FileType"] = "图片文件",
                ["Image.SourceInfo"] = "图片信息",
                ["Image.Format"] = "格式",
                ["Image.Resolution"] = "分辨率",
                ["Image.CompressionParams"] = "图片压缩参数",
                ["Image.TargetSize"] = "目标大小",
                ["Image.FormatLabel"] = "图片格式",
                ["Image.IconSizes"] = "ICO尺寸",
                ["Image.NoSupportedFiles"] = "未找到支持的图片",
                ["Image.BatchMode"] = "图片批量处理：{0} 个文件",
                ["Image.ConversionHint"] = "可压缩图片、转换格式并按预设调整尺寸",
                ["Image.CompressHint"] = "按目标大小压缩图片",
                ["Image.Estimate"] = "目标约 {0}，输出 {1}{2}",
                ["Image.EstimateSizeSuffix"] = "，尺寸 {0}",
                ["Result.ImageComplete"] = "图片处理完成",
                ["Result.VideoComplete"] = "转换完成",
                ["Result.BatchComplete"] = "批量转换完成：{0} 个文件",
                ["Result.Elapsed"] = "用时：{0}",
                ["Result.Output"] = "输出：{0}",
                ["Result.SizeCompare"] = "大小：{0} -> {1} ({2})",
                ["Result.ImageFormat"] = "格式：{0} -> {1}",
                ["Result.AudioFormat"] = "音频格式：{0} -> {1}",
                ["Result.AudioBitrate"] = "音频码率：{0}",
                ["Result.Resolution"] = "分辨率：{0} -> {1}",
                ["Result.BitrateCompare"] = "比特率：{0} -> {1}",
                ["Result.Unknown"] = "未知",
                ["Result.ChangeUnavailable"] = "无法计算变化",
                ["Result.Reduced"] = "减少 {0:F1}%",
                ["Result.Increased"] = "增加 {0:F1}%",
                ["Result.OutputSize"] = "输出大小：{0}",
                ["Result.FFmpegInfo"] = "FFmpeg 信息：",
                ["Status.Detecting"] = " - FFmpeg状态检测中...",
                ["Status.Redetecting"] = " - 重新检测中...",
                ["Status.Checking"] = " - 检测FFmpeg中...",
                ["Status.Ready"] = " - FFmpeg已就绪",
                ["Status.NotConfigured"] = " - FFmpeg未配置",
                ["Command.SelectInput"] = "请先选择输入文件或文件夹",
                ["Estimate.SelectVideo"] = "请先选择视频文件",
                ["Estimate.Analyzing"] = "分析视频中...",
                ["Estimate.NonVideo"] = "非视频文件",
                ["Estimate.AudioFile"] = "已选择音频文件，可开启音频转换",
                ["Estimate.Calculating"] = "计算中...",
                ["Estimate.Current"] = "{0}k (预估: {1:F1}MB, {2}: {3:F1}%)",
                ["Estimate.Crf"] = "CRF {0} 模式（音频 {1}k），优先控制画质，输出大小会随内容复杂度变化",
                ["Estimate.Increase"] = "增大",
                ["Estimate.Compress"] = "压缩",
                ["Batch.Empty"] = "此文件夹中没有可处理的媒体文件",
                ["Batch.Found"] = "批量模式：找到 {0} 个可处理文件",
                ["Batch.Mode"] = "批量处理 {0} 个文件。当前转换规则会统一应用到这些文件。",
                ["Batch.Preview"] = "批量模式将处理 {0} 个文件。下面显示第一条示例命令：",
                ["Conversion.HelpTitle"] = "转换工具说明",
                ["Conversion.HelpMessage"] = "格式转换：用于视频容器/格式转换，支持 MP4、MKV、WebM、MOV、AVI，也支持视频转 GIF。\n\n音频转换：独占模式。开启后会自动关闭视频格式转换和分辨率转换，并使用 -vn 只输出音频；视频文件也可以用它提取音频。\n\n分辨率转换：只作用于视频，可与格式转换同时使用。它会按所选高度等比缩放，不会放大超过原始画面。\n\n文件夹批量模式：选择文件夹后会扫描当前文件夹内的媒体文件，并把同一套规则逐个应用。CLI 预览只显示第一条示例命令。",
                ["Conversion.AudioExclusiveHint"] = "音频转换为独占模式，会自动关闭视频格式和分辨率转换。",
                ["Conversion.GifHint"] = "GIF 输出会自动移除音频，可与分辨率转换一起使用。",
                ["Conversion.VideoToolsHint"] = "视频格式和分辨率转换可同时使用；音频转换会单独处理。",
                ["Preset.HelpTitle"] = "预设模式说明",
                ["Preset.HelpMessage"] = "无：不套用预设，按目标大小手动压缩。\n\n微信 / QQ 发送：MP4 + H.264，分辨率限制到 720p，帧率限制到 30fps，视频码率约 800k-1500k，音频 AAC 96k。目标是能发出去、能自动播放，并减少平台二次压缩。\n\n邮箱附件：MP4 + H.264，默认目标 25MB，分辨率限制到 720p，音频 AAC 64k。目标是压进常见 20MB / 25MB 附件限制，目标大小仍可手动改。\n\n网页上传：MP4 + H.264，保持原分辨率和帧率，CRF 23，音频 AAC 128k。适合上传平台，保持清晰度。\n\n极限压缩：H.265，分辨率限制到 480p，帧率限制到 24fps，CRF 30，音频 AAC 48k。最大限度压缩体积，画质较低。",
                ["Codec.H264.Desc"] = "兼容性最好",
                ["Codec.H265.Desc"] = "压缩率更高",
                ["Codec.VP9.Desc"] = "开源编码",
                ["Codec.AV1.Desc"] = "新一代高压缩",
                ["AudioBitrate.320.Desc"] = "高音质",
                ["AudioBitrate.256.Desc"] = "清晰均衡",
                ["AudioBitrate.128.Desc"] = "常用体积",
                ["AudioBitrate.96.Desc"] = "更小体积",
                ["AudioBitrate.64.Desc"] = "语音/低码率",
                ["AudioBitrate.8.Desc"] = "极低码率",
                ["VideoFormat.MP4.Desc"] = "兼容性最好",
                ["VideoFormat.MKV.Desc"] = "多轨封装",
                ["VideoFormat.WebM.Desc"] = "网页友好",
                ["VideoFormat.MOV.Desc"] = "Apple/剪辑软件",
                ["VideoFormat.AVI.Desc"] = "旧设备兼容",
                ["VideoFormat.GIF.Desc"] = "视频转动图",
                ["AudioFormat.MP3.Desc"] = "通用音频",
                ["AudioFormat.AAC.Desc"] = "体积小",
                ["AudioFormat.M4A.Desc"] = "Apple/移动设备",
                ["AudioFormat.WAV.Desc"] = "无压缩",
                ["AudioFormat.FLAC.Desc"] = "无损压缩",
                ["AudioFormat.OGG.Desc"] = "开源音频",
                ["Resolution.Original"] = "原尺寸",
                ["Resolution.Original.Desc"] = "不调整尺寸",
                ["Resolution.1080.Desc"] = "全高清",
                ["Resolution.720.Desc"] = "高清",
                ["Resolution.480.Desc"] = "小体积",
                ["Resolution.512.Desc"] = "头像",
                ["Resolution.360.Desc"] = "极小体积",
                ["ImageFormat.JPG.Desc"] = "通用照片格式",
                ["ImageFormat.PNG.Desc"] = "透明与无损场景",
                ["ImageFormat.WebP.Desc"] = "网页体积更小",
                ["Cleanup.Title"] = "删除本地数据",
                ["Cleanup.ConfirmMessage"] = "将删除本机配置文件、日志文件、工具路径记录、已安装的内置工具和注册表信息。\n\n配置目录：{0}\n\n此操作不会删除当前程序文件。确定继续吗？",
                ["Cleanup.Done"] = "本地配置、日志文件和注册表信息已清理。",
                ["Failure.CopyErrorDetails"] = "复制错误详情",
                ["Failure.CopyFullCommand"] = "复制完整命令",
                ["Failure.OpenLogs"] = "打开日志目录",
                ["Update.Title"] = "检查更新",
                ["Update.NewVersion"] = "发现新版本：v{0}\n{1}",
                ["Update.Latest"] = "当前已是最新版本：v{0}\n{1}",
                ["Update.Unavailable"] = "暂时无法检查更新。\n{0}",
                ["Update.Releases"] = "GitHub Releases：{0}",
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
                ["Dialog.RedetectToolsResult"] = "FFmpeg：{0}\nExifTool：{1}",
                ["Dialog.ConfigCheckTitle"] = "配置检查",
                ["Dialog.FFmpegWarn"] = "FFmpeg未正确配置，某些功能可能无法使用。\n您可以通过菜单重新配置。",
                ["Dialog.ExecuteError"] = "执行FFmpeg命令时出错:\n{0}",
                ["Dialog.AboutMessage"] = "FFGUITool v{0}\n\nFFmpeg版本: {1}\nExifTool版本: {2}\n\n© 2025 FFGUITool\nPowered by FFmpeg & ExifTool and Avalonia\nAssembled by brealin",
                ["ExifTool.NotInstalled"] = "ExifTool未安装",
                ["ExifTool.VersionUnavailable"] = "无法获取版本信息",
                ["Picker.VideoFiles"] = "视频文件",
                ["Picker.AudioFiles"] = "音频文件",
                ["Picker.AllFiles"] = "所有文件",
                ["Picker.Executable"] = "可执行文件",
                ["Picker.Archive"] = "压缩包文件",
                ["Picker.SelectVideo"] = "选择视频文件",
                ["Picker.SelectFolder"] = "选择文件夹",
                ["Picker.SelectOutput"] = "选择输出文件夹",
                ["Picker.SelectFFmpeg"] = "选择FFmpeg可执行文件",
                ["Picker.SelectExifTool"] = "选择ExifTool可执行文件",
                ["Picker.SelectExifToolFolder"] = "选择ExifTool文件夹",
                ["Picker.SelectExifToolArchive"] = "选择ExifTool压缩包",
                ["Picker.SelectArchive"] = "选择FFmpeg压缩包",
                ["Setup.Title"] = "FFmpeg 引导设置",
                ["Setup.Language"] = "语言",
                ["Setup.Header"] = "初始化 FFmpeg",
                ["Setup.Subtitle"] = "FFmpeg是核心组件；ExifTool为可选隐私清理组件",
                ["Setup.FFmpegTab"] = "FFmpeg",
                ["Setup.ExifToolTab"] = "ExifTool（可选）",
                ["Setup.DetectSystemFFmpeg"] = "从系统命令检测 ffmpeg",
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
                ["Setup.HowToGetExifTool"] = "如何获取 ExifTool？",
                ["Setup.Visit"] = "访问 ",
                ["Setup.DownloadHint"] = " 下载 Windows builds 静态版本 (Static)。",
                ["Setup.Recommend"] = "推荐直接下载压缩包并使用上方“从压缩包安装”功能。",
                ["Setup.ExifToolDownloadHint"] = "下载 Windows Executable，解压后可选择 exiftool(-k).exe、文件夹或压缩包。",
                ["Setup.OpenLink"] = "打开链接",
                ["Setup.Link"] = "链接",
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
                ["Setup.SystemFFmpegMissing"] = "系统命令中未检测到FFmpeg。",
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
                ["App.Title"] = "FFGUITool",
                ["App.Title.Ready"] = "FFGUITool - FFmpeg Ready",
                ["App.Title.NotConfigured"] = "FFGUITool - FFmpeg Not Configured",
                ["Menu.File"] = "File (_F)",
                ["Menu.Exit"] = "Exit (_X)",
                ["Menu.Tools"] = "Tools (_T)",
                ["Menu.Preferences"] = "Preferences (_P)",
                ["Menu.FFmpegSettings"] = "FFmpeg Settings (_S)",
                ["Menu.ExifToolSettings"] = "ExifTool Settings (_E)",
                ["Menu.RedetectFFmpeg"] = "Redetect (_R)",
                ["Menu.ConfigCheck"] = "Config Check (_C)",
                ["Menu.OpenLogFolder"] = "Open log folder (_L)",
                ["Menu.CheckUpdates"] = "Check for updates (_U)",
                ["Menu.GitHubReleases"] = "GitHub Releases (_G)",
                ["Menu.Language"] = "Language (_L)",
                ["Menu.Language.Chinese"] = "中文",
                ["Menu.Language.English"] = "English",
                ["Menu.Help"] = "Help (_H)",
                ["Menu.About"] = "About (_A)",
                ["Menu.Theme"] = "Theme (_T)",
                ["Menu.OpenConfigFolder"] = "Open local config folder (_O)",
                ["Menu.CleanupLocalData"] = "Delete local config and registry (_C)",
                ["Theme.Toggle"] = "Toggle theme",
                ["Theme.System"] = "Follow system",
                ["Theme.Light"] = "Light",
                ["Theme.Dark"] = "Dark",
                ["Main.InputSource"] = "Input Source",
                ["Main.InputWatermark"] = "Drop a file or click a button...",
                ["Main.SelectFile"] = "Select File",
                ["Main.SelectFolder"] = "Select Folder",
                ["Main.SourceInfo"] = "Source Info",
                ["Main.SourceDetails"] = "Details",
                ["SourceDetails.Title"] = "Source Details",
                ["SourceDetails.Basic"] = "Basic info",
                ["SourceDetails.Metadata"] = "Full metadata",
                ["SourceDetails.NoExtra"] = "No additional metadata was read. Configure ExifTool to see fuller photo/video details.",
                ["SourceDetails.ConfigureToolsFirst"] = "Please configure FFmpeg or ExifTool first.",
                ["Estimate.ConfigureToolsFirst"] = "Please configure FFmpeg or ExifTool first",
                ["Main.Size"] = "Size",
                ["Main.Duration"] = "Duration",
                ["Main.OriginalBitrate"] = "Original Bitrate",
                ["Main.CompressionParams"] = "Compression",
                ["Main.CompressionStrength"] = "Strength",
                ["Main.CompressionHint"] = "Manual input supports values from 1 to 100 with 1% steps",
                ["Main.TargetSize"] = "Target Size",
                ["Main.CompressionPreset"] = "Preset",
                ["Main.AdvancedMode"] = "Advanced mode",
                ["Main.ConversionTools"] = "Conversion tools",
                ["Main.FormatConversion"] = "Format conversion",
                ["Main.VideoFormat"] = "Video format",
                ["Main.AudioConversion"] = "Audio conversion",
                ["Main.AudioFormat"] = "Audio format",
                ["Main.AudioBitrate"] = "Audio bitrate",
                ["Main.AudioTrackMode"] = "Audio track",
                ["Main.TrimSegment"] = "Trim segment",
                ["Main.TrimStart"] = "Start time",
                ["Main.TrimEnd"] = "End time",
                ["Main.TrimWatermark"] = "e.g. 00:00:05 or 5",
                ["Main.ResolutionConversion"] = "Resolution conversion",
                ["Main.TargetResolution"] = "Target resolution",
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
                ["Main.Privacy"] = "Privacy",
                ["Metadata.Clear"] = "Clear metadata",
                ["Metadata.ClearPreview"] = "Sensitive metadata to remove",
                ["ExifTool.Ready"] = "ExifTool is ready. Metadata will be removed after output is created.",
                ["ExifTool.NotConfigured"] = "ExifTool was not detected. Metadata removal is unavailable.",
                ["ExifTool.PathSuccess"] = "ExifTool path configured successfully.",
                ["ExifTool.Invalid"] = "The selected file is not a valid ExifTool executable.",
                ["ExifTool.SetupTitle"] = "ExifTool privacy cleanup (optional)",
                ["ExifTool.SetupDesc"] = "Reads and removes GPS, device, lens, author, and other photo/video metadata. Compression works without it.",
                ["ExifTool.DetectSystem"] = "Detect exiftool from system command",
                ["ExifTool.SelectExecutableWatermark"] = "Select exiftool.exe or exiftool(-k).exe...",
                ["ExifTool.SelectFolderWatermark"] = "Select a folder containing ExifTool...",
                ["ExifTool.ArchiveWatermark"] = "Select an ExifTool .zip archive...",
                ["ExifTool.ApplyExecutable"] = "Use This ExifTool",
                ["ExifTool.ApplyFolder"] = "Find and Use from Folder",
                ["ExifTool.InstallArchive"] = "Install ExifTool from Archive",
                ["ExifTool.Validating"] = "Validating ExifTool...",
                ["ExifTool.SystemMissing"] = "ExifTool was not found in the system command path.",
                ["ExifTool.OptionalNotConfigured"] = "ExifTool is not configured. This is optional and only affects metadata reading and removal.",
                ["ExifTool.SelectExecutableFirst"] = "Select an ExifTool executable first.",
                ["ExifTool.SelectFolderFirst"] = "Select an ExifTool folder first.",
                ["ExifTool.SelectArchiveFirst"] = "Select an ExifTool archive first.",
                ["ExifTool.FolderMissing"] = "The specified ExifTool folder does not exist.",
                ["ExifTool.InvalidFolder"] = "No valid ExifTool executable was found in that folder.",
                ["ExifTool.ArchiveMissing"] = "The specified ExifTool archive does not exist.",
                ["ExifTool.Installing"] = "Installing ExifTool...",
                ["ExifTool.InstallSuccess"] = "ExifTool installed successfully.",
                ["ExifTool.InstallFailed"] = "ExifTool installation failed. Make sure the archive contains exiftool.exe.",
                ["ExifTool.InstallError"] = "Error while installing ExifTool: {0}",
                ["Mode.Choose"] = "Choose a processing mode",
                ["Mode.Video"] = "Video mode",
                ["Mode.VideoDesc"] = "Compress, transcode, and extract audio",
                ["Mode.Image"] = "Image mode",
                ["Mode.ImageDesc"] = "Compress, convert, and resize images",
                ["Mode.Switch"] = "Switch mode",
                ["Image.InputSource"] = "Image source",
                ["Image.SelectFile"] = "Select image",
                ["Image.SelectFolder"] = "Select image folder",
                ["Image.FileType"] = "Image files",
                ["Image.SourceInfo"] = "Image info",
                ["Image.Format"] = "Format",
                ["Image.Resolution"] = "Resolution",
                ["Image.CompressionParams"] = "Image compression",
                ["Image.TargetSize"] = "Target size",
                ["Image.FormatLabel"] = "Image format",
                ["Image.IconSizes"] = "ICO sizes",
                ["Image.NoSupportedFiles"] = "No supported images found",
                ["Image.BatchMode"] = "Batch image processing: {0} files",
                ["Image.ConversionHint"] = "Compress, convert format, and resize images",
                ["Image.CompressHint"] = "Compress images by target size",
                ["Image.Estimate"] = "Target about {0}, output {1}{2}",
                ["Image.EstimateSizeSuffix"] = ", size {0}",
                ["Result.ImageComplete"] = "Image processing complete",
                ["Result.VideoComplete"] = "Conversion complete",
                ["Result.BatchComplete"] = "Batch conversion complete: {0} files",
                ["Result.Elapsed"] = "Elapsed: {0}",
                ["Result.Output"] = "Output: {0}",
                ["Result.SizeCompare"] = "Size: {0} -> {1} ({2})",
                ["Result.ImageFormat"] = "Format: {0} -> {1}",
                ["Result.AudioFormat"] = "Audio format: {0} -> {1}",
                ["Result.AudioBitrate"] = "Audio bitrate: {0}",
                ["Result.Resolution"] = "Resolution: {0} -> {1}",
                ["Result.BitrateCompare"] = "Bitrate: {0} -> {1}",
                ["Result.Unknown"] = "Unknown",
                ["Result.ChangeUnavailable"] = "change unavailable",
                ["Result.Reduced"] = "reduced {0:F1}%",
                ["Result.Increased"] = "increased {0:F1}%",
                ["Result.OutputSize"] = "Output size: {0}",
                ["Result.FFmpegInfo"] = "FFmpeg info:",
                ["Status.Detecting"] = " - Checking FFmpeg...",
                ["Status.Redetecting"] = " - Redetecting...",
                ["Status.Checking"] = " - Checking FFmpeg...",
                ["Status.Ready"] = " - FFmpeg ready",
                ["Status.NotConfigured"] = " - FFmpeg not configured",
                ["Command.SelectInput"] = "Select an input file or folder first",
                ["Estimate.SelectVideo"] = "Select a video file first",
                ["Estimate.Analyzing"] = "Analyzing video...",
                ["Estimate.NonVideo"] = "Not a video file",
                ["Estimate.AudioFile"] = "Audio file selected. Enable audio conversion to convert it.",
                ["Estimate.Calculating"] = "Calculating...",
                ["Estimate.Current"] = "{0}k (est: {1:F1}MB, {2}: {3:F1}%)",
                ["Estimate.Crf"] = "CRF {0} mode (audio {1}k). Quality is prioritized; output size depends on video complexity.",
                ["Estimate.Increase"] = "increase",
                ["Estimate.Compress"] = "saved",
                ["Batch.Empty"] = "No supported media files were found in this folder",
                ["Batch.Found"] = "Batch mode: found {0} processable files",
                ["Batch.Mode"] = "Batch processing {0} files. The current settings will be applied to each file.",
                ["Batch.Preview"] = "Batch mode will process {0} files. First command preview:",
                ["Conversion.HelpTitle"] = "Conversion Tool Details",
                ["Conversion.HelpMessage"] = "Format conversion: changes video container/format. Supports MP4, MKV, WebM, MOV, AVI, and video to GIF.\n\nAudio conversion: exclusive mode. When enabled, video format and resolution conversion are turned off, and -vn is used to output audio only. It can also extract audio from video files.\n\nResolution conversion: video only. It can be combined with format conversion and scales proportionally by height without upscaling past the source frame.\n\nFolder batch mode: selecting a folder scans media files in that folder and applies the same settings to each one. The CLI preview shows only the first example command.",
                ["Conversion.AudioExclusiveHint"] = "Audio conversion is exclusive and turns off video format/resolution conversion.",
                ["Conversion.GifHint"] = "GIF output removes audio and can be combined with resolution conversion.",
                ["Conversion.VideoToolsHint"] = "Video format and resolution conversion can be combined; audio conversion runs separately.",
                ["Preset.HelpTitle"] = "Preset Details",
                ["Preset.HelpMessage"] = "None: no preset; compress manually by target size.\n\nWeChat / QQ: MP4 + H.264, max 720p, max 30fps, video bitrate about 800k-1500k, AAC 96k audio. Designed to send, autoplay, and avoid harsh second-pass compression.\n\nEmail attachment: MP4 + H.264, default 25MB target, max 720p, AAC 64k audio. Designed for common 20MB / 25MB attachment limits; target size remains editable.\n\nWeb upload: MP4 + H.264, keeps source resolution and framerate, CRF 23, AAC 128k audio. Clearer output for upload platforms.\n\nExtreme: H.265, max 480p, max 24fps, CRF 30, AAC 48k audio. Smallest practical file, lower quality.",
                ["Codec.H264.Desc"] = "Best compatibility",
                ["Codec.H265.Desc"] = "Higher compression",
                ["Codec.VP9.Desc"] = "Open codec",
                ["Codec.AV1.Desc"] = "Next-gen compression",
                ["AudioBitrate.320.Desc"] = "High quality",
                ["AudioBitrate.256.Desc"] = "Clear balance",
                ["AudioBitrate.128.Desc"] = "Common size",
                ["AudioBitrate.96.Desc"] = "Smaller files",
                ["AudioBitrate.64.Desc"] = "Voice / low bitrate",
                ["AudioBitrate.8.Desc"] = "Very low bitrate",
                ["VideoFormat.MP4.Desc"] = "Best compatibility",
                ["VideoFormat.MKV.Desc"] = "Multi-track container",
                ["VideoFormat.WebM.Desc"] = "Web friendly",
                ["VideoFormat.MOV.Desc"] = "Apple/editing apps",
                ["VideoFormat.AVI.Desc"] = "Legacy compatibility",
                ["VideoFormat.GIF.Desc"] = "Video to GIF",
                ["AudioFormat.MP3.Desc"] = "Universal audio",
                ["AudioFormat.AAC.Desc"] = "Small size",
                ["AudioFormat.M4A.Desc"] = "Apple/mobile devices",
                ["AudioFormat.WAV.Desc"] = "Uncompressed",
                ["AudioFormat.FLAC.Desc"] = "Lossless compression",
                ["AudioFormat.OGG.Desc"] = "Open audio",
                ["Resolution.Original"] = "Original",
                ["Resolution.Original.Desc"] = "No resizing",
                ["Resolution.1080.Desc"] = "Full HD",
                ["Resolution.720.Desc"] = "HD",
                ["Resolution.480.Desc"] = "Small size",
                ["Resolution.512.Desc"] = "Avatar",
                ["Resolution.360.Desc"] = "Very small",
                ["ImageFormat.JPG.Desc"] = "Common photo format",
                ["ImageFormat.PNG.Desc"] = "Transparency/lossless",
                ["ImageFormat.WebP.Desc"] = "Smaller web images",
                ["Cleanup.Title"] = "Delete local data",
                ["Cleanup.ConfirmMessage"] = "This will delete local config files, logs, tool path records, bundled tools installed by the app, and registry information.\n\nConfig folder: {0}\n\nThis will not delete the current program files. Continue?",
                ["Cleanup.Done"] = "Local config files, logs, and registry information have been removed.",
                ["Failure.CopyErrorDetails"] = "Copy error details",
                ["Failure.CopyFullCommand"] = "Copy full command",
                ["Failure.OpenLogs"] = "Open logs",
                ["Update.Title"] = "Update Check",
                ["Update.NewVersion"] = "New version available: v{0}\n{1}",
                ["Update.Latest"] = "You are using the latest version: v{0}\n{1}",
                ["Update.Unavailable"] = "Unable to check updates now.\n{0}",
                ["Update.Releases"] = "GitHub Releases: {0}",
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
                ["Dialog.RedetectToolsResult"] = "FFmpeg: {0}\nExifTool: {1}",
                ["Dialog.ConfigCheckTitle"] = "Config Check",
                ["Dialog.FFmpegWarn"] = "FFmpeg is not configured correctly, so some features may not work.\nYou can configure it again from the menu.",
                ["Dialog.ExecuteError"] = "An error occurred while running FFmpeg:\n{0}",
                ["Dialog.AboutMessage"] = "FFGUITool v{0}\nFFmpeg video compressor\n\nFFmpeg version: {1}\nExifTool version: {2}\n\n© 2025 FFGUITool\nPowered by FFmpeg, ExifTool and Avalonia\nAssembled by brealin",
                ["ExifTool.NotInstalled"] = "ExifTool is not installed",
                ["ExifTool.VersionUnavailable"] = "Version unavailable",
                ["Picker.VideoFiles"] = "Video files",
                ["Picker.AudioFiles"] = "Audio files",
                ["Picker.AllFiles"] = "All files",
                ["Picker.Executable"] = "Executable files",
                ["Picker.Archive"] = "Archive files",
                ["Picker.SelectVideo"] = "Select video file",
                ["Picker.SelectFolder"] = "Select folder",
                ["Picker.SelectOutput"] = "Select output folder",
                ["Picker.SelectFFmpeg"] = "Select FFmpeg executable",
                ["Picker.SelectExifTool"] = "Select ExifTool executable",
                ["Picker.SelectExifToolFolder"] = "Select ExifTool folder",
                ["Picker.SelectExifToolArchive"] = "Select ExifTool archive",
                ["Picker.SelectArchive"] = "Select FFmpeg archive",
                ["Setup.Title"] = "FFmpeg Setup",
                ["Setup.Language"] = "Language",
                ["Setup.Header"] = "Initialize FFmpeg",
                ["Setup.Subtitle"] = "FFmpeg is required. ExifTool is optional for privacy cleanup.",
                ["Setup.FFmpegTab"] = "FFmpeg",
                ["Setup.ExifToolTab"] = "ExifTool (optional)",
                ["Setup.DetectSystemFFmpeg"] = "Detect ffmpeg from system command",
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
                ["Setup.HowToGetExifTool"] = "How to get ExifTool",
                ["Setup.Visit"] = "Visit ",
                ["Setup.DownloadHint"] = " and download a Windows static build.",
                ["Setup.Recommend"] = "Direct archive download is recommended, then use Install from Archive above.",
                ["Setup.ExifToolDownloadHint"] = "Download the Windows Executable, then choose exiftool(-k).exe, its folder, or the zip archive above.",
                ["Setup.OpenLink"] = "Open link",
                ["Setup.Link"] = "Link",
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
                ["Setup.SystemFFmpegMissing"] = "FFmpeg was not found in the system command path.",
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

        static LocalizationService()
        {
            LoadExternalResources();
        }

        public static void SetLanguage(string? languageCode, bool persistPreference = true)
        {
            if (string.IsNullOrWhiteSpace(languageCode) || !Resources.ContainsKey(languageCode))
            {
                languageCode = DefaultLanguage;
            }

            if (CurrentLanguage == languageCode)
            {
                if (persistPreference)
                {
                    SaveLanguagePreference(languageCode);
                }

                ApplyResources();
                LanguageChanged?.Invoke(null, EventArgs.Empty);
                return;
            }

            CurrentLanguage = languageCode;
            if (persistPreference)
            {
                SaveLanguagePreference(languageCode);
            }

            ApplyResources();
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void SaveLanguagePreference(string languageCode)
        {
            var config = AppConfigService.Load();
            config.Language = languageCode;
            AppConfigService.Save(config);
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

        private static void LoadExternalResources()
        {
            foreach (var directory in GetExternalResourceDirectories())
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var languageCode = Path.GetFileNameWithoutExtension(file);
                        if (!Resources.ContainsKey(languageCode))
                        {
                            Resources[languageCode] = new Dictionary<string, string>();
                        }

                        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
                        if (values == null)
                        {
                            continue;
                        }

                        foreach (var pair in values)
                        {
                            Resources[languageCode][pair.Key] = pair.Value;
                        }

                        AppLogger.Info($"Loaded localization overrides from {file}.");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warn($"Failed to load localization file '{file}': {ex.Message}");
                    }
                }
            }
        }

        private static IEnumerable<string> GetExternalResourceDirectories()
        {
            yield return Path.Combine(AppContext.BaseDirectory, "i18n");
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FFGUITool",
                "i18n");
        }
    }
}
