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

        public void Dispose()
        {
            ServiceManager.WindowSystem.RemoveAllWindows();
        }
    }
}
