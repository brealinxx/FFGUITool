// Views/MainWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Styling;
using FFGUITool.ViewModels;
using System;

namespace FFGUITool.Views
{
    /// <summary>
    /// 主窗口视图
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // 创建并设置ViewModel
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            
            // 窗口加载时初始化ViewModel
            Loaded += async (sender, e) =>
            {
                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }
            };
            
            // 监听主题变化
            ActualThemeVariantChanged += OnThemeChanged;
        }
        
        private void OnThemeChanged(object? sender, EventArgs e)
        {
            // 当系统主题变化时，通知ViewModel更新资源
            if (_viewModel != null && ActualThemeVariant != null)
            {
                var isDark = ActualThemeVariant == ThemeVariant.Dark;
                if (_viewModel.CurrentTheme == ThemeVariant.Default)
                {
                    // 如果是跟随系统模式，更新显示状态
                    _viewModel.IsThemeDark = isDark;
                }
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            ActualThemeVariantChanged -= OnThemeChanged;
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}