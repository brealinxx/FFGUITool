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
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect();

#if DEBUG
            builder.LogToTrace();
#endif

            return builder;
        }
    }
}
