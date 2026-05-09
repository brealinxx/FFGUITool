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

项目同时提供 PowerShell 与 Bash 两种发布脚本，功能保持一致。

- Windows 用户推荐使用 PowerShell 脚本（`publish.ps1`）
- macOS / Linux 用户推荐使用 Bash 脚本（`publish.sh`）

默认情况下，脚本会根据当前系统自动构建对应的平台组合，并自动生成 `.zip` 压缩包。

默认构建目标：

| 平台 | 构建目标 |
|---|---|
| Windows | `win-x64`、`win-x86`、`win-arm64` |
| macOS | `osx-x64`、`osx-arm64` |

---

### Windows（推荐使用 PowerShell）

构建默认 Windows 平台组合：

```powershell
.\publish.ps1
```

显式构建 Windows 平台：

```powershell
.\publish.ps1 -Windows
```

构建 macOS 平台：

```powershell
.\publish.ps1 -MacOS
```

同时构建 Windows 与 macOS 平台：

```powershell
.\publish.ps1 -All
```

生成 `.7z` 压缩包而不是 `.zip`：

```powershell
.\publish.ps1 -Windows -Archive 7z
```

---

### macOS / Linux（Bash）

首次使用时，如有需要请先赋予脚本执行权限：

```bash
chmod +x publish.sh
```

构建当前系统默认平台组合：

```bash
./publish.sh
```

构建 Windows 平台：

```bash
./publish.sh -windows
```

构建 macOS 平台：

```bash
./publish.sh -macos
```

同时构建所有平台：

```bash
./publish.sh -all
```

生成 `.7z` 压缩包：

```bash
./publish.sh -windows --archive 7z
```

---

构建输出目录：

```text
FFGUITool/bin/publish/
```

压缩包输出目录：

```text
FFGUITool/bin/publish/archives/
```

生成的目标目录：

- `FFGUITool-win-x64`
- `FFGUITool-win-x86`
- `FFGUITool-win-arm64`
- `FFGUITool-osx-x64`
- `FFGUITool-osx-arm64`

> 建议在 macOS 系统上构建 macOS 发布包后再进行实际分发。从 Windows 交叉构建 macOS 包时，部分情况下可能无法在真实 Mac 设备上正常运行。

## 许可证

见 [LICENSE](LICENSE)。
