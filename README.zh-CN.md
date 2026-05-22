# FFGUITool

[English README](README.md)

FFGUITool 是一个基于 FFmpeg 的轻量跨平台桌面 GUI 工具。它可以压缩和转换视频、音频、图片，支持文件夹批量处理，能预览生成的 CLI 命令，让常见 FFmpeg 工作流不再需要手写命令。

## 下载

可以从 GitHub Releases 下载最新可直接使用的版本：

[GitHub Releases](https://github.com/brealinxx/FFGUITool/releases/latest)

## 主要功能

- 按目标大小、目标码率、预设或 CRF 质量模式压缩视频。
- 按目标大小或质量压缩图片，支持 KB/MB 目标大小控制。
- 视频、音频、图片格式转换。
- 文件夹批量处理，视频和图片批量模式支持按每个文件原始大小的比例压缩。
- 支持拖拽文件和文件夹。
- 图片输入支持 JPG、JPEG、PNG、WebP、HEIC、HEIF、BMP、GIF、TIFF、ICO、TGA、AVIF。
- 图片输出支持 JPG、PNG、WebP。
- 视频输出支持 MP4、MKV、WebM、MOV、AVI、GIF。
- 音频提取和转换支持 MP3、AAC、M4A、WAV、FLAC、OGG。
- 分辨率预设支持原始尺寸、2160p、1080p、720p、480p、512px、360p。
- 硬件编码选项，可在可用时使用 NVIDIA、Intel、AMD、Apple VideoToolbox、VAAPI。
- 可选 ExifTool 集成，用于读取和清除视频/图片隐私元数据。
- 支持中文和英文界面，支持浅色、深色、跟随系统主题。
- 支持绿色版压缩包、Windows 安装包和 macOS DMG。

## 基本使用

1. 启动 FFGUITool。
2. 首次使用时配置 FFmpeg：
   - 选择已有的 FFmpeg 可执行文件，或
   - 从 `.zip` / `.7z` 压缩包安装 FFmpeg。
3. 选择 **视频处理模式** 或 **图片处理模式**。
4. 选择或拖入文件/文件夹。
5. 调整目标大小、预设、输出格式、质量、分辨率或高级选项。
6. 如有需要，查看 CLI command preview 确认命令。
7. 点击 **开始转换** 执行处理。

后续可以在设置菜单中重新修改 FFmpeg、ExifTool、语言、主题和本地数据配置。

## 可选隐私清理

ExifTool 是可选组件。配置后，FFGUITool 可以读取并清除输出文件中的 GPS 位置、设备型号、作者、创建时间、软件、镜头、媒体处理器等元数据。

不配置 ExifTool 也可以正常压缩和转换。

## 本地编译运行

需要：

- .NET 8 SDK
- Git

克隆并编译：

```bash
git clone https://github.com/brealinxx/FFGUITool.git
cd FFGUITool
dotnet restore FFGUIToolAvalonia.sln
dotnet build FFGUIToolAvalonia.sln
```

本地运行：

```bash
dotnet run --project FFGUITool/FFGUITool.csproj
```

## 发布

项目提供 PowerShell 和 Bash 发布脚本。包版本会自动从 `FFGUITool.csproj` 读取，因此发布文件会命名为类似 `FFGUITool-v1.6.0-<platform>-Portable.zip` 的格式。

常用命令：

```powershell
.\publish.ps1 -Windows -Installer
.\publish.ps1 -MacOS
.\publish.ps1 -All
```

```bash
chmod +x publish.sh
./publish.sh -macos --dmg
./publish.sh -all
```

构建目标与包名标识：

| 平台 | Runtime | 包名标识 |
|---|---|---|
| Windows | `win-x64` | `windows-x64` |
| Windows | `win-x86` | `windows-x86` |
| Windows | `win-arm64` | `windows-arm64` |
| macOS | `osx-x64` | `macos-intel` |
| macOS | `osx-arm64` | `macos-arm64` |

包名格式：

- 绿色版：`FFGUITool-vx.x.x-<platform>-Portable.zip`
- Windows 安装版：`FFGUITool-vx.x.x-<platform>-Installer.exe`
- macOS 安装版：`FFGUITool-vx.x.x-<platform>-Installer.dmg`

构建输出位于 `FFGUITool/bin/publish/`，绿色版压缩包位于 `archives/`，Windows 安装包位于 `installer/`，macOS DMG 位于 `dmg/`。

> DMG 依赖 macOS 的 `hdiutil`，建议在 macOS 系统上构建 macOS 安装包。macOS app bundle 使用 `FFGUITool/Resources/AppIcon.icns`，Windows 构建使用 `FFGUITool/Resources/icon.ico`。

## 许可证

见 [LICENSE](LICENSE)。
