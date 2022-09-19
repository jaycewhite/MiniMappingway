using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using MiniMappingway.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniMappingway.Service
{
    public sealed class FinderService : IDisposable
    {
        const string FCMembersKey = "fcmembers";
        const string friendKey = "friends";

        public bool inCombat = false;

        private readonly CancellationTokenSource cancellationToken = new();

        public FinderService()
        {
            ServiceManager.NaviMapManager.AddOrUpdateSource(FCMembersKey, ServiceManager.Configuration.fcColour);
            ServiceManager.NaviMapManager.AddOrUpdateSource(friendKey, ServiceManager.Configuration.friendColour);
            CheckNewPeople();
            CheckStillInObjectTable();
        }

        private void CheckNewPeople()
        {
            ServiceManager.Framework.RunOnTick(CheckNewPeople, TimeSpan.FromSeconds(0.5), cancellationToken: cancellationToken.Token);

            if (ServiceManager.WindowManager.naviMapWindow.checksPassed)
            {
                Task.Run(() => { LookFor(); });
            }
        }

        private void CheckStillInObjectTable()
        {
            ServiceManager.Framework.RunOnTick(CheckStillInObjectTable, TimeSpan.FromSeconds(0.5), cancellationToken: cancellationToken.Token);

            Task.Run(() =>
            {
                foreach (var personList in ServiceManager.NaviMapManager.personListsDict)
                {
                    foreach (var person in personList.Value)
                    {
                        var existsAndCorrectPerson = ServiceManager.ObjectTable.Any(x => x.ObjectId == person.Value && x.Name.ToString() == person.Key);
                        if (!existsAndCorrectPerson)
                        {
                            ServiceManager.NaviMapManager.RemoveFromBag(personList.Key, person.Value);
                        }
                    }
                }
            });

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

            //Parallel.For(1, ServiceManager.ObjectTable.Length, (i, state) =>
            var iterator = Enumerable.Range(2, 200).Where(x => x % 2 == 0);
            Parallel.ForEach(iterator, i =>
            {
                bool alreadyInFriendBag = false;
                bool alreadyInFcbag = false;
                var obj = ServiceManager.ObjectTable[i];

                if (obj == null) { return; }
                unsafe
                {
                    if(ServiceManager.NaviMapManager.personListsDict.TryGetValue(friendKey,out var friendBag))
                    {
                        if (friendBag.Any(x => x.Value == obj.ObjectId))
                        {
                            alreadyInFriendBag = true;
                        }
                    }

                    if (ServiceManager.NaviMapManager.personListsDict.TryGetValue(FCMembersKey, out var fCBag))
                    {
                        if (fCBag.Any(x => x.Value == obj.ObjectId))
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
                            var personDetails = new PersonDetails(obj.Name.ToString(), obj.ObjectId);

                            ServiceManager.NaviMapManager.AddToBag("friends", personDetails);

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
                            var personDetails = new PersonDetails(obj.Name.ToString(), obj.ObjectId);

                            ServiceManager.NaviMapManager.AddToBag("fc", personDetails);
                        }
                    }
                }
            });

            

        }



        public void Dispose()
        {
            cancellationToken.Cancel();
        }

    }
}
