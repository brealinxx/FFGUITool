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
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}