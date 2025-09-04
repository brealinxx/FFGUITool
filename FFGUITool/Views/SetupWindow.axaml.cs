// Views/SetupWindow.axaml.cs

using System;
using Avalonia.Controls;
using FFGUITool.ViewModels;

namespace FFGUITool.Views
{
    /// <summary>
    /// FFmpeg设置窗口视图
    /// </summary>
    public partial class SetupWindow : Window
    {
        public SetupWindow()
        {
            InitializeComponent();
            
            // 如果DataContext没有在外部设置，创建默认的ViewModel
            if (DataContext == null)
            {
                DataContext = new SetupWindowViewModel();
            }
            
            // 监听ViewModel的关闭请求
            if (DataContext is SetupWindowViewModel viewModel)
            {
                viewModel.OnCloseRequested += Close;
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // 清理事件订阅
            if (DataContext is SetupWindowViewModel viewModel)
            {
                viewModel.OnCloseRequested -= Close;
            }
            
            base.OnClosed(e);
        }
    }
}