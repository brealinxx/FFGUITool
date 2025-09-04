// Views/MainWindow.axaml.cs
using Avalonia.Controls;
using FFGUITool.ViewModels;

namespace FFGUITool.Views
{
    /// <summary>
    /// 主窗口视图
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 创建并设置ViewModel
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            
            // 窗口加载时初始化ViewModel
            Loaded += async (sender, e) =>
            {
                await viewModel.InitializeAsync();
            };
            
            // 添加退出命令处理
            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.Title))
                {
                    // 如果需要根据ViewModel更新UI，可以在这里处理
                }
            };
        }
    }
}