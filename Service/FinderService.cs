using Dalamud;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using MiniMappingway.Manager;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Types = Dalamud.Game.ClientState.Objects.Types;

namespace MiniMappingWay.Service
{
    public sealed class FinderService : IDisposable, IServiceType
    {

        private readonly Configuration configuration;
        private readonly GameGui _gameGui;
        private readonly ObjectTable _objectTable;
        private readonly SigScanner _sigScanner;

        public List<Types.GameObject> friends = new List<Types.GameObject>();
        public List<Types.GameObject> fcMembers = new List<Types.GameObject>();

        public Vector2 playerPos = new Vector2();
        public bool inCombat = false;





        public FinderService(Configuration configuration, GameGui gameGui, ObjectTable objectTable, DataManager dataManager, SigScanner sigScanner)
        {
            this.configuration = configuration;
            _gameGui = gameGui;
            _objectTable = objectTable;
            _sigScanner = sigScanner;
        }







        public unsafe void LookFor()
        {
            if (!configuration.showFcMembers && !configuration.showFriends)
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
                    if(_objectTable[0] is null) {
                        return;
                    }
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
                    if (configuration.showFriends)
                    {
                        if (((StatusFlags)charPointer->StatusFlags).HasFlag(StatusFlags.IsCasting))
                        {
                            lock (friends)
                            {
                                friends.Add(obj);

                            }
                        }
                    }

                    if (configuration.showFcMembers)
                    {
                        if (FC == null)
                        {
                            return;
                        }
                        var tempFc = new ReadOnlySpan<byte>(charPointer->FreeCompanyTag, 7);
                        var playerFC = new ReadOnlySpan<byte>(FC, 7);
                        char test = " "[0];
                        NaviMapManager.debugValue = (FC->CompareTo(0) == 0).ToString();
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






    }
}
