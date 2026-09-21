using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FFGUITool.Services;

namespace FFGUITool.Views;

public sealed class PreviewWindow : Window
{
    public PreviewWindow(PreviewArtifact artifact)
    {
        Title = LocalizationService.T("Improve.Preview"); Width = 1100; Height = 760;
        MinWidth = 700; MinHeight = 500; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var before = new Bitmap(artifact.BeforeImage);
        var after = new Bitmap(artifact.AfterImage);
        var first = new Image { Source = before, Stretch = Stretch.Uniform, Width = 480, Height = 460 };
        var second = new Image { Source = after, Stretch = Stretch.Uniform, Width = 480, Height = 460 };
        var beforeScroll = new ScrollViewer { Content = first, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        var afterScroll = new ScrollViewer { Content = second, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        beforeScroll.ScrollChanged += (_, _) => { if (afterScroll.Offset != beforeScroll.Offset) afterScroll.Offset = beforeScroll.Offset; };
        afterScroll.ScrollChanged += (_, _) => { if (beforeScroll.Offset != afterScroll.Offset) beforeScroll.Offset = afterScroll.Offset; };
        var zoom = new Slider { Minimum = 0.1, Maximum = 3, Value = 0.5, Width = 260 };
        zoom.PropertyChanged += (_, e) =>
        {
            if (e.Property != Slider.ValueProperty) return;
            first.Width = before.PixelSize.Width * zoom.Value; first.Height = before.PixelSize.Height * zoom.Value;
            second.Width = after.PixelSize.Width * zoom.Value; second.Height = after.PixelSize.Height * zoom.Value;
        };
        var play = new Button { Content = LocalizationService.T("Improve.OpenResult") };
        play.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(artifact.SamplePath) { UseShellExecute = true }); }
            catch (Exception ex) { AppLogger.Error("Cannot open preview.", ex); play.Content = ex.Message; }
        };
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Children =
        { new TextBlock { Text = LocalizationService.T("Improve.Zoom"), VerticalAlignment = VerticalAlignment.Center }, zoom, play } };
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), Margin = new Thickness(18) };
        root.Children.Add(controls);
        var titles = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        titles.Children.Add(new TextBlock { Text = LocalizationService.T("Improve.Before"), Margin = new Thickness(0,12) });
        var afterTitle = new TextBlock { Text = LocalizationService.T("Improve.After"), Margin = new Thickness(0,12) };
        Grid.SetColumn(afterTitle, 1); titles.Children.Add(afterTitle); Grid.SetRow(titles, 1); root.Children.Add(titles);
        var images = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        images.Children.Add(beforeScroll); Grid.SetColumn(afterScroll, 1); images.Children.Add(afterScroll);
        Grid.SetRow(images, 2); root.Children.Add(images);
        var summary = new ScrollViewer { MaxHeight = 120, Content = new TextBlock { Text = artifact.Summary, TextWrapping = TextWrapping.Wrap } };
        Grid.SetRow(summary, 3); root.Children.Add(summary);
        Content = root;
        Closed += (_, _) => { first.Source = null; second.Source = null; before.Dispose(); after.Dispose(); };
    }
}
