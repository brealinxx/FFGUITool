# FFGUITool

[English](README.md) · [下载](https://github.com/brealinxx/FFGUITool/releases/latest) · [更新记录](CHANGELOG.md)

基于 FFmpeg 的跨平台媒体压缩与转换工具，支持视频、音频和图片，无需手写命令。

![FFGUITool 主界面：文件队列、压缩参数与命令预览](Assets/intro.png)

## 下载与安装

前往 [GitHub Releases](https://github.com/brealinxx/FFGUITool/releases/latest)，选择对应系统和架构：

| 系统 | 可选架构 | 安装方式 |
| --- | --- | --- |
| Windows | x64、x86、ARM64 | 安装包 `.exe` 或绿色版 `.zip` |
| macOS | Intel、Apple Silicon | `.dmg` 或绿色版 `.zip` |
| Linux | x64、ARM64 | 绿色版 `.zip` |

首次启动时配置 **FFmpeg**，可选择已有程序或从压缩包安装。建议同时提供 ffprobe；ExifTool 为可选元数据工具。

## 主要功能

- **压缩与转换**：按目标大小、画质或码率处理媒体，支持裁剪、缩放、音频提取和兼容格式的无损转封装。
- **独立与批量任务**：单独导入的文件各自保存参数；文件夹共用参数，支持子目录扫描、搜索、失败筛选和排序。
- **试压与预设**：对比短样片或图片，保存常用参数，查看处理历史并重新使用参数。
- **输出保护**：支持命名模板和子目录结构；编码失败或取消时保留旧输出，失败任务可重试。
- **易用界面**：中英文、浅深主题、可选托盘模式，以及可复制的 CLI 命令预览。

常用格式包括 MP4、MKV、WebM、MOV、MP3、WAV、FLAC、JPG、PNG、WebP、ICO 等；实际编码能力取决于 FFmpeg 构建和设备。

## 快速上手

1. 选择“视频处理模式”（含音频）或“图片处理模式”。
2. 拖入文件，或选择一个文件夹；导入后查看数量及跳过原因。
3. 设置处理目标、大小/画质和输出格式。参数区会标明“仅当前文件”或“整批文件共享”。
4. 在“输出设置”中选择保存位置和命名规则；需要时先“试压与画面对比”。
5. 点击处理当前文件或全部任务，完成后在“处理结果”中打开输出。

多个独立文件需要统一参数时，使用“应用当前设置到全部”。“偏好设置 → 应用设置”管理托盘行为和图片并发数，自动保存且不随预设切换。

快捷键：`Ctrl+O` 导入 · `Ctrl+Enter` 执行 · `Escape` 取消。

## 使用前了解

- 图片和视频/音频按当前模式筛选，尚不支持混合类型统一队列；文件夹需单独导入。
- 目标大小不一定能达到；超限会明确警告。图片只有在启用“允许降低图片尺寸”后才会为达标缩小尺寸。
- 视频试压最多取 10 秒，不能准确预测完整文件大小；恢复队列会重新执行未完成任务，不是断点续传。
- 硬件编码依赖设备及驱动；两遍编码仅适用于兼容的软件 H.264/VP9 码率模式。
- 动图按静态图片处理时仅导出第一帧；HDR 转 SDR 不会自动进行，建议先检查试压效果。

## 本地开发

需要 .NET 8 SDK；媒体处理和集成测试还需要 FFmpeg。

```bash
dotnet restore FFGUIToolAvalonia.sln
dotnet run --project FFGUITool/FFGUITool.csproj
```

测试、千文件界面验证和打包方法见 [开发与发布指南](docs/DEVELOPMENT.md)。当前绿色版命名示例：`FFGUITool-v1.10.0-<platform>-Portable.zip`。

## 许可证

见 [LICENSE](LICENSE)。
