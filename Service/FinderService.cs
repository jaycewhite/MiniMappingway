using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Utility;

namespace MiniMappingway.Service
{
    public sealed class FinderService : IDisposable
    {
        public const string FcMembersKey = "fcmembers";
        public const string FriendKey = "friends";
        private readonly IEnumerable<int> _enumerable;
        private IEnumerator<int> _enumerator;
        private int _index;



        public FinderService()
        {
            ServiceManager.NaviMapManager.AddOrUpdateSource(FcMembersKey, ServiceManager.Configuration.FcColour);
            ServiceManager.NaviMapManager.AddOrUpdateSource(FriendKey, ServiceManager.Configuration.FriendColour);
            ServiceManager.Framework.Update += Iterate;
            //ServiceManager.Framework.Update += CheckStillInObjectTable;


            _enumerable = Enumerable.Range(2, 200).Where(x => x % 2 == 0);
            _enumerator = _enumerable.GetEnumerator();


        }

        private void Iterate(Framework framework)
        {

            CheckNewPeople(_index);
            CheckSamePerson(_index);
            var iteratorValid = _enumerator.MoveNext();
            if (!iteratorValid)
            {
                _enumerator = _enumerable.GetEnumerator();
                _enumerator.MoveNext();

            }
            _index = _enumerator.Current;
        }

        private static void CheckSamePerson(int i)
        {
            ServiceManager.NaviMapManager.PersonDict.TryGetValue(i, out var person);
            if (person == null)
            {
                return;
            }

            if (ServiceManager.ObjectTable[i] == null)
            {
                return;
            }
            if (ServiceManager.ObjectTable[i]?.Name.ToString() != person.Name)
            {
                ServiceManager.NaviMapManager.RemoveFromBag(person.Id);
            }
        }

        private static void CheckNewPeople(int i)
        {
            if (MarkerUtility.ChecksPassed)
            {
                LookFor(i);
            }
        }

        private static void CheckStillInObjectTable(Framework framework)
        {
            if (!MarkerUtility.ChecksPassed)
            {
                return;
            }

            Parallel.ForEach(ServiceManager.NaviMapManager.PersonDict, person =>
            {
                var existsAndCorrectPerson = false;
                foreach (var x in Enumerable.Range(2, 200).Where(x => x % 2 == 0))
                {
                    if (ServiceManager.ObjectTable[x]?.ObjectKind != ObjectKind.Player)
                    {
                        continue;
                    }

                    if (person.Value.Id == ServiceManager.ObjectTable[x]?.ObjectId &&
                        ServiceManager.ObjectTable[x]?.Name.ToString() == person.Value.Name)
                    {
                        existsAndCorrectPerson = true;
                    }
                }

                if (!existsAndCorrectPerson)
                {
                    Dalamud.Logging.PluginLog.Verbose($"Removing person {person.Value.Name}");
                    Dalamud.Logging.PluginLog.Verbose($"old {person.Value.SourceName} {person.Value.Id}");
                    ServiceManager.NaviMapManager.RemoveFromBag(person.Value.Id);
                }


            });
        }

        private static unsafe void LookFor(int i)
        {
            if (!ServiceManager.Configuration.ShowFcMembers && !ServiceManager.Configuration.ShowFriends)
            {
                return;
            }

            byte* fc = null;
            try
            {
                if (ServiceManager.ObjectTable[0] is null)
                {
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
                fc = player->FreeCompanyTag;
            }
            catch
            {
                // ignored
            }

            //Parallel.For(1, ServiceManager.ObjectTable.Length, (i, state) =>

            var alreadyInFriendBag = false;
            var alreadyInFcBag = false;
            var obj = ServiceManager.ObjectTable[i];

            if (obj == null) { return; }

            if (ServiceManager.NaviMapManager.PersonDict
                .Any(x => x.Value.Id == obj.ObjectId && x.Value.SourceName == FriendKey))
            {
                alreadyInFriendBag = true;
            }

            if (ServiceManager.NaviMapManager.PersonDict
                .Any(x => x.Value.Id == obj.ObjectId && x.Value.SourceName == FcMembersKey))
            {
                alreadyInFcBag = true;
            }


            if (alreadyInFcBag && alreadyInFriendBag)
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

            //IsCasting currently means friend
            if (ServiceManager.Configuration.ShowFriends && !alreadyInFriendBag)
            {
                if (((StatusFlags)charPointer->StatusFlags).HasFlag(StatusFlags.IsCasting))
                {
                    var personDetails = new PersonDetails(obj.Name.ToString(), obj.ObjectId, FriendKey, obj.Address);
                    Dalamud.Logging.PluginLog.Verbose("adding person friend");

                    ServiceManager.NaviMapManager.AddToBag(personDetails);

                }
            }

            if (ServiceManager.Configuration.ShowFcMembers && !alreadyInFcBag)
            {
                if (fc == null)
                {
                    return;
                }
                var tempFc = new ReadOnlySpan<byte>(charPointer->FreeCompanyTag, 7);
                var playerFc = new ReadOnlySpan<byte>(fc, 7);
                if (fc->CompareTo(0) == 0)
                {
                    return;
                }
                if (playerFc.SequenceEqual(tempFc))
                {
                    Dalamud.Logging.PluginLog.Verbose("adding person fc");

                    var personDetails = new PersonDetails(obj.Name.ToString(), obj.ObjectId, FcMembersKey, obj.Address);

                    ServiceManager.NaviMapManager.AddToBag(personDetails);
                }
            }



        }



        public void Dispose()
        {
            ServiceManager.Framework.Update -= Iterate;
            //ServiceManager.Framework.Update -= CheckStillInObjectTable;
        }

    }
}
