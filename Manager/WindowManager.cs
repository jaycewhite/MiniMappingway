using System;
using MiniMappingway.Window;

namespace MiniMappingway.Manager
{
    public class WindowManager : IDisposable
    {

        internal readonly NaviMapWindow NaviMapWindow = new();

        public void AddWindowsToWindowSystem()
        {
            ServiceManager.Log.Verbose("Adding Windows To Window System");

            ServiceManager.WindowSystem.AddWindow(NaviMapWindow);
        }

        public void Dispose()
        {
            ServiceManager.WindowSystem.RemoveAllWindows();
        }
    }
}
