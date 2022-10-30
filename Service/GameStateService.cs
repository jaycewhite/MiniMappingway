using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MiniMappingway.Manager;
using MiniMappingway.Service.Interface;

namespace MiniMappingway.Service
{
    internal class GameStateService : IGameStateService
    {

        public bool DebugMode { get; set; }

        public bool IsLoading => CheckIfLoading();

        public bool IsLoggedIn => _clientState.IsLoggedIn;

        public PlayerCharacter? Player => _clientState.LocalPlayer;

        public bool ChecksPassed { get; set; }

        public bool InCombat
        {
            get
            {
                if (Player == null)
                {
                    return false;
                }
                return ((StatusFlags)Player.StatusFlags).HasFlag(StatusFlags.InCombat);
            }
        }

        private readonly GameGui _gameGui;
        private readonly ClientState _clientState;
        private readonly IMapService _mapService;
        private readonly IConfigurationService _configurationService;

        public GameStateService(GameGui gameGui, ClientState clientState, IMapService mapService, IConfigurationService configurationService)
        {
            _gameGui = gameGui;
            _clientState = clientState;
            _mapService = mapService;
            _configurationService = configurationService;
        }

        private unsafe bool CheckIfLoading()
        {
            var locationTitle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("_LocationTitle", 1);
            var fadeMiddle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("FadeMiddle", 1);
            return locationTitle->IsVisible ||
                   fadeMiddle->IsVisible;
        }

        public bool RunChecks()
        {
            if (!IsLoggedIn
                || !_configurationService.GetConfiguration().Enabled)
            {
                return false;
            }
            if (!_mapService.UpdateNaviMap())
            {
                ChecksPassed = false;
                return false;
            }

            if (!_mapService.Data.Visible)
            {
                return false;
            }
            if (_mapService.IsLoading)
            {
                return false;
            }

            unsafe
            {
                var player = (Character*)ServiceManager.ObjectTable[0]?.Address;
                if (player == null)
                {
                    return false;
                }
            }
            ChecksPassed = true;
            return true;
        }

    }
}
