using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using FFGUITool.Models;
using FFGUITool.Services;

namespace FFGUITool.ViewModels;

public partial class MainWindowViewModel
{
        private void UpdateConversionOptionLists()
        {
            UpdateVideoFormatOptions();
            UpdateAudioFormatOptions();
            UpdateResolutionOptions();
            UpdateImageFormatOptions();
        }

        private void UpdateVideoFormatOptions()
        {
            var selectedValue = SelectedVideoFormatOption?.Value ?? CompressionSettings.OutputFormat;
            VideoFormatOptions = new List<CodecOption>
            {
                new("MP4", "mp4", LocalizationService.T("VideoFormat.MP4.Desc")),
                new("MKV", "mkv", LocalizationService.T("VideoFormat.MKV.Desc")),
                new("WebM", "webm", LocalizationService.T("VideoFormat.WebM.Desc")),
                new("MOV", "mov", LocalizationService.T("VideoFormat.MOV.Desc")),
                new("AVI", "avi", LocalizationService.T("VideoFormat.AVI.Desc")),
                new("GIF", "gif", LocalizationService.T("VideoFormat.GIF.Desc"))
            };
            SelectedVideoFormatOption = VideoFormatOptions.Find(option => option.Value == selectedValue) ?? VideoFormatOptions[0];
        }

        private void UpdateAudioFormatOptions()
        {
            var selectedValue = SelectedAudioFormatOption?.Value ?? CompressionSettings.AudioOutputFormat;
            AudioFormatOptions = new List<CodecOption>
            {
                new("MP3", "mp3", LocalizationService.T("AudioFormat.MP3.Desc")),
                new("AAC", "aac", LocalizationService.T("AudioFormat.AAC.Desc")),
                new("M4A", "m4a", LocalizationService.T("AudioFormat.M4A.Desc")),
                new("WAV", "wav", LocalizationService.T("AudioFormat.WAV.Desc")),
                new("FLAC", "flac", LocalizationService.T("AudioFormat.FLAC.Desc")),
                new("OGG", "ogg", LocalizationService.T("AudioFormat.OGG.Desc"))
            };
            SelectedAudioFormatOption = AudioFormatOptions.Find(option => option.Value == selectedValue) ?? AudioFormatOptions[0];
        }

        private void UpdateResolutionOptions()
        {
            var selectedValue = SelectedResolutionOption?.Value ?? CompressionSettings.ResolutionHeight.ToString();
            ResolutionOptions = new List<CodecOption>
            {
                new(LocalizationService.T("Resolution.Original"), "0", LocalizationService.T("Resolution.Original.Desc")),
                new("2160p", "2160", "4K"),
                new("1080p", "1080", LocalizationService.T("Resolution.1080.Desc")),
                new("720p", "720", LocalizationService.T("Resolution.720.Desc")),
                new("480p", "480", LocalizationService.T("Resolution.480.Desc")),
                new("512px", "512", LocalizationService.T("Resolution.512.Desc")),
                new("360p", "360", LocalizationService.T("Resolution.360.Desc"))
            };
            SelectedResolutionOption = ResolutionOptions.Find(option => option.Value == selectedValue) ?? ResolutionOptions[3];
        }

        private void UpdateImageFormatOptions()
        {
            var selectedValue = SelectedImageFormatOption?.Value ?? CompressionSettings.ImageOutputFormat;
            var options = new List<CodecOption>
            {
                new("JPG", "jpg", LocalizationService.T("ImageFormat.JPG.Desc")),
                new("PNG", "png", LocalizationService.T("ImageFormat.PNG.Desc"))
            };

            if (IsEncoderAvailableOrUnknown("libwebp"))
            {
                options.Add(new("WebP", "webp", LocalizationService.T("ImageFormat.WebP.Desc")));
            }

            options.Add(new("ICO", "ico", LocalizationService.CurrentLanguage == "en-US" ? "Windows icon" : "Windows 图标"));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                options.Add(new("ICNS", "icns", LocalizationService.CurrentLanguage == "en-US" ? "macOS icon" : "macOS 图标"));
            }

            ImageFormatOptions = options;
            SelectedImageFormatOption = ImageFormatOptions.Find(option => option.Value == selectedValue) ?? ImageFormatOptions[0];
            IsIconOptionsVisible = IsImageMode && IsIconFormat(SelectedImageFormatOption.Value);
        }

        private void UpdateCodecOptions()
        {
            var selectedValue = SelectedCodecOption?.Value ?? SelectedCodec;
            var candidates = new List<CodecOption>
            {
                new("H.264 (libx264)", "libx264", LocalizationService.T("Codec.H264.Desc")),
                new("H.265 (libx265)", "libx265", LocalizationService.T("Codec.H265.Desc")),
                new("VP9 (libvpx-vp9)", "libvpx-vp9", LocalizationService.T("Codec.VP9.Desc")),
                new("AV1 (libaom-av1)", "libaom-av1", LocalizationService.T("Codec.AV1.Desc"))
            };

            CodecOptions = candidates
                .Where(option => IsEncoderAvailableOrUnknown(option.Value))
                .ToList();

            if (CodecOptions.Count == 0)
            {
                CodecOptions = candidates;
            }

            SelectedCodecOption = CodecOptions.Find(option => option.Value == selectedValue) ?? CodecOptions[0];
        }

        private void UpdateAudioBitrateOptions()
        {
            var selectedValue = SelectedAudioBitrateOption?.Value ?? CompressionSettings.AudioBitrate.ToString();
            AudioBitrateOptions = new List<CodecOption>
            {
                new("320 kb/s", "320", LocalizationService.T("AudioBitrate.320.Desc")),
                new("256 kb/s", "256", LocalizationService.T("AudioBitrate.256.Desc")),
                new("128 kb/s", "128", LocalizationService.T("AudioBitrate.128.Desc")),
                new("96 kb/s", "96", LocalizationService.T("AudioBitrate.96.Desc")),
                new("64 kb/s", "64", LocalizationService.T("AudioBitrate.64.Desc")),
                new("8 kb/s", "8", LocalizationService.T("AudioBitrate.8.Desc"))
            };
            SelectedAudioBitrateOption = AudioBitrateOptions.Find(option => option.Value == selectedValue)
                                         ?? AudioBitrateOptions[^1];
        }

        private async Task RefreshHardwareEncoderOptions()
        {
            var selectedValue = SelectedHardwareEncoderOption?.Value ?? "";
            _cachedGpuText = await Task.Run(GetLocalGpuText);
            _availableVideoEncoders = await _ffmpegManager.GetAvailableVideoEncoders();
            _availableVideoDecoders = await _ffmpegManager.GetAvailableVideoDecoders();
            UpdateCodecOptions();
            UpdateImageFormatOptions();
            HardwareEncoderOptions = CreateHardwareEncoderOptions(_availableVideoEncoders);
            SelectedHardwareEncoderOption = HardwareEncoderOptions.Find(option => option.Value == selectedValue)
                ?? HardwareEncoderOptions[0];
            CompressionSettings.HardwareEncoder = SelectedHardwareEncoderOption.Value == "auto"
                ? RecommendHardwareEncoder()
                : SelectedHardwareEncoderOption.Value;
        }

        private bool IsEncoderAvailableOrUnknown(string encoder)
        {
            return !_ffmpegManager.IsFFmpegAvailable ||
                   _availableVideoEncoders.Count == 0 ||
                   _availableVideoEncoders.Contains(encoder);
        }

        private List<CodecOption> CreateHardwareEncoderOptions(IReadOnlySet<string> availableEncoders)
        {
            var isEnglish = LocalizationService.CurrentLanguage == "en-US";
            var options = new List<CodecOption>
            {
                new(isEnglish ? "Off" : "关闭", "", isEnglish ? "Software" : "软件编码"),
                new(isEnglish ? "Auto" : "自动推荐", "auto", isEnglish ? "Recommended" : "自动选择")
            };

            var candidates = new (string Encoder, string Name, string Description)[]
            {
                ("h264_nvenc", "NVIDIA H.264 (NVENC)", "NVIDIA GPU"),
                ("hevc_nvenc", "NVIDIA H.265 (NVENC)", "NVIDIA GPU"),
                ("h264_qsv", "Intel H.264 (QSV)", "Intel Quick Sync"),
                ("hevc_qsv", "Intel H.265 (QSV)", "Intel Quick Sync"),
                ("h264_amf", "AMD H.264 (AMF)", "AMD GPU"),
                ("hevc_amf", "AMD H.265 (AMF)", "AMD GPU"),
                ("h264_videotoolbox", "Apple VideoToolbox H.264", "Apple"),
                ("hevc_videotoolbox", "Apple VideoToolbox H.265", "Apple"),
                ("h264_vaapi", "VAAPI H.264", "Linux VAAPI"),
                ("hevc_vaapi", "VAAPI H.265", "Linux VAAPI")
            };

            foreach (var candidate in candidates)
            {
                if (availableEncoders.Contains(candidate.Encoder) || IsAppleVideoToolboxCandidate(candidate.Encoder))
                {
                    options.Add(new CodecOption(candidate.Name, candidate.Encoder, candidate.Description));
                }
            }

            if (options.Count == 2)
            {
                options[0].Description = isEnglish
                    ? "No hardware encoder reported by current FFmpeg"
                    : "当前 FFmpeg 未报告可用硬件编码器";
            }

            return options;
        }

        private static bool IsAppleVideoToolboxCandidate(string encoder)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                   && encoder.EndsWith("_videotoolbox", StringComparison.OrdinalIgnoreCase);
        }

        private string RecommendHardwareEncoder()
        {
            var availableEncoders = HardwareEncoderOptions
                .Select(option => option.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "auto")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return PickFirstAvailable(availableEncoders, "hevc_videotoolbox", "h264_videotoolbox");
            }

            var gpuText = _cachedGpuText;
            if (gpuText.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                return PickFirstAvailable(availableEncoders, "hevc_nvenc", "h264_nvenc");
            }

            if (gpuText.Contains("AMD", StringComparison.OrdinalIgnoreCase) || gpuText.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            {
                return PickFirstAvailable(availableEncoders, "hevc_amf", "h264_amf");
            }

            if (gpuText.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                return PickFirstAvailable(availableEncoders, "hevc_qsv", "h264_qsv");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return PickFirstAvailable(availableEncoders, "hevc_vaapi", "h264_vaapi");
            }

            return PickFirstAvailable(
                availableEncoders,
                "hevc_nvenc",
                "h264_nvenc",
                "hevc_qsv",
                "h264_qsv",
                "hevc_amf",
                "h264_amf",
                "hevc_videotoolbox",
                "h264_videotoolbox",
                "hevc_vaapi",
                "h264_vaapi");
        }

        private static string PickFirstAvailable(IReadOnlySet<string> availableEncoders, params string[] candidates)
        {
            return candidates.FirstOrDefault(availableEncoders.Contains) ?? "";
        }

        private string _cachedGpuText = "";

        private static string GetLocalGpuText()
        {
            var nvidiaInfo = TryReadProcessOutput("nvidia-smi", "--query-gpu=name --format=csv,noheader");
            if (!string.IsNullOrWhiteSpace(nvidiaInfo))
            {
                return nvidiaInfo;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return TryReadProcessOutput("wmic", "path win32_VideoController get name");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return TryReadProcessOutput("system_profiler", "SPDisplaysDataType");
            }

            return TryReadProcessOutput("lspci", "");
        }

        private static string TryReadProcessOutput(string fileName, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();
                if (!process.WaitForExit(1500))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                }

                return process.StandardOutput.ReadToEnd();
            }
            catch
            {
                return "";
            }
        }

}
