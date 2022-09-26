using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using MiniMappingway.Api;
using MiniMappingway.Service;

namespace MiniMappingway.Manager
{
    public class ServiceManager
    {
        //Dalamud services
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static GameGui GameGui { get; private set; } = null!;
        [PluginService] public static ObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] public static DataManager DataManager { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface DalamudPluginInterface { get; private set; } = null!;
        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
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
