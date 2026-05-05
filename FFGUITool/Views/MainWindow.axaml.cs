// Views/MainWindow.axaml.cs
using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using FFGUITool.ViewModels;

namespace FFGUITool.Views
{
    /// <summary>
    /// Main window view.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            var initialIsDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            _viewModel.IsThemeDark = initialIsDark;
            _viewModel.CurrentTheme = initialIsDark ? ThemeVariant.Dark : ThemeVariant.Light;
            UpdateTheme(initialIsDark);

            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);

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
            };
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles()?.ToList();
            var path = files?.Count == 1 ? files[0].Path.LocalPath : null;
            e.DragEffects = IsSupportedDroppedFile(path) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            var item = e.Data.GetFiles()?.FirstOrDefault();
            var path = item?.Path.LocalPath;
            if (IsSupportedDroppedFile(path) && _viewModel != null)
            {
                await _viewModel.ProcessSelectedInput(path!);
            }

            e.Handled = true;
        }

        private static bool IsSupportedDroppedFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || System.IO.Directory.Exists(path))
            {
                return false;
            }

            var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return extension is ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm"
                or ".mp3" or ".aac" or ".m4a" or ".wav" or ".flac" or ".ogg" or ".wma";
        }

        private void UpdateTheme(bool isDark)
        {
            var theme = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
            RequestedThemeVariant = theme;

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = theme;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
