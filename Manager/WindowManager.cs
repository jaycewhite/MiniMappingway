using System;
using MiniMappingway.Windows;

namespace MiniMappingway.Manager
{
    public class WindowManager : IDisposable
    {

        internal readonly NaviMapWindow NaviMapWindow = new();
        internal readonly SettingsWindow SettingsWindow = new();

        public void AddWindowsToWindowSystem()
        {
            ServiceManager.Log.Verbose("Adding Windows To Window System");

            ServiceManager.WindowSystem.AddWindow(NaviMapWindow);
            ServiceManager.WindowSystem.AddWindow(SettingsWindow);
        }

        public void Dispose()
        {
            ServiceManager.WindowSystem.RemoveAllWindows();
        }
    }
}
