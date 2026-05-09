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

The project provides both PowerShell and Bash publish scripts with the same functionality.

- Windows users are recommended to use the PowerShell script (`publish.ps1`)
- macOS/Linux users are recommended to use the Bash script (`publish.sh`)

By default, the scripts build the current platform group and automatically create `.zip` archives.

Default targets:

| Platform | Targets |
|---|---|
| Windows | `win-x64`, `win-x86`, `win-arm64` |
| macOS | `osx-x64`, `osx-arm64` |

---

### Windows (PowerShell Recommended)

Build the default Windows target group:

```powershell
.\publish.ps1
```

Build Windows packages explicitly:

```powershell
.\publish.ps1 -Windows
```

Build macOS packages:

```powershell
.\publish.ps1 -MacOS
```

Build all Windows and macOS packages:

```powershell
.\publish.ps1 -All
```

Create `.7z` archives instead of `.zip`:

```powershell
.\publish.ps1 -Windows -Archive 7z
```

---

### macOS / Linux (Bash)

Make the script executable first if needed:

```bash
chmod +x publish.sh
```

Build the default platform group:

```bash
./publish.sh
```

Build Windows packages:

```bash
./publish.sh -windows
```

Build macOS packages:

```bash
./publish.sh -macos
```

Build all packages:

```bash
./publish.sh -all
```

Create `.7z` archives:

```bash
./publish.sh -windows --archive 7z
```

---

Outputs are written to:

```text
FFGUITool/bin/publish/
```

Archives are written to:

```text
FFGUITool/bin/publish/archives/
```

Generated target folders:

- `FFGUITool-win-x64`
- `FFGUITool-win-x86`
- `FFGUITool-win-arm64`
- `FFGUITool-osx-x64`
- `FFGUITool-osx-arm64`

> It is recommended to build macOS packages directly on macOS when distributing to real Mac devices. Cross-building macOS packages from Windows may produce archives that do not run correctly on macOS.

## License

See [LICENSE](LICENSE).
