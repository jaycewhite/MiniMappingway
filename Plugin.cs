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

namespace MiniMappingway
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Mini-Mappingway";

        private const string commandName = "/mmway";


        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<ServiceManager>();

            ServiceManager.Configuration = ServiceManager.DalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            #region Initialise Managers

            ServiceManager.NaviMapManager = new();
            ServiceManager.PluginUi = new();
            ServiceManager.WindowManager = new();

            #endregion

            #region Initialise Services

            ServiceManager.FinderService = new();

            #endregion

            #region Setup Commands and Actions

            ServiceManager.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens Mini-Mappingway settings"
            });

            ServiceManager.DalamudPluginInterface.UiBuilder.Draw += DrawUI;
            ServiceManager.DalamudPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            ServiceManager.ClientState.TerritoryChanged += (i, x) => { ServiceManager.NaviMapManager.updateOncePerZone(); };

            #endregion
        }

        public void Dispose()
        {
            ServiceManager.CommandManager.RemoveHandler(commandName);
        }

        private void OnCommand(string command, string args)
        {
            if (command != null && command == "/mmway")
            {
                ServiceManager.PluginUi.SettingsVisible = true;
            }
            // in response to the slash command, just display our main ui

        }

        private void DrawUI()
        {
            ServiceManager.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            ServiceManager.PluginUi.SettingsVisible = true;
        }
    }
}
