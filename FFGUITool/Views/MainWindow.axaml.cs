// Views/MainWindow.axaml.cs
using System;
using Avalonia;
using Avalonia.Controls;
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
