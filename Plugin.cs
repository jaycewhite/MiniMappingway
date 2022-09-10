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
using ImGuiNET;

namespace MiniMappingway
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Mini-Mappingway";

        private const string commandName = "/mmway";
        private const string commandNameDebug = "/mmwaydebug";


        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<ServiceManager>();

            ServiceManager.Configuration = ServiceManager.DalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            ServiceManager.Configuration.Initialize(ServiceManager.DalamudPluginInterface);

            #region Initialise Managers

            ServiceManager.NaviMapManager = new();
            ServiceManager.PluginUi = new();
            ServiceManager.WindowManager = new();
            ServiceManager.FcManager = new();

            #endregion

            #region Initialise Services

            ServiceManager.FinderService = new();

            #endregion

            ServiceManager.WindowManager.AddWindowsToWindowSystem();

            #region Setup Commands and Actions

            ServiceManager.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens Mini-Mappingway settings"
            });

            ServiceManager.CommandManager.AddHandler(commandNameDebug, new CommandInfo(OnCommand)
            {
                
            });
            ServiceManager.DalamudPluginInterface.UiBuilder.Draw += DrawUI;

            ServiceManager.DalamudPluginInterface.UiBuilder.Draw += ServiceManager.WindowSystem.Draw;
            ServiceManager.DalamudPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            ServiceManager.ClientState.TerritoryChanged += (i, x) => { ServiceManager.NaviMapManager.updateMap(); };

            ServiceManager.Framework.Update += (i) => { ServiceManager.FcManager.LoadFcMembers(); };

            #endregion
        }

        public void Dispose()
        {
            ServiceManager.CommandManager.RemoveHandler(commandName);
            ServiceManager.CommandManager.RemoveHandler(commandNameDebug);
        }

        private void OnCommand(string command, string args)
        {
            Dalamud.Logging.PluginLog.Verbose("Command received");

            if (command != null && command == "/mmway")
            {
                ServiceManager.PluginUi.SettingsVisible = true;
            }
            if (command != null && command == commandNameDebug)
            {
                ServiceManager.NaviMapManager.debugMode = !ServiceManager.NaviMapManager.debugMode;
                if (ServiceManager.NaviMapManager.debugMode)
                {
                    ServiceManager.WindowManager.naviMapWindow.Flags &= ~ImGuiWindowFlags.NoBackground;
                }
                else
                {
                    ServiceManager.WindowManager.naviMapWindow.Flags |= ImGuiWindowFlags.NoBackground;

                }
            }
            // in response to the slash command, just display our main ui

        }

        private void DrawUI()
        {
            ServiceManager.PluginUi.DrawSettingsWindow();
        }
        private void DrawConfigUI()
        {
            Dalamud.Logging.PluginLog.Verbose("Draw config ui on");
            ServiceManager.PluginUi.SettingsVisible = true;
        }
    }
}
