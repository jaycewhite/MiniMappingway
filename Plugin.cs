using System;
using System.Collections.Concurrent;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using MiniMappingway.Api;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service;
using MiniMappingway.Service.Interface;

namespace MiniMappingway
{
    public sealed class Plugin : IDalamudPlugin
    {
        private static IServiceProvider _serviceProvider;

        

        

        public string Name => "Mini-Mappingway";

        private const string CommandName = "/mmway";
        private const string CommandNameDebug = "/mmwaydebug";


        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<ServiceManager>();

            #region Setup Commands and Actions

            ServiceManager.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens Mini-Mappingway settings"
            });

            ServiceManager.CommandManager.AddHandler(CommandNameDebug, new CommandInfo(OnCommand));
            ServiceManager.DalamudPluginInterface.UiBuilder.Draw += DrawUi;

            ServiceManager.DalamudPluginInterface.UiBuilder.Draw += _serviceProvider.GetService<IWindowService>().Draw;
            ServiceManager.DalamudPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;

            ServiceManager.ClientState.TerritoryChanged += TerritoryChanged;

            #endregion
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IPersonService, PersonService>();
            services.AddSingleton<ISourceService, SourceService>();
            services.AddSingleton<IMapService, MapService>();
            services.AddSingleton<IFinderService, FinderService>();
            services.AddSingleton<IMarkerService, MarkerService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IGameStateService, GameStateService>();
            services.AddSingleton<ClientState>();
            services.AddSingleton<GameGui>();
            services.AddSingleton<ObjectTable>();
            services.AddSingleton<DataManager>();
            services.AddSingleton<DalamudPluginInterface>();
            services.AddSingleton<CommandManager>();
            services.AddSingleton<Framework>();

            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            ServiceManager.CommandManager.RemoveHandler(CommandName);
            ServiceManager.CommandManager.RemoveHandler(CommandNameDebug);
            ServiceManager.DalamudPluginInterface.UiBuilder.Draw -= DrawUi;
            ServiceManager.DalamudPluginInterface.UiBuilder.Draw -= _serviceProvider.GetService<IWindowService>().Draw;
            ServiceManager.DalamudPluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;
            ServiceManager.ClientState.TerritoryChanged -= TerritoryChanged;

        }

        private void TerritoryChanged(object? sender, ushort e)
        {
            _serviceProvider.GetService<IMapService>().UpdateMap();
            var personService = _serviceProvider.GetService<IPersonService>();

            foreach (var dict in personService.PersonDict)
            {
                personService.ClearBag(dict.Key);
            }
        }

        private void OnCommand(string? command, string args)
        {
            var gameStateService = _serviceProvider.GetService<IGameStateService>();
            var windowService = _serviceProvider.GetService<IWindowService>();

            PluginLog.Verbose("Command received");

            if (command != null && command == "/mmway")
            {
                ServiceManager.PluginUi.SettingsVisible = true;
            }
            if (command != null && command == CommandNameDebug)
            {
                gameStateService.DebugMode = !gameStateService.DebugMode;
                if (gameStateService.DebugMode)
                {
                    windowService.NaviMapWindow.Flags &= ~ImGuiWindowFlags.NoBackground;
                }
                else
                {
                    windowService.NaviMapWindow.Flags |= ImGuiWindowFlags.NoBackground;

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
