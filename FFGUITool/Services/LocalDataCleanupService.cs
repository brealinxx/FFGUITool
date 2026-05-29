using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace FFGUITool.Services
{
    public static class LocalDataCleanupService
    {
        private const string AppRegistryKey = @"Software\FFGUITool";

        public static string CleanupTargetDescription => AppConfigService.AppDataPath;

        public static void DeleteLocalDataAndRegistry()
        {
            DeleteDirectory(AppLogger.LogDirectory);
            DeleteDirectory(AppConfigService.AppDataPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DeleteRegistryKey();
            }
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
            }
        }

        [SupportedOSPlatform("windows")]
        private static void DeleteRegistryKey()
        {
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
