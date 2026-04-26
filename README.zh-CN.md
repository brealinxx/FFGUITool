# FFGUITool

[English README](README.md)

FFGUITool 是一个基于 FFmpeg 的轻量桌面视频压缩工具。它可以帮助你选择输入文件或文件夹、调整压缩参数、预览生成的 FFmpeg 命令，并直接执行转换。

## 下载

可以从 GitHub Releases 下载最新可直接使用的版本：

[GitHub Releases](https://github.com/brealinxx/FFGUITool/releases/latest)

## 基本使用

1. 启动 FFGUITool。
2. 首次使用时配置 FFmpeg：
   - 选择已有的 `ffmpeg.exe`，或
   - 从 `.zip` / `.7z` 压缩包安装 FFmpeg。
3. 选择视频文件或文件夹。
4. 调整压缩强度、目标码率、编码器和输出位置。
5. 如有需要，可以查看 CLI command preview。
6. 点击 **开始转换** 执行压缩。

后续可以通过 **工具 > FFmpeg设置** 重新修改 FFmpeg 配置。

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

## 许可证

见 [LICENSE](LICENSE)。
