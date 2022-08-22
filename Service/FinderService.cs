using Dalamud;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MiniMappingWay.Model;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MiniMappingWay.Service
{
    public sealed class FinderService : IDisposable, IServiceType
    {

        private Configuration configuration;
        private readonly GameGui _gameGui;
        private readonly ObjectTable _objectTable;

        public List<GameObject> friends = new List<GameObject>();
        public List<GameObject> fcMembers = new List<GameObject>();
        public NaviMapInfo naviMapInfo = new NaviMapInfo();

        public Vector2 playerPos = new Vector2();
        public bool inCombat = false;

        private SeString FC;


        public FinderService(Configuration configuration, GameGui gameGui, ObjectTable objectTable)
        {
            this.configuration = configuration;
            this._gameGui = gameGui;
            this._objectTable = objectTable;

        }

        public void LookFor()
        {
            if(!this.configuration.showFcMembers && !this.configuration.showFriends)
            {
                return;
            }
            friends.Clear();
            fcMembers.Clear();
            if(_objectTable == null || _objectTable.Length <= 0)
            {
                return;
            }
            try
            {
                var player = _objectTable[0];
                playerPos = new Vector2(player.Position.X, player.Position.Z);
                if(player is Character playerChara)
                {
                    if (playerChara.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat))
                    {
                        return;
                    }
                    FC = playerChara.CompanyTag.ToString();
                }

            }
            catch
            {

            }

            for (var i = 1; i < _objectTable.Length-1; i++)
            {
                var obj = _objectTable[i];

                if (obj == null) { continue; }
                if (!(obj is Character chara))
                {
                    continue;
                }
                //iscasting currently means friend
                if (this.configuration.showFriends)
                {
                    if (chara.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.IsCasting))
                    {
                        friends.Add(obj);
                    }
                }
 
                if (this.configuration.showFcMembers)
                {
                    if(chara.CompanyTag == FC)
                    {
                        fcMembers.Add(obj);
                    }
                }
            }


        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool updateNaviMap()
        {
            var naviMapPtr = this._gameGui.GetAddonByName("_NaviMap", 1);
            if (naviMapPtr == IntPtr.Zero)
            {
                return false;
            }

            unsafe
            {
                var naviMap = (AtkUnitBase*)naviMapPtr;

                var rot = (float*)((nint)naviMap + 0x254);
                var scale = (float*)((nint)naviMap + 0x24C);
                try
                {
                    naviMapInfo.rotation = *rot;
                    naviMapInfo.zoom = *scale;
                }
                catch
                {

                }

                naviMapInfo.X = naviMap->X;
                naviMapInfo.Y = naviMap->Y;
                naviMapInfo.scale = naviMap->Scale;
            }

            return true;
        }


    }
}
