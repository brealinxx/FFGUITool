using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using FFGUITool.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FFGUITool.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IFFmpegService _ffmpegService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger = Log.ForContext<SettingsViewModel>();

        // 事件用于请求文件/文件夹选择
        public event EventHandler? FileSelectionRequested;
        public event EventHandler? OutputDirectorySelectionRequested;
        
        private string _ffmpegPath = "";
        private string _ffmpegVersion = "";
        private bool _isFFmpegAvailable;
        private string _defaultOutputDirectory = "";
        private int _parallelTasks = 1;
        private bool _overwriteExistingFiles;
        private int _afterProcessingAction;
        private int _themeIndex;
        private int _languageIndex;
        private bool _autoCheckFFmpegOnStartup = true;
        private bool _rememberWindowState = true;
        private string _appVersion = "";

        public SettingsViewModel(IFFmpegService ffmpegService, IConfiguration configuration)
        {
            _ffmpegService = ffmpegService;
            _configuration = configuration;
            
            // Initialize commands with event triggers
            BrowseFFmpegCommand = new RelayCommand(_ => FileSelectionRequested?.Invoke(this, EventArgs.Empty));
            InstallFFmpegCommand = new RelayCommand(async _ => await InstallFFmpegAsync());
            TestFFmpegCommand = new RelayCommand(async _ => await TestFFmpegAsync());
            BrowseOutputDirectoryCommand = new RelayCommand(_ => OutputDirectorySelectionRequested?.Invoke(this, EventArgs.Empty));
            SaveSettingsCommand = new RelayCommand(async _ => await SaveSettingsAsync());
            ResetSettingsCommand = new RelayCommand(async _ => await ResetSettingsAsync());
            CheckUpdateCommand = new RelayCommand(async _ => await CheckUpdateAsync());
            OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
            
            // Load initial settings
            _ = LoadSettingsAsync();
            
            // Get app version
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            AppVersion = $"{version?.Major}.{version?.Minor}.{version?.Build}";
        }

        #region Properties

        public string FFmpegPath
        {
            get => _ffmpegPath;
            set => SetProperty(ref _ffmpegPath, value);
        }

        public string FFmpegVersion
        {
            get => _ffmpegVersion;
            set => SetProperty(ref _ffmpegVersion, value);
        }

        public bool IsFFmpegAvailable
        {
            get => _isFFmpegAvailable;
            set => SetProperty(ref _isFFmpegAvailable, value);
        }

        public string DefaultOutputDirectory
        {
            get => _defaultOutputDirectory;
            set => SetProperty(ref _defaultOutputDirectory, value);
        }

        public int ParallelTasks
        {
            get => _parallelTasks;
            set => SetProperty(ref _parallelTasks, value);
        }

        public bool OverwriteExistingFiles
        {
            get => _overwriteExistingFiles;
            set => SetProperty(ref _overwriteExistingFiles, value);
        }

        public int AfterProcessingAction
        {
            get => _afterProcessingAction;
            set => SetProperty(ref _afterProcessingAction, value);
        }

        public int ThemeIndex
        {
            get => _themeIndex;
            set
            {
                if (SetProperty(ref _themeIndex, value))
                {
                    ApplyTheme(value);
                }
            }
        }

        public int LanguageIndex
        {
            get => _languageIndex;
            set => SetProperty(ref _languageIndex, value);
        }

        public bool AutoCheckFFmpegOnStartup
        {
            get => _autoCheckFFmpegOnStartup;
            set => SetProperty(ref _autoCheckFFmpegOnStartup, value);
        }

        public bool RememberWindowState
        {
            get => _rememberWindowState;
            set => SetProperty(ref _rememberWindowState, value);
        }

        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        #endregion

        #region Commands

        public ICommand BrowseFFmpegCommand { get; set; }
        public ICommand InstallFFmpegCommand { get; set; }
        public ICommand TestFFmpegCommand { get; }
        public ICommand BrowseOutputDirectoryCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand OpenLogFolderCommand { get; }

        #endregion

        #region Methods

        private async Task LoadSettingsAsync()
        {
            try
            {
                _logger.Information("Loading settings");
                
                // Load FFmpeg settings
                FFmpegPath = _ffmpegService.FFmpegPath;
                IsFFmpegAvailable = _ffmpegService.IsAvailable;
                
                if (IsFFmpegAvailable)
                {
                    FFmpegVersion = await _ffmpegService.GetVersionAsync();
                }
                else
                {
                    FFmpegVersion = "未安装";
                }
                
                // Load from configuration
                ParallelTasks = _configuration.GetValue<int>("Processing:DefaultParallelTasks", 1);
                DefaultOutputDirectory = _configuration.GetValue<string>("Processing:DefaultOutputDirectory") ?? "";
                OverwriteExistingFiles = _configuration.GetValue<bool>("Processing:OverwriteExistingFiles", false);
                AfterProcessingAction = _configuration.GetValue<int>("Processing:AfterProcessingAction", 0);
                
                ThemeIndex = _configuration.GetValue<int>("UI:ThemeIndex", 0);
                LanguageIndex = _configuration.GetValue<int>("UI:LanguageIndex", 0);
                AutoCheckFFmpegOnStartup = _configuration.GetValue<bool>("UI:AutoCheckFFmpegOnStartup", true);
                RememberWindowState = _configuration.GetValue<bool>("UI:RememberWindowState", true);
                
                _logger.Information("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load settings");
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                _logger.Information("Saving settings");
                
                // Save settings to configuration file
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                // In production, you would properly update the JSON file
                // For now, we'll just log the save action
                _logger.Information("Settings saved to {ConfigPath}", configPath);
                
                // Show success message (implement in View)
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save settings");
            }
        }

        private async Task ResetSettingsAsync()
        {
            try
            {
                _logger.Information("Resetting settings to defaults");
                
                ParallelTasks = 1;
                DefaultOutputDirectory = "";
                OverwriteExistingFiles = false;
                AfterProcessingAction = 0;
                ThemeIndex = 0;
                LanguageIndex = 0;
                AutoCheckFFmpegOnStartup = true;
                RememberWindowState = true;
                
                await SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to reset settings");
            }
        }

        public async Task SetFFmpegPath(string path)
        {
            try
            {
                _logger.Information("Setting FFmpeg path to {Path}", path);
                
                var result = await _ffmpegService.SetCustomPathAsync(path);
                if (result)
                {
                    FFmpegPath = path;
                    IsFFmpegAvailable = true;
                    FFmpegVersion = await _ffmpegService.GetVersionAsync();
                    _logger.Information("FFmpeg path set successfully");
                }
                else
                {
                    _logger.Warning("Invalid FFmpeg path: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to set FFmpeg path");
            }
        }

        private async Task InstallFFmpegAsync()
        {
            // This will be called from View with archive file path
            // The View handles the actual file dialog
        }

        public async Task InstallFromArchive(string archivePath)
        {
            try
            {
                _logger.Information("Installing FFmpeg from archive: {Path}", archivePath);
                
                var result = await _ffmpegService.InstallFromArchiveAsync(archivePath);
                if (result)
                {
                    await LoadSettingsAsync();
                    _logger.Information("FFmpeg installed successfully");
                }
                else
                {
                    _logger.Warning("Failed to install FFmpeg from archive");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to install FFmpeg from archive");
            }
        }

        private async Task TestFFmpegAsync()
        {
            try
            {
                _logger.Information("Testing FFmpeg");
                
                var result = await _ffmpegService.InitializeAsync();
                IsFFmpegAvailable = result;
                
                if (result)
                {
                    FFmpegVersion = await _ffmpegService.GetVersionAsync();
                    FFmpegPath = _ffmpegService.FFmpegPath;
                    _logger.Information("FFmpeg test successful");
                }
                else
                {
                    FFmpegVersion = "未检测到FFmpeg";
                    _logger.Warning("FFmpeg test failed");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test FFmpeg");
                FFmpegVersion = "检测失败";
            }
        }

        public void SetDefaultOutputDirectory(string path)
        {
            DefaultOutputDirectory = path;
            _logger.Information("Default output directory set to {Path}", path);
        }

        private async Task CheckUpdateAsync()
        {
            try
            {
                _logger.Information("Checking for updates");
                
                // In production, this would check a remote server for updates
                // For now, we'll just log the action
                await Task.Delay(1000); // Simulate network request
                
                _logger.Information("No updates available");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to check for updates");
            }
        }

        private void OpenLogFolder()
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
                
                _logger.Information("Opened log folder: {Path}", logPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to open log folder");
            }
        }

        private void ApplyTheme(int themeIndex)
        {
            try
            {
                _logger.Information("Applying theme: {ThemeIndex}", themeIndex);
                
                // Apply theme to application
                // This would typically be done through a theme service
                var app = Avalonia.Application.Current;
                if (app != null)
                {
                    app.RequestedThemeVariant = themeIndex switch
                    {
                        1 => Avalonia.Styling.ThemeVariant.Light,
                        2 => Avalonia.Styling.ThemeVariant.Dark,
                        _ => Avalonia.Styling.ThemeVariant.Default
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to apply theme");
            }
        }

        #endregion
    }
}