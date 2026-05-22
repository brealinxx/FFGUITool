# FFGUITool

[English README](README.md)

FFGUITool 是一个基于 FFmpeg 的轻量桌面视频压缩与转换工具。它可以帮助你选择输入文件或文件夹、调整压缩参数、使用常用预设、转换视频/音频格式、批量处理文件夹，并预览生成的 FFmpeg 命令。

## 下载

可以从 GitHub Releases 下载最新可直接使用的版本：

[GitHub Releases](https://github.com/brealinxx/FFGUITool/releases/latest)

## 基本使用

1. 启动 FFGUITool。
2. 首次使用时配置 FFmpeg：
   - 选择已有的 `ffmpeg.exe`，或
   - 从 `.zip` / `.7z` 压缩包安装 FFmpeg。
3. 选择视频文件、音频文件或文件夹。
4. 设置目标大小、预设模式、输出位置等参数。
5. 如需更多控制，可开启高级模式，调整目标码率、编码器、格式转换、音频转换或分辨率转换。
6. 查看 CLI command preview 确认命令。
7. 点击 **开始转换** 执行处理。

后续可以通过 **工具 > FFmpeg 设置** 重新修改 FFmpeg 配置。

## 主要功能

- 基于目标大小的视频压缩。
- 预设压缩模式：无、微信/QQ 发送、邮箱附件、网页上传、极限压缩。
- 高级模式：目标码率、编码器、格式转换、音频转换、分辨率转换。
- 视频格式转换：MP4、MKV、WebM、MOV、AVI、GIF。
- 音频转换/提取：MP3、AAC、M4A、WAV、FLAC、OGG。
- 分辨率转换：2160p、1080p、720p、480p、360p。
- 拖拽单个视频或音频文件到窗口。
- 文件夹批量模式，统一处理文件夹内的媒体文件。
- FFmpeg 命令预览与复制。

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

项目提供 PowerShell 与 Bash 发布脚本。脚本会生成区分架构的 Portable 压缩包；Windows 可生成 Inno Setup 安装包，macOS 可在 macOS 系统上生成 DMG 安装包。

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

> DMG 依赖 macOS 的 `hdiutil`，建议在 macOS 系统上构建 macOS 安装包。

## 许可证

见 [LICENSE](LICENSE)。
