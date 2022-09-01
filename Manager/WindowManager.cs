using MiniMappingway.Window;
using System;

namespace MiniMappingway.Manager
{
    public class WindowManager : IDisposable
    {

        internal readonly NaviMapWindow naviMapWindow = new NaviMapWindow();


        public WindowManager()
        {
            
        }

        public void AddWindowsToWindowSystem()
        {
            Dalamud.Logging.PluginLog.Verbose("Adding Windows To Window System");

            ServiceManager.WindowSystem.AddWindow(naviMapWindow);
        }

        public void Dispose()
        {
            ServiceManager.WindowSystem.RemoveAllWindows();
        }
    }
}
