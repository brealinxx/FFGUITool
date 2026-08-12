// Views/MainWindow.axaml.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using FFGUITool.Services;
using FFGUITool.ViewModels;

namespace FFGUITool.Views
{
    /// <summary>
    /// Main window view.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;
        public bool AllowClose { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = _viewModel.CurrentTheme;
            }
            _viewModel.IsThemeDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            UpdateTheme(_viewModel.CurrentTheme);

            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.CurrentTheme))
                {
                    UpdateTheme(_viewModel.CurrentTheme);
                }
                else if (e.PropertyName == nameof(MainWindowViewModel.IsWorkspaceVisible) && _viewModel.IsWorkspaceVisible)
                {
                    _ = PlayWorkspaceIntroAsync();
                }
            };

            Loaded += async (sender, e) =>
            {
                await _viewModel.InitializeAsync();
            };
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles()?.ToList();
            var imageMode = _viewModel?.IsImageMode == true;
            e.DragEffects = files?.Count > 0 && files.All(file => MediaFileSupport.IsSupportedDroppedPath(file.Path.LocalPath, imageMode))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            var paths = e.Data.GetFiles()?.Select(file => file.Path.LocalPath).ToList() ?? [];
            if (_viewModel != null && paths.Count > 0 && paths.All(path => MediaFileSupport.IsSupportedDroppedPath(path, _viewModel.IsImageMode)))
            {
                await _viewModel.ProcessSelectedInputs(paths);
            }

            e.Handled = true;
        }

        private void UpdateTheme(ThemeVariant theme)
        {
            RequestedThemeVariant = theme;

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = theme;
            }

            if (_viewModel != null)
            {
                _viewModel.IsThemeDark = ActualThemeVariant == ThemeVariant.Dark;
            }
        }

        private async Task PlayWorkspaceIntroAsync()
        {
            WorkspaceRoot.Opacity = 0;
            WorkspaceRoot.RenderTransform = new TranslateTransform(0, 18);

            for (var i = 1; i <= 10; i++)
            {
                var progress = i / 10.0;
                WorkspaceRoot.Opacity = progress;

                if (WorkspaceRoot.RenderTransform is TranslateTransform transform)
                {
                    transform.Y = 18 * (1 - progress);
                }

                await Task.Delay(16);
            }

            WorkspaceRoot.Opacity = 1;
            WorkspaceRoot.RenderTransform = new TranslateTransform(0, 0);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
