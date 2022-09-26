using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using MiniMappingway.Api;
using MiniMappingway.Manager;
using MiniMappingway.Service;

namespace MiniMappingway
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Mini-Mappingway";

        private const string CommandName = "/mmway";
        private const string CommandNameDebug = "/mmwaydebug";


        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<ServiceManager>();

            ServiceManager.Configuration = ServiceManager.DalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            ServiceManager.Configuration.Initialize();

            #region Initialise Managers

            ServiceManager.NaviMapManager = new NaviMapManager();
            ServiceManager.PluginUi = new PluginUi();
            ServiceManager.WindowManager = new WindowManager();
            ServiceManager.ApiController = new ApiController();

            #endregion

            #region Initialise Services

            ServiceManager.FinderService = new FinderService();

            #endregion

            ServiceManager.WindowManager.AddWindowsToWindowSystem();

            #region Setup Commands and Actions

            ServiceManager.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens Mini-Mappingway settings"
            });

            ServiceManager.CommandManager.AddHandler(CommandNameDebug, new CommandInfo(OnCommand));
            ServiceManager.DalamudPluginInterface.UiBuilder.Draw += DrawUi;

            ServiceManager.DalamudPluginInterface.UiBuilder.Draw += ServiceManager.WindowSystem.Draw;
            ServiceManager.DalamudPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;

            ServiceManager.ClientState.TerritoryChanged += TerritoryChanged;

            #endregion
        }

        public void Dispose()
        {
            ServiceManager.CommandManager.RemoveHandler(CommandName);
            ServiceManager.CommandManager.RemoveHandler(CommandNameDebug);
            ServiceManager.DalamudPluginInterface.UiBuilder.Draw -= DrawUi;
            ServiceManager.DalamudPluginInterface.UiBuilder.Draw -= ServiceManager.WindowSystem.Draw;
            ServiceManager.DalamudPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;
            ServiceManager.ClientState.TerritoryChanged -= TerritoryChanged;
            ServiceManager.Dispose();

        }

        private void TerritoryChanged(object? sender, ushort e)
        {
            ServiceManager.NaviMapManager.UpdateMap();

            foreach (var dict in ServiceManager.NaviMapManager.PersonDict)
            {
                ServiceManager.NaviMapManager.ClearPersonBag(dict.Key);
            }
        }

        private void OnCommand(string? command, string args)
        {
            PluginLog.Verbose("Command received");

            if (command != null && command == "/mmway")
            {
                ServiceManager.PluginUi.SettingsVisible = true;
            }
            if (command != null && command == CommandNameDebug)
            {
                ServiceManager.NaviMapManager.DebugMode = !ServiceManager.NaviMapManager.DebugMode;
                if (ServiceManager.NaviMapManager.DebugMode)
                {
                    ServiceManager.WindowManager.NaviMapWindow.Flags &= ~ImGuiWindowFlags.NoBackground;
                }
                else
                {
                    ServiceManager.WindowManager.NaviMapWindow.Flags |= ImGuiWindowFlags.NoBackground;

                }
            }
            // in response to the slash command, just display our main ui

        }

        private void DrawUi()
        {
            ServiceManager.PluginUi.DrawSettingsWindow();
        }
        private void DrawConfigUi()
        {
            PluginLog.Verbose("Draw config ui on");
            ServiceManager.PluginUi.SettingsVisible = true;
        }
    }
}
