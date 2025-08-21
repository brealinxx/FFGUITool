using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Models;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.Services
{
    public class BatchProcessor : IBatchProcessor
    {
        private readonly IVideoProcessor _videoProcessor;
        private readonly IMediaAnalyzer _mediaAnalyzer;
        
        public int MaxParallelTasks { get; set; } = 1;

        public BatchProcessor(IVideoProcessor videoProcessor, IMediaAnalyzer mediaAnalyzer)
        {
            _videoProcessor = videoProcessor;
            _mediaAnalyzer = mediaAnalyzer;
        }

        public async Task<BatchProcessResult> ProcessFolderAsync(
            string folderPath, 
            ProcessingOptions options, 
            IProgress<BatchProgress>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            var files = ScanFolder(folderPath, GetExtensionsForType(options.Type));
            return await ProcessFileListAsync(files, options, progress, cancellationToken);
        }

        public async Task<BatchProcessResult> ProcessFileListAsync(
            IEnumerable<string> files, 
            ProcessingOptions options, 
            IProgress<BatchProgress>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            var fileList = files.ToList();
            var result = new BatchProcessResult();
            var errors = new List<ProcessingError>();
            var startTime = DateTime.Now;

            var batchProgress = new BatchProgress
            {
                TotalTasks = fileList.Count,
                CompletedTasks = 0,
                FailedTasks = 0
            };

            // Process files with parallelism control
            var semaphore = new SemaphoreSlim(MaxParallelTasks, MaxParallelTasks);
            var tasks = fileList.Select(async file =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    batchProgress.CurrentFile = file;
                    progress?.Report(batchProgress);

                    var success = await ProcessSingleFile(file, options, cancellationToken);
                    
                    if (success)
                    {
                        Interlocked.Increment(ref result.SuccessCount);
                        Interlocked.Increment(ref batchProgress.CompletedTasks);
                    }
                    else
                    {
                        Interlocked.Increment(ref result.FailureCount);
                        Interlocked.Increment(ref batchProgress.FailedTasks);
                        errors.Add(new ProcessingError
                        {
                            FilePath = file,
                            ErrorMessage = "Processing failed",
                            Timestamp = DateTime.Now
                        });
                    }

                    batchProgress.OverallProgress = (double)(batchProgress.CompletedTasks + batchProgress.FailedTasks) / batchProgress.TotalTasks * 100;
                    progress?.Report(batchProgress);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref result.FailureCount);
                    Interlocked.Increment(ref batchProgress.FailedTasks);
                    errors.Add(new ProcessingError
                    {
                        FilePath = file,
                        ErrorMessage = ex.Message,
                        Timestamp = DateTime.Now
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            result.TotalProcessed = fileList.Count;
            result.TotalDuration = DateTime.Now - startTime;
            result.Errors = errors;

            return result;
        }

        public IEnumerable<string> ScanFolder(string folderPath, string[] extensions, bool recursive = true)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = new List<string>();

            foreach (var extension in extensions)
            {
                files.AddRange(Directory.GetFiles(folderPath, $"*{extension}", searchOption));
            }

            return files.Distinct().OrderBy(f => f);
        }

        private async Task<bool> ProcessSingleFile(string filePath, ProcessingOptions options, CancellationToken cancellationToken)
        {
            try
            {
                var outputFile = GenerateOutputPath(filePath, options);

                // Check if output file exists and overwrite setting
                if (File.Exists(outputFile) && !options.OverwriteExisting)
                {
                    return false;
                }

                // Process based on type
                switch (options.Type)
                {
                    case ProcessingType.VideoCompression:
                        if (options.VideoOptions == null)
                        {
                            options.VideoOptions = new VideoCompressionOptions();
                        }
                        options.VideoOptions.InputFile = filePath;
                        options.VideoOptions.OutputFile = outputFile;
                        return await _videoProcessor.CompressVideoAsync(options.VideoOptions, null, cancellationToken);

                    case ProcessingType.AudioConversion:
                        // TODO: Implement audio conversion
                        return false;

                    case ProcessingType.FormatConversion:
                        // TODO: Implement format conversion
                        return false;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private string GenerateOutputPath(string inputFile, ProcessingOptions options)
        {
            var directory = string.IsNullOrEmpty(options.OutputDirectory) 
                ? Path.GetDirectoryName(inputFile) ?? ""
                : options.OutputDirectory;

            var fileName = Path.GetFileNameWithoutExtension(inputFile);
            var extension = Path.GetExtension(inputFile);

            var outputFileName = options.OutputFilePattern
                .Replace("{name}", fileName)
                .Replace("{ext}", extension);

            return Path.Combine(directory, outputFileName);
        }

        private string[] GetExtensionsForType(ProcessingType type)
        {
            return type switch
            {
                ProcessingType.VideoCompression => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" },
                ProcessingType.AudioConversion => new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma" },
                _ => new[] { "*.*" }
            };
        }
    }
}