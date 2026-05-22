using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FFGUITool.Services
{
    public static class LocalDataCleanupService
    {
        private const string AppRegistryKey = @"Software\FFGUITool";

        public static string CleanupTargetDescription => AppConfigService.AppDataPath;

        public static void DeleteLocalDataAndRegistry()
        {
            try
            {
                if (Directory.Exists(AppConfigService.AppDataPath))
                {
                    Directory.Delete(AppConfigService.AppDataPath, recursive: true);
                }
            }
            catch
            {
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(AppRegistryKey, throwOnMissingSubKey: false);
            }
            catch
            {
            }
        }

        public static void OpenConfigFolder()
        {
            Directory.CreateDirectory(AppConfigService.AppDataPath);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppConfigService.AppDataPath,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }
    }
}
