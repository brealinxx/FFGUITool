# Changelog

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
