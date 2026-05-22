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

## Publish Packages

The project provides PowerShell and Bash publish scripts. Both create architecture-specific Portable archives; Windows can also create Inno Setup installers, and macOS can create DMG installers on macOS.

Common commands:

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

Targets and package labels:

| Platform | Runtime | Package label |
|---|---|---|
| Windows | `win-x64` | `windows-x64` |
| Windows | `win-x86` | `windows-x86` |
| Windows | `win-arm64` | `windows-arm64` |
| macOS | `osx-x64` | `macos-intel` |
| macOS | `osx-arm64` | `macos-arm64` |

Package names use this format:

- Portable: `FFGUITool-vx.x.0-<platform>-Portable.zip`
- Windows installer: `FFGUITool-vx.x.0-<platform>-Installer.exe`
- macOS installer: `FFGUITool-vx.x.0-<platform>-Installer.dmg`

Outputs are written to `FFGUITool/bin/publish/`, Portable archives to `archives/`, Windows installers to `installer/`, and macOS DMGs to `dmg/`.

> Build macOS DMG packages on macOS because DMG creation uses `hdiutil`.

## License

See [LICENSE](LICENSE).
