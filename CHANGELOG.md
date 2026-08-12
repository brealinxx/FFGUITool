# Changelog

## v1.9.0

### Added

- Added browser-style source tabs for video and image files. Every file task keeps its own processing settings and analysis state.
- Added a shared-settings folder workflow with a batch preview, per-file inclusion controls, and optional subfolder scanning.
- Added task queue actions for processing the current file or all files, cancelling an active queue, and retrying failed tasks.
- Added queue progress and per-task status reporting. A failed task no longer prevents the remaining tasks from running.
- Added output conflict handling with automatic renaming or explicit overwrite behavior.
- Added automated coverage for processing workspaces, task selection, failure continuation, cancellation, and output conflicts.

### Changed

- Unified independent file tabs and shared folder entries around the `ProcessingTask` model.
- Extracted media discovery, command execution, result formatting, and queue coordination from `MainWindowViewModel` into focused services and a processing-specific partial view model.
- Updated drag-and-drop so multiple supported video or image files become independent tasks instead of replacing the current input.
- Updated the main window layout to expose source tabs, batch controls, queue progress, retry, and cancellation actions.

### Fixed

- Preserved each file tab's settings when switching between multiple video or image inputs.
- Prevented folder batches from accidentally diverging into per-file settings by keeping one shared configuration for the batch.
- Prevented existing or duplicate output paths from being overwritten silently.

### Version

- Updated application version metadata from `1.8.0` to `1.9.0`.
- Updated assembly version and file version to `1.9.0.0`.
- Updated Windows application manifest assembly identity to `1.9.0.0`.
- Updated installer and package documentation version metadata to `1.9.0`.

## v1.8.0

### Added

- Added a system tray icon with quick actions for Video mode, Image mode, and full application exit.
- Added ICO and ICNS image conversion output support, including selectable icon sizes written into a single icon file.

### Changed

- Changed the main window close button to minimize the app to the system tray instead of exiting.
- Changed ICO export behavior to create a single `.ico` file instead of automatically packaging generated icons into a ZIP archive.
- Updated English and Chinese README feature lists to mention ICO and ICNS conversion support.

### Fixed

- Fixed setup wizard completion after FFmpeg is detected from the system command path.
- Unified FFmpeg and ExifTool setup status display through the wizard progress/status area.
- Fixed preset and image-mode editability so WeChat/QQ presets keep target size locked while advanced mode remains editable, and image target size controls remain editable.
- Fixed ICO/ICNS image conversion UI so resolution conversion is disabled for icon output.

### Version

- Updated application version metadata from `1.7.0` to `1.8.0`.
- Updated assembly version and file version to `1.8.0.0`.
- Updated Windows application manifest assembly identity to `1.8.0.0`.
- Updated installer and package documentation version metadata to `1.8.0`.

## v1.7.0

### Added

- Added automated test coverage for command generation, output paths, image format mapping, batch file filtering, version metadata, package naming, README examples, changelog entries, and release icons.
- Added GitHub Actions CI for Windows, macOS, and Linux builds with `dotnet test`, release metadata checks, and tag-triggered publishing.
- Added `scripts/release-check.ps1` to verify version consistency, changelog coverage, icon files, and README package naming.
- Added local file logging for app startup, FFmpeg detection, FFmpeg command execution, stderr summaries, output diagnostics, and crashes.
- Added FFmpeg failure actions for copying error details, copying the full command, and opening the log directory.
- Added FFmpeg encoder capability probing so unavailable codec/image options can be hidden from the UI.
- Added Help menu update checking and GitHub Releases access.
- Added external JSON localization override support through `i18n/*.json` and `%AppData%/FFGUITool/i18n/*.json`.
- Added Linux publish targets to the PowerShell and Bash publish scripts.

### Changed

- Reorganized the menu bar into Tools, Preferences, and Help groups.
- Improved local data cleanup so app logs are included in cleanup prompts and cleanup behavior.
- Updated the Windows uninstall helper to remove `%AppData%\FFGUITool` and the app registry key after uninstall.
- Centralized media file extension filtering through `MediaFileSupport`.

### Fixed

- Fixed hard-coded English strings in update-check and failure-action UI paths so they follow the selected language.
- Fixed release automation to run standard `dotnet test` instead of a custom test runner command.

### Version

- Updated application version metadata from `1.6.0` to `1.7.0`.
- Updated assembly version and file version to `1.7.0.0`.
- Updated Windows application manifest assembly identity to `1.7.0.0`.
- Updated installer and package documentation version metadata to `1.7.0`.

## v1.6.0

### Added

- Added broader image input support for HEIF, GIF, TIFF, ICO, TGA, and AVIF files in image mode.
- Added automatic release package version detection from the project file for Portable, installer, and DMG names.
- Added macOS bundle icon wiring through `AppIcon.icns` and explicit Windows installer shortcut icon wiring through `icon.ico`.

### Changed

- Updated English and Chinese README files to match the current video, audio, image, metadata, theme, and packaging features.
- Improved button layout and hit areas in the main window.

### Fixed

- Fixed CLI preview fallback text so it follows the selected UI language.
- Fixed macOS DMG app bundles missing the application icon.
- Fixed Windows installer shortcuts continuing to use stale default icon sources.

### Version

- Updated application version metadata from `1.5.0` to `1.6.0`.
- Updated assembly version and file version to `1.6.0.0`.
- Updated Windows application manifest assembly identity to `1.6.0.0`.
- Updated installer and macOS bundle version metadata to `1.6.0`.

## v1.5.0

### Added

- Added Settings menu theme switching with System, Light, and Dark modes, including current-selection markers.
- Added persistent local app configuration in `%AppData%\FFGUITool\config.json` for theme and language preferences.
- Added a local data cleanup action for removing app config files and registry records.
- Added audio bitrate choices for audio conversion, including 320, 256, 128, 96, 64, and 8 kb/s.
- Added AV1 (`libaom-av1`) as a selectable video encoder.
- Added Windows Inno Setup installer generation for x64, x86, and ARM64 packages.
- Added macOS DMG installer packaging support for Intel and Apple Silicon builds.
- Added installer-time app language selection so FFGUITool can start in the selected Chinese or English interface language.

### Changed

- Renamed release package outputs to distinguish Portable archives from Installer packages.
- Simplified release documentation and clarified package naming for Windows and macOS architectures.
- Improved advanced conversion layout so dynamically shown conversion controls stay above the Privacy section.
- Improved audio-only conversion summaries so output messages match the actual audio conversion result.
- Updated the About dialog to include ExifTool version information.
- Updated ExifTool settings from the menu to open the setup wizard directly on the ExifTool tab.

### Fixed

- Disabled video format and resolution conversion controls now appear greyed out and non-clickable while audio conversion is enabled.
- Fixed language refresh behavior for conversion option lists.
- Fixed theme icon/state consistency between the main window and menu actions.
- Fixed Inno Setup Chinese language file resolution by bundling the required language file.

### Version

- Updated application version metadata from `1.4.0` to `1.5.0`.
- Updated assembly version and file version to `1.5.0.0`.
- Updated Windows application manifest assembly identity to `1.5.0.0`.
- Updated installer and macOS bundle version metadata to `1.5.0`.

## v1.4.0

### Added

- Added ExifTool integration for image and video metadata detection, including sensitive fields such as GPS location, author, creation time, device model, lens, software, and handler metadata.
- Added optional ExifTool configuration to the FFmpeg setup wizard, with system command detection, executable selection, folder search, and zip archive installation.
- Added an advanced metadata removal option that runs ExifTool after output creation to strip metadata from generated files.
- Added localized Chinese and English labels for metadata removal and its sensitive-information preview.
- Added a unified redetect action that checks both FFmpeg and ExifTool configuration status.

### Changed

- Image quality changes now update target-size estimates and per-file ratio values in real time.
- Image batch estimates now show the expected total output size for all images.
- Image per-file ratio and target-size controls now display one decimal place.
- Reworked the menu bar by removing the File menu, renaming Tools to Settings, and moving language selection under Settings.
- Metadata removal is disabled when ExifTool is not available.
- Renamed the redetect menu item from FFmpeg-only wording to a general redetect action.
- Reduced startup/runtime overhead by removing unused dependency injection setup, the embedded Inter font package, and release trace logging.

### Fixed

- Fixed image target-size calculations so per-file percentage compression is applied consistently during batch processing.
- Fixed video source details so duration and bitrate refresh immediately after analysis instead of waiting for a language/layout refresh.
- Improved image fallback compression by allowing smaller image dimensions to be parsed before resize retries.

### Version

- Updated application version metadata from `1.3.0` to `1.4.0`.
- Updated assembly version and file version to `1.4.0.0`.
- Updated Windows application manifest assembly identity to `1.4.0.0`.

## v1.3.0

### Added

- Added per-file ratio batch compression for video and image folders so each source file gets its own target size.
- Added folder drag-and-drop support for video, audio, and image batch workflows.
- Added video advanced controls for CRF quality mode and hardware acceleration.
- Added automatic hardware encoder recommendation with NVIDIA, Intel, AMD, Apple VideoToolbox, and VAAPI candidates.
- Added a hardware acceleration help dialog explaining performance, quality, compatibility, and fallback tradeoffs.

### Changed

- Batch mode now shows a per-file ratio control instead of applying one absolute target size to every file.
- CRF mode now disables target-size and bitrate controls because output size cannot be reliably predicted.
- CRF controls stay hidden until CRF mode is enabled.
- Image advanced mode now labels the quality control as quality instead of bitrate.
- Output names now use the source filename first, followed by `FFGUIToolOutPut` and the active output label.
- Hardware acceleration labels and options now refresh when switching languages.
- Shortened advanced-mode labels and descriptions to fit better in English and Chinese layouts.

### Fixed

- Fixed a freeze when dragging folders that contain videos into the app.
- Fixed batch CLI previews so the sample video bitrate updates when the per-file ratio changes.
- Fixed image batch mode so changing the quality slider no longer rewrites the per-file ratio.
- Fixed image batch CLI previews so quality-related FFmpeg parameters update immediately.
- Fixed missing Apple VideoToolbox options on macOS.

### Version

- Updated application version metadata from `1.2.0` to `1.3.0`.
- Updated assembly version and file version to `1.3.0.0`.
- Updated Windows application manifest assembly identity to `1.3.0.0`.

## v1.2.0

### Added

- Added a startup mode selector for video processing and image processing.
- Added image processing mode with image compression, format conversion, and resize presets.
- Added image target-size controls with KB/MB unit selection.
- Added target-size driven image compression retries that reduce quality and then dimensions when needed.
- Added system notifications and detailed completion summaries for successful or failed processing.

### Changed

- Video processing mode keeps the existing video workflow after mode selection.
- Image mode now shows image-specific source details such as file size, format, and resolution.
- Image target-size slider now uses the selected image size as the maximum value and 1 KB as the minimum.
- Advanced mode now starts closed in both processing modes.
- Estimate text now stays blank until an input is selected or settings provide enough context.
- Updated mode selector layout with fixed, symmetrical mode cards.
- Improved localized text refresh when switching languages.

### Fixed

- Fixed default output directory handling when the user does not choose an output folder.
- Fixed image file-size display for small files by using B, KB, MB, or GB as appropriate.
- Fixed image target-size unit conversion between KB and MB.
- Fixed image-mode estimate and completion messages so they no longer show video bitrate wording.
- Fixed alignment of the image target-size input and unit selector.

### Version

- Updated application version metadata from `1.1.0` to `1.2.0`.
- Updated assembly version and file version to `1.2.0.0`.
- Updated Windows application manifest assembly identity to `1.2.0.0`.

## v1.1.0

### Added

- Added target-size based compression. The main compression slider now uses the selected video's file size as the maximum target size and recalculates the target bitrate automatically.
- Added compression presets:
  - None: manual target-size compression.
  - WeChat/QQ: H.264, max 720p, max 30fps, 800k-1500k video bitrate, AAC 96k audio.
  - Email attachment: H.264, default 25MB target, max 720p, AAC 64k audio.
  - Web upload: H.264 CRF 23, source resolution/framerate, AAC 128k audio.
  - Extreme compression: H.265 CRF 30, max 480p, max 24fps, AAC 48k audio.
- Added help dialogs for compression presets and conversion tools.
- Added advanced mode for power-user settings.
- Added format conversion tools for MP4, MKV, WebM, MOV, AVI, and video-to-GIF output.
- Added audio conversion and audio extraction support for MP3, AAC, M4A, WAV, FLAC, and OGG.
- Added resolution conversion options for 2160p, 1080p, 720p, 480p, and 360p.
- Added support for selecting audio files as input.
- Added drag-and-drop support for a single video or audio file.
- Added folder batch mode. Selecting a folder now scans supported media files and applies the same settings to each file.

### Changed

- Moved target bitrate and encoder selection behind advanced mode.
- Moved format, audio, and resolution conversion toggles into advanced mode.
- Updated the advanced mode control styling to make it visually distinct from regular conversion toggles.
- CLI preview now reflects preset, conversion, audio extraction, GIF, and batch-mode choices more clearly.
- Batch-mode CLI preview now shows a batch summary and the first generated command as an example.
- Output file names now include the target size and selected output format.
- WebM output now uses VP9 video with Opus audio by default.

### Fixed

- Prevented conflicting conversion modes from being enabled together.
- Audio conversion is now exclusive and automatically disables video format and resolution conversion.
- GIF output now removes audio automatically.
- Audio input now disables video-only conversion controls.
- Target bitrate is now synchronized from target size, presets, and advanced bitrate edits.

### Version

- Updated application version metadata from `1.0.0` to `1.1.0`.
- Updated assembly version and file version to `1.1.0.0`.
