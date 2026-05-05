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

## 打包发布

项目根目录提供了预设打包脚本。

一次性发布 Windows 和 macOS 的所有预设目标：

```bash
./publish.sh
```

Windows PowerShell 下：

```powershell
.\publish.ps1
```

在 macOS 机器上只发布 macOS 版本：

```bash
./publish-macos.sh
```

只发布单个 macOS 目标：

```bash
./publish-macos.sh osx-arm64
./publish-macos.sh osx-x64
```

只发布任意单个目标：

```bash
./publish.sh win-x64
```

```powershell
.\publish.ps1 -Runtime win-x64
```

支持的目标：

- `win-x86`
- `win-x64`
- `win-arm64`
- `osx-x64`
- `osx-arm64`

输出位置为 `FFGUITool/bin/publish/`，文件夹命名如下：

- `FFGUITool-win-x86`
- `FFGUITool-win-x64`
- `FFGUITool-win-arm64`
- `FFGUITool-osx-x64`
- `FFGUITool-osx-arm64`

macOS/Linux 首次运行脚本前可能需要：

```bash
chmod +x publish.sh publish-macos.sh
```

## 许可证

见 [LICENSE](LICENSE)。
