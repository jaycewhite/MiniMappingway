using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using MiniMappingway.Manager;
using MiniMappingWay.Service;

namespace MiniMappingWay
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Mini-Mappingway";

        private const string commandName = "/mmway";


        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

        private DalamudPluginInterface PluginInterface { get; init; }
        private DataManager DataManager { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private FinderService finderService { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] SigScanner sigScanner,
            [RequiredVersion("1.0")] ClientState ClientState)
        {


            PluginInterface = pluginInterface;
            CommandManager = commandManager;


            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            finderService = new FinderService(Configuration, gameGui, objectTable, dataManager, sigScanner);

            PluginUi = new PluginUI(Configuration, finderService, ClientState);

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens Mini-Mappingway settings"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            ClientState.TerritoryChanged += (i, x) => { NaviMapManager.updateOncePerZone(gameGui); };


        }

        public void Dispose()
        {
            PluginUi.Dispose();
            CommandManager.RemoveHandler(commandName);
        }

        private void OnCommand(string command, string args)
        {
            if (command != null && command == "/mmway")
            {
                PluginUi.SettingsVisible = true;
            }
            // in response to the slash command, just display our main ui

        }

        private void DrawUI()
        {
            PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            PluginUi.SettingsVisible = true;
        }
    }
}
