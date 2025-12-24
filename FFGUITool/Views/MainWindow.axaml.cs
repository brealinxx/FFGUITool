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
            
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
            
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.IsThemeDark))
                {
                    UpdateTheme(_viewModel.IsThemeDark);
                }
            };

            Loaded += async (sender, e) =>
            {
                await _viewModel.InitializeAsync();
                // 初始化时应用一次主题
                UpdateTheme(_viewModel.IsThemeDark);
            };
        }
        
        private void UpdateTheme(bool isDark)
        {
            // 强制更改窗口的请求主题
            this.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
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