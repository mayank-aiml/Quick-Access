using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace QuickAccessHub.Services
{
    public class StartupManager
    {
        private const string AppName = "QuickAccessHub";
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsStartWithWindowsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                var value = key?.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }

        public static bool SetStartWithWindows(bool enable, out string? errorMessage)
        {
            errorMessage = null;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                if (key == null)
                {
                    errorMessage = "Unable to access startup registry key.";
                    return false;
                }

                if (enable)
                {
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName
                        ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                    
                    key.SetValue(AppName, $"\"{exePath}\" --autostart");
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
