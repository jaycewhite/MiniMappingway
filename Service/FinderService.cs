using Dalamud;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using MiniMappingWay.Model;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Types = Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;

using Lumina.Excel;
using Dalamud.Game;

namespace MiniMappingWay.Service
{
    public sealed class FinderService : IDisposable, IServiceType
    {

        private Configuration configuration;
        private readonly GameGui _gameGui;
        private readonly ObjectTable _objectTable;
        private readonly SigScanner _sigScanner;

        public List<Types.GameObject> friends = new List<Types.GameObject>();
        public List<Types.GameObject> fcMembers = new List<Types.GameObject>();
        public NaviMapInfo naviMapInfo = new NaviMapInfo();

        public Vector2 playerPos = new Vector2();
        public bool inCombat = false;

        bool loggedIn;

        IntPtr MapId1;
        IntPtr MapId2;

        ExcelSheet<Map>? Maps;




        public FinderService(Configuration configuration, GameGui gameGui, ObjectTable objectTable, DataManager dataManager, SigScanner sigScanner)
        {
            this.configuration = configuration;
            this._gameGui = gameGui;
            this._objectTable = objectTable;
            this._sigScanner = sigScanner;
            Maps = dataManager.GetExcelSheet<Map>();

            this.MapId2 = _sigScanner.GetStaticAddressFromSig("44 0F 44 3D ?? ?? ?? ??");
            this.MapId1 = _sigScanner.GetStaticAddressFromSig("44 8B 3D ?? ?? ?? ?? 45 85 FF");

            updateMap();



        }

        unsafe uint getMapId()
        {
            try
            {
                return *(uint*)MapId1 == 0 ? *(uint*)MapId2 : *(uint*)MapId1;

            }
            catch
            {
                return 0;
            }
        }

        public void updateMap()
        {
            if (Maps != null)
            {
                var map = Maps.GetRow(getMapId());
                if (map == null) { return; }
                if (map.SizeFactor != 0)
                {
                    naviMapInfo.zoneScale = (float)map.SizeFactor / 100;
                }
                else
                {
                    naviMapInfo.zoneScale = 1;
                }
                naviMapInfo.offsetX = map.OffsetX;
                naviMapInfo.offsetY = map.OffsetY;

            }
        }



        public unsafe void LookFor()
        {
            if (!this.configuration.showFcMembers && !this.configuration.showFriends)
            {
                return;
            }

            friends.Clear();
            fcMembers.Clear();



            byte* FC = null;
            if (_objectTable == null || _objectTable.Length <= 0)
            {
                return;
            }
            try
            {
                
                unsafe
                {
                    
                    var player = (Character*)_objectTable[0].Address;
                    playerPos = new Vector2(player->GameObject.Position.X, player->GameObject.Position.Z);

                        if (((StatusFlags)player->StatusFlags).HasFlag(StatusFlags.InCombat))
                        {
                            return;
                        }
                      FC = player->FreeCompanyTag;

                }


            }
            catch
            {

            }

            Parallel.For(1, _objectTable.Length, (i, state) =>
            {
                var obj = _objectTable[i];

                if (obj == null) { return; }
                unsafe
                {
                    var ptr = obj.Address;
                    var charPointer = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)ptr;
                    if (charPointer->GameObject.ObjectKind != (byte)ObjectKind.Player)
                    {
                        return;
                    }

                    //iscasting currently means friend
                    if (this.configuration.showFriends)
                    {
                        if (((StatusFlags)charPointer->StatusFlags).HasFlag(StatusFlags.IsCasting))
                        {
                            lock (friends)
                            {
                                friends.Add(obj);

                            }
                        }
                    }

                    if (this.configuration.showFcMembers)
                    {
                        if(FC == null)
                        {
                            return;
                        }
                        var tempFc = new ReadOnlySpan<byte>(charPointer->FreeCompanyTag, 7);
                        var playerFC = new ReadOnlySpan<byte>(FC, 7);
                        char test = " "[0];
                        naviMapInfo.debugValue = (FC->CompareTo(0) == 0).ToString();
                        if (FC->CompareTo(0) == 0)
                        {
                            return;
                        }
                        if (playerFC.SequenceEqual(tempFc))
                        {
                            lock (fcMembers)
                            {
                                fcMembers.Add(obj);
                            }
                        }
                    }
                }
            });

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool updateNaviMap()
        {
            unsafe
            {
                var titleCard = (AtkUnitBase*)_gameGui.GetAddonByName("_LocationTitle", 1);
                var blackScreen = (AtkUnitBase*)_gameGui.GetAddonByName("FadeMiddle", 1);
                naviMapInfo.loading = titleCard != null && titleCard->IsVisible || blackScreen != null && blackScreen->IsVisible;
            }

            var naviMapPtr = this._gameGui.GetAddonByName("_NaviMap", 1);
            if (naviMapPtr == IntPtr.Zero)
            {
                return false;
            }

            unsafe
            {
                var naviMap = (AtkUnitBase*)naviMapPtr;

                var rot = (float*)((nint)naviMap + 0x254);
                var naviScale = (float*)((nint)naviMap + 0x24C);
                if(naviMap->UldManager.LoadedState != AtkLoadState.Loaded)
                {
                    return false;
                }
                try
                {
                    naviMapInfo.rotation = *rot;
                    naviMapInfo.zoom = *naviScale;
                }
                catch
                {

                }

                naviMapInfo.X = naviMap->X;
                naviMapInfo.Y = naviMap->Y;
                naviMapInfo.naviScale = naviMap->Scale;
                naviMapInfo.visible = naviMap->IsVisible;
            }

            return true;
        }


    }
}
