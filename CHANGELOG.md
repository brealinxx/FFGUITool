# Changelog

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
