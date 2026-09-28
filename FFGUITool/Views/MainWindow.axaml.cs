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
            SizeChanged += (_, _) =>
            {
                UpdateWorkspaceColumns();
                // Re-arrange the workspace when shrinking a window with scrollable content.
                WorkspaceRoot.InvalidateMeasure();
                WorkspaceRoot.InvalidateArrange();
                if (Content is Control content)
                {
                    content.InvalidateMeasure();
                    content.InvalidateArrange();
                }
            };

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = _viewModel.CurrentTheme;
            }
            _viewModel.IsThemeDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            UpdateTheme(_viewModel.CurrentTheme);
            PropertyChanged += (_, e) =>
            {
                if (e.Property == ActualThemeVariantProperty && _viewModel != null)
                    _viewModel.IsThemeDark = ActualThemeVariant == ThemeVariant.Dark;
            };

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

        private bool _wideLayout;
        private void UpdateWorkspaceColumns()
        {
            // 360 DIP for tasks and at least 700 DIP for the English parameter editor.
            var wide = ClientSize.Width >= 1180;
            if (_wideLayout == wide) return;
            _wideLayout = wide;
            if (wide)
            {
                EditorStack.Children.Remove(TaskColumn);
                TaskScroll.Content = TaskColumn;
            }
            else
            {
                TaskScroll.Content = null;
                EditorStack.Children.Insert(0, TaskColumn);
            }
            WorkspaceColumns.ColumnDefinitions[0].Width = new GridLength(wide ? 360 : 0);
            WorkspaceColumns.ColumnDefinitions[1].Width = new GridLength(wide ? 16 : 0);
            TaskScroll.IsVisible = wide;
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles()?.ToList();
            e.DragEffects = files?.Count > 0 ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            var paths = e.Data.GetFiles()?.Select(file => file.Path.LocalPath).ToList() ?? [];
            if (_viewModel != null && paths.Count > 0)
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

        private bool _exitPending;
        public async Task RequestExitAsync()
        {
            if (_exitPending) return;
            _exitPending = true;
            try
            {
                if (_viewModel == null || await _viewModel.PrepareForExitAsync())
                { AllowClose = true; Close(); }
            }
            finally { _exitPending = false; }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                if (_viewModel?.CloseToTray == true)
                {
                    Hide();
                    var config = AppConfigService.Load();
                    if (!config.TrayHintShown)
                    {
                        SystemNotificationService.Show("FFGUITool", LocalizationService.T("Improve.TrayHint"));
                        config.TrayHintShown = true; AppConfigService.Save(config);
                    }
                }
                else _ = RequestExitAsync();
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
