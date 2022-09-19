using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using MiniMappingway.Manager;
using MiniMappingway.Service;

namespace MiniMappingway
{
    public class ServiceManager
    {
        //Dalamud services
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static GameGui GameGui { get; set; } = null!;
        [PluginService] public static ObjectTable ObjectTable { get; set; } = null!;
        [PluginService] public static DataManager DataManager { get; set; } = null!;
        [PluginService] public static SigScanner SigScanner { get; set; } = null!;
        [PluginService] public static DalamudPluginInterface DalamudPluginInterface { get; set; } = null!;
        [PluginService] public static CommandManager CommandManager { get; set; } = null!;
        [PluginService] public static Framework Framework { get; set; } = null!;
        public static WindowSystem WindowSystem { get; set; } = new("MiniMappingway");


        //Custom services
        public static FinderService FinderService { get; set; } = null!;


        //Custom managers
        public static NaviMapManager NaviMapManager { get; set; } = null!;
        public static Configuration Configuration { get; set; } = null!;
        public static PluginUI PluginUi { get; set; } = null!;
        public static WindowManager WindowManager { get; set; } = null!;

    }
}
