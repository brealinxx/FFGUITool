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
        private SetupWindowViewModel? _viewModel;

        public SetupWindow()
        {
            InitializeComponent();

            // 如果DataContext没有在外部设置，创建默认的ViewModel
            if (DataContext == null)
            {
                DataContext = new SetupWindowViewModel();
            }

            SubscribeCloseRequest(DataContext as SetupWindowViewModel);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            SubscribeCloseRequest(DataContext as SetupWindowViewModel);
        }

        private void SubscribeCloseRequest(SetupWindowViewModel? viewModel)
        {
            if (_viewModel == viewModel)
            {
                return;
            }

            if (_viewModel != null)
            {
                _viewModel.OnCloseRequested -= Close;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.OnCloseRequested += Close;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理事件订阅
            if (_viewModel != null)
            {
                _viewModel.OnCloseRequested -= Close;
                _viewModel.Dispose();
                _viewModel = null;
            }

            base.OnClosed(e);
        }
    }
}
