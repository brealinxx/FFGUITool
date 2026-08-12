# FFGUITool

[中文说明](README.zh-CN.md)

FFGUITool is a lightweight cross-platform desktop GUI for FFmpeg. It helps you compress and convert videos, audio, and images, configure multiple files as independent tasks, process folders with shared settings, preview generated CLI commands, and run common FFmpeg workflows without writing commands by hand.

## Download

Download the latest ready-to-use build from:

[GitHub Releases](https://github.com/brealinxx/FFGUITool/releases/latest)

## Features

- Video compression by target size, target bitrate, preset, or CRF quality mode.
- Image compression by target size or quality, with KB/MB target-size controls.
- Video, audio, and image format conversion.
- Browser-style source tabs for multiple video or image files, with independent settings for every file.
- Folder batch processing with shared settings, a file preview, per-file inclusion controls, optional subfolder scanning, and per-file target ratios.
- Queue controls for processing the current task or all tasks, progress tracking, cancellation, failure continuation, and retrying failed tasks.
- Output conflict choices for automatic renaming or overwriting existing files.
- Drag-and-drop support for one or multiple files and folders.
- Image input support for JPG, JPEG, PNG, WebP, HEIC, HEIF, BMP, GIF, TIFF, ICO, TGA, and AVIF.
- Image output formats: JPG, PNG, WebP, ICO, and ICNS.
- Video output formats: MP4, MKV, WebM, MOV, AVI, and GIF.
- Audio extraction/conversion to MP3, AAC, M4A, WAV, FLAC, and OGG.
- Resolution presets including original size, 2160p, 1080p, 720p, 480p, 512px, and 360p.
- Hardware encoder options, including NVIDIA, Intel, AMD, Apple VideoToolbox, and VAAPI when available.
- Optional ExifTool integration for reading and removing privacy metadata from videos and images.
- Chinese and English UI, plus light, dark, and system themes.
- Portable archives, Windows installers, and macOS DMG packages.

## Basic Usage

1. Start FFGUITool.
2. Configure FFmpeg when prompted:
   - Select an existing FFmpeg executable, or
   - Install FFmpeg from a `.zip` or `.7z` archive.
3. Choose **Video mode** or **Image mode**.
4. Select or drag in one or more files, or select a folder.
5. For files, switch between source tabs and configure each task independently. Use **Apply to all** when several files should share the current tab's settings.
6. For a folder, configure the shared settings, choose whether to scan subfolders, and include or exclude files in the batch preview.
7. Adjust target size, preset, output format, quality, resolution, or advanced options, then check the CLI command preview if needed.
8. Process the current tab or the whole queue. You can cancel an active queue or retry failed tasks afterward.

You can change FFmpeg, ExifTool, language, theme, and local data settings later from the Settings menu.

## Optional Privacy Cleanup

ExifTool is optional. When configured, FFGUITool can inspect and remove metadata such as GPS location, device model, author, creation time, software, lens, and media handler fields after FFmpeg creates the output file.

Compression and conversion still work without ExifTool.

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

The project provides PowerShell and Bash publish scripts. Package versions are read automatically from `FFGUITool.csproj`, so release files are named like `FFGUITool-v1.9.0-<platform>-Portable.zip`.

Common commands:

```powershell
.\publish.ps1 -Windows -Installer
.\publish.ps1 -MacOS
.\publish.ps1 -Linux
.\publish.ps1 -All
```

```bash
chmod +x publish.sh
./publish.sh -macos --dmg
./publish.sh -linux
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

- Portable: `FFGUITool-vx.x.x-<platform>-Portable.zip`
- Windows installer: `FFGUITool-vx.x.x-<platform>-Installer.exe`
- macOS installer: `FFGUITool-vx.x.x-<platform>-Installer.dmg`

Outputs are written to `FFGUITool/bin/publish/`, Portable archives to `archives/`, Windows installers to `installer/`, and macOS DMGs to `dmg/`.

> Build macOS DMG packages on macOS because DMG creation uses `hdiutil`. The macOS app bundle uses `FFGUITool/Resources/AppIcon.icns`; Windows builds use `FFGUITool/Resources/icon.ico`.

## License

See [LICENSE](LICENSE).
