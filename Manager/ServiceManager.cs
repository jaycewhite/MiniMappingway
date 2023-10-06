using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MiniMappingway.Api;
using MiniMappingway.Service;

namespace MiniMappingway.Manager
{
    public class ServiceManager
    {
        //Dalamud services
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface DalamudPluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        public static WindowSystem WindowSystem { get; private set; } = new("MiniMappingway");


        //Custom services
        public static FinderService FinderService { get; set; } = null!;


        //Custom managers
        public static NaviMapManager NaviMapManager { get; set; } = null!;
        public static Configuration Configuration { get; set; } = null!;
        public static PluginUi PluginUi { get; set; } = null!;
        public static WindowManager WindowManager { get; set; } = null!;

        //Api Controller 
        public static ApiController ApiController { get; set; } = null!;

        public static void Dispose()
        {
            FinderService.Dispose();
            NaviMapManager.Dispose();
            WindowManager.Dispose();
            ApiController.Dispose();
        }
    }
}
