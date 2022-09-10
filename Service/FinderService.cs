using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Types = Dalamud.Game.ClientState.Objects.Types;

namespace MiniMappingway.Service
{
    public sealed class FinderService : IDisposable
    {
        //public List<Types.GameObject> friends = new List<Types.GameObject>();
        //public List<Types.GameObject> fcMembers = new List<Types.GameObject>();

        const string FCMembersKey = "fcmembers";
        const string friendKey = "friends";

        public Vector2 playerPos = new Vector2();
        public bool inCombat = false;

        public FinderService()
        {
            ServiceManager.NaviMapManager.AddOrUpdateSource(FCMembersKey, new SourceData(ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.fcColour)));
            ServiceManager.NaviMapManager.AddOrUpdateSource(friendKey, new SourceData(ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.friendColour)));
        }

        public unsafe void LookFor()
        {
            if (!ServiceManager.Configuration.showFcMembers && !ServiceManager.Configuration.showFriends)
            {
                return;
            }

            byte* FC = null;
            if (ServiceManager.ObjectTable == null || ServiceManager.ObjectTable.Length <= 0)
            {
                return;
            }
            try
            {

                unsafe
                {
                    if(ServiceManager.ObjectTable[0] is null) {
                        return;
                    }
                    var player = (Character*)ServiceManager.ObjectTable[0]?.Address;
                    if (player == null)
                    {
                        return;
                    }

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

            Parallel.For(1, ServiceManager.ObjectTable.Length, (i, state) =>
            {
                bool alreadyInFriendBag = false;
                bool alreadyInFcbag = false;
                var obj = ServiceManager.ObjectTable[i];

                if (obj == null) { return; }
                unsafe
                {
                    if(ServiceManager.NaviMapManager.personListsDict.TryGetValue(friendKey,out var friendBag))
                    {
                        if (friendBag.Contains(obj.Address))
                        {
                            alreadyInFriendBag = true;
                        }
                    }

                    if (ServiceManager.NaviMapManager.personListsDict.TryGetValue(FCMembersKey, out var fCBag))
                    {
                        if (fCBag.Contains(obj.Address))
                        {
                            alreadyInFcbag = true;
                        }
                    }

                    if(alreadyInFcbag && alreadyInFriendBag)
                    {
                        return;
                    }

                    var ptr = obj.Address;
                    var charPointer = (Character*)ptr;
                    if (charPointer->GameObject.ObjectKind != (byte)ObjectKind.Player)
                    {
                        return;
                    }
                    if (((StatusFlags)charPointer->StatusFlags).HasFlag(StatusFlags.AllianceMember))
                    {
                        return;
                    }
                    
                    //iscasting currently means friend
                    if (ServiceManager.Configuration.showFriends && !alreadyInFriendBag)
                    {


                        if (((StatusFlags)charPointer->StatusFlags).HasFlag(StatusFlags.IsCasting))
                        {
                            ServiceManager.NaviMapManager.AddToBag("friends", obj.Address);

                        }
                    }

                    if (ServiceManager.Configuration.showFcMembers && !alreadyInFcbag)
                    {
                        if (FC == null)
                        {
                            return;
                        }
                        var tempFc = new ReadOnlySpan<byte>(charPointer->FreeCompanyTag, 7);
                        var playerFC = new ReadOnlySpan<byte>(FC, 7);
                        if (FC->CompareTo(0) == 0)
                        {
                            return;
                        }
                        if (playerFC.SequenceEqual(tempFc))
                        {
                            ServiceManager.NaviMapManager.AddToBag("fc", obj.Address);
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
