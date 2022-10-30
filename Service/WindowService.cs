using System;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using MiniMappingway.Service.Interface;
using MiniMappingway.Window;

namespace MiniMappingway.Service
{
    public class WindowService : IDisposable, IWindowService
    {

        public NaviMapWindow NaviMapWindow { get; }

        private readonly WindowSystem _windowSystem = new("MiniMappingway");

        public WindowService(IGameStateService gameStateService, IMapService mapService, IMarkerService markerService,
            IConfigurationService configurationService, IPersonService personService
            , ISourceService sourceService)
        {
            NaviMapWindow = new NaviMapWindow(gameStateService,mapService,markerService,configurationService,personService,sourceService);
        }

        public void Draw()
        {
            _windowSystem.Draw();
        }

        public void AddWindowsToWindowSystem()
        {
            PluginLog.Verbose("Adding Windows To Window System");

            _windowSystem.AddWindow(NaviMapWindow);
        }

        public void Dispose()
        {
            _windowSystem.RemoveAllWindows();
        }
    }
}
