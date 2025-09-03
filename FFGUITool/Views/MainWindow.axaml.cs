using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using FFGUITool.ViewModels;
using FFGUITool.Services;
using FFGUITool.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Avalonia.Platform.Storage;
using System.Linq;
using Avalonia.Controls.Primitives;

namespace FFGUITool
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        // Constructor for DI
        public MainWindow(MainWindowViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // 绑定文件选择命令到实际的文件对话框操作
            SetupCommandHandlers();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // If ViewModel wasn't injected, try to get it from DI
            if (_viewModel == null)
            {
                _viewModel = Program.ServiceProvider?.GetService<MainWindowViewModel>() 
                    ?? CreateDefaultViewModel();
                DataContext = _viewModel;
            }

            // Initialize the application
            if (_viewModel.InitializeCommand?.CanExecute(null) == true)
            {
                _viewModel.InitializeCommand.Execute(null);
            }
        }
        
        private MainWindowViewModel CreateDefaultViewModel()
        {
            // Create configuration
            var configuration = CreateDefaultConfiguration();
    
            // Create services with proper dependencies
            var ffmpegService = new FFmpegService();
            var mediaAnalyzer = new MediaAnalyzer(ffmpegService);
            var videoProcessor = new VideoProcessor(ffmpegService, mediaAnalyzer);
    
            return new MainWindowViewModel(
                ffmpegService,
                mediaAnalyzer,
                videoProcessor
            );
        }

        private IConfiguration CreateDefaultConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }

        private void SetupCommandHandlers()
        {
            // 当ViewModel的命令执行时，我们需要显示文件对话框
            // 这通过监听命令执行或使用交互服务来实现
        }

        // Override the command execution to show file dialogs
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            if (DataContext is MainWindowViewModel vm)
            {
                _viewModel = vm;
                
                // 重新绑定命令以使用文件对话框
                RebindFileCommands();
            }
        }

        private void RebindFileCommands()
        {
            // 查找按钮并重新设置它们的点击事件
            var selectFileButton = this.FindControl<Button>("SelectFileButton");
            var selectFolderButton = this.FindControl<Button>("SelectFolderButton");
            var selectOutputButton = this.FindControl<Button>("SelectOutputButton");
            
            if (selectFileButton != null)
            {
                selectFileButton.Click -= SelectFileButton_Click;
                selectFileButton.Click += SelectFileButton_Click;
            }
            
            if (selectFolderButton != null)
            {
                selectFolderButton.Click -= SelectFolderButton_Click;
                selectFolderButton.Click += SelectFolderButton_Click;
            }
            
            if (selectOutputButton != null)
            {
                selectOutputButton.Click -= SelectOutputButton_Click;
                selectOutputButton.Click += SelectOutputButton_Click;
            }
        }

        // File dialog handlers - 这些需要保留因为需要访问Window的TopLevel
        private async void SelectFileButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择视频文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("视频文件")
                    {
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var filePath = files[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(filePath) && _viewModel != null)
                {
                    await _viewModel.SetInputFile(filePath);
                }
            }
        }

        private async void SelectFolderButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择包含视频文件的文件夹",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var folderPath = folders[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(folderPath) && _viewModel != null)
                {
                    _viewModel.SetInputFolder(folderPath);
                }
            }
        }

        private async void SelectOutputButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择输出文件夹",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var folderPath = folders[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(folderPath) && _viewModel != null)
                {
                    _viewModel.SetOutputFolder(folderPath);
                }
            }
        }

        // 比特率帮助提示的事件处理器
        private void BitrateHelpButton_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (this.FindControl<Popup>("BitrateTooltipPopup") is Popup popup)
            {
                popup.IsOpen = true;
            }
        }

        private void BitrateHelpButton_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (this.FindControl<Popup>("BitrateTooltipPopup") is Popup popup)
            {
                popup.IsOpen = false;
            }
        }
    }
}