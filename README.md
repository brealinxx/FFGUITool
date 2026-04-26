# FFGUITool

[中文说明](README.zh-CN.md)

FFGUITool is a lightweight desktop GUI for FFmpeg video compression. It helps you select an input file or folder, choose compression settings, preview the generated FFmpeg command, and run the conversion without writing commands by hand.

## Download

Download the latest ready-to-use build from:

[GitHub Releases](https://github.com/brealinxx/FFGUITool/releases/latest)

## Basic Usage

1. Start FFGUITool.
2. Configure FFmpeg when prompted:
   - Select an existing `ffmpeg.exe`, or
   - Install FFmpeg from a `.zip` or `.7z` archive.
3. Choose a video file or folder.
4. Adjust compression strength, target bitrate, codec, and output location.
5. Check the CLI command preview if needed.
6. Click **Start** to begin conversion.

You can change FFmpeg settings later from **Tools > FFmpeg Settings**.

## Build From Source

Requirements:

- .NET 8 SDK
- Git

Clone and build:

```bash
git clone https://github.com/brealinxx/FFGUITool.git
cd FFGUITool
dotnet restore FFGUIToolAvalonia.sln
dotnet build FFGUIToolAvalonia.sln
```

Run locally:

```bash
dotnet run --project FFGUITool/FFGUITool.csproj
```

## License

See [LICENSE](LICENSE).
