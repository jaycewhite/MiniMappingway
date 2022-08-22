using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using MiniMappingWay.Service;
using System.IO;

namespace MiniMappingWay
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Mini-Mappingway";

        private const string commandName = "/mmway";


        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private FinderService finderService { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] ObjectTable objectTable)
        {
            

            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.finderService = new FinderService(this.Configuration, gameGui, objectTable);

            this.PluginUi = new PluginUI(this.Configuration, gameGui, finderService);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens Mini-Mappingway settings"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
        }

        private void OnCommand(string command, string args)
        {
            if(command != null && command == "/mmway")
            {
                this.PluginUi.SettingsVisible = true;
            }
            // in response to the slash command, just display our main ui

        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
