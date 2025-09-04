using Avalonia;
using System;

namespace FFGUITool
{
    internal class Program
    {
        // 程序入口点
        [STAThread]
        public static void Main(string[] args) 
            => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

        // 配置 Avalonia
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()   // 使用内置字体（可选）
                .LogToTrace();
    }
}