﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Utility;

namespace MiniMappingway.Service
{
    public sealed class FinderService : IDisposable
    {
        public const string FcMembersKey = "FC Members";
        public const string FriendKey = "Friends";
        public const string EveryoneKey = "Everyone";
        private readonly IEnumerable<int> _enumerable;
        private IEnumerator<int> _enumerator;
        private int _index;



        public FinderService()
        {
            ServiceManager.NaviMapManager.AddOrUpdateSource(FriendKey, new Vector4(0.957f,0.533f,0.051f,1));
            ServiceManager.NaviMapManager.AddOrUpdateSource(FcMembersKey, new Vector4(1, 0, 0, 1));
            ServiceManager.NaviMapManager.AddOrUpdateSource(EveryoneKey, new Vector4(0, 0.7f, 0.7f, 1));
            ServiceManager.Framework.Update += Iterate;


            _enumerable = Enumerable.Range(2, 200).Where(x => x % 2 == 0);
            _enumerator = _enumerable.GetEnumerator();
            _enumerator.MoveNext();
            _index = _enumerator.Current;




        }

        private void Iterate(IFramework framework)
        {
            unsafe
            {
                AgentMap.Instance()->ResetMiniMapMarkers();
            }
            CheckNewPeople(_index);
            CheckSamePerson(_index);

            var iteratorValid = _enumerator.MoveNext();
            if (!iteratorValid)
            {
                _enumerator = _enumerable.GetEnumerator();
                _enumerator.MoveNext();

            }
            _index = _enumerator.Current;

            foreach (var dict in ServiceManager.NaviMapManager.PersonDict)
            {
                //ServiceManager.Log.Verbose(dict.Value.Count.ToString());

                foreach (var newdict in dict.Value)
                {
                    
                    unsafe
                    {
                        var personObj = ServiceManager.ObjectTable.CreateObjectReference(newdict.Value.Ptr);
                        AgentMap.Instance()->AddMiniMapMarker(new Vector3(personObj.Position.X, 0, personObj.Position.Z), 60260, 60);
                    }

                }
            }
        }

        private static void CheckSamePerson(in int i)
        {
            foreach (var dict in ServiceManager.NaviMapManager.PersonDict)
            {
                if (!ServiceManager.NaviMapManager.SourceDataDict[dict.Key].Enabled)
                {
                    continue;
                }
                dict.Value.TryGetValue(i, out var person);
                if (person == null)
                {
                    continue;
                }
                
                if (ServiceManager.ObjectTable[i] == null)
                {
                    continue;
                }
                var ptr = person.Ptr;
                unsafe
                {
                    var charPointer = (Character*)ptr;
                    if ((byte)charPointer->GameObject.ObjectKind != (byte)ObjectKind.Player)
                    {
                        ServiceManager.NaviMapManager.RemoveFromBag(person.Id, dict.Key);
                        continue;
                    }
                }
                if (ServiceManager.ObjectTable[i]?.Name.ToString() != person.Name)
                {
                    ServiceManager.NaviMapManager.RemoveFromBag(person.Id,dict.Key);
                }
            }
        }
        private static void CheckNewPeople(in int i)
        {
            if (MarkerUtility.RunChecks())
            {
                LookFor(i);
            }
        }

        private static unsafe void LookFor(int i)
        {
            ServiceManager.Configuration.SourceConfigs.TryGetValue(FriendKey, out var friendConfig);
            ServiceManager.Configuration.SourceConfigs.TryGetValue(FcMembersKey, out var fcConfig);
            ServiceManager.Configuration.SourceConfigs.TryGetValue(EveryoneKey, out var everyoneConfig);
            if (fcConfig == null || friendConfig == null || everyoneConfig == null)
            {
                return;
            }
            if (!fcConfig.Enabled && !friendConfig.Enabled && !everyoneConfig.Enabled)
            {
                return;
            }

            Span<byte> fc = null;
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

                ServiceManager.NaviMapManager.InCombat = player->InCombat;
                if (ServiceManager.NaviMapManager.InCombat)
                {
                    return;
                }
                fc = player->FreeCompanyTag;
            }
            catch
            {
                // ignored
            }

            var alreadyInFriendBag = false;
            var alreadyInFcBag = false;
            var obj = ServiceManager.ObjectTable[i];

            if (obj == null) { return; }

            ServiceManager.NaviMapManager.PersonDict.TryGetValue(FriendKey, out var friendDict);
            ServiceManager.NaviMapManager.PersonDict.TryGetValue(FcMembersKey, out var fcDict);

            if (friendDict == null || fcDict == null)
            {
                return;
            }

            if (friendDict.Any(x => x.Value.Id == obj.GameObjectId) && friendConfig.Enabled)
            {
                alreadyInFriendBag = true;
            }

            if (fcDict.Any(x => x.Value.Id == obj.GameObjectId) && fcConfig.Enabled)
            {
                alreadyInFcBag = true;
            }


            if (alreadyInFcBag && alreadyInFriendBag)
            {
                return;
            }

            var ptr = obj.Address;
            var charPointer = (Character*)ptr;
            if ((byte)charPointer->GameObject.ObjectKind != (byte)ObjectKind.Player)
            {
                return;
            }
            if ((byte)charPointer->GameObject.ObjectKind == (byte)ObjectKind.BattleNpc)
            {
                return;
            }

            if (charPointer->IsAllianceMember && charPointer->IsPartyMember)
            {
                return;
            }

            if (friendConfig.Enabled && !alreadyInFriendBag)
            {
                if (charPointer->IsFriend)
                {
                    var personDetails = new PersonDetails(obj.Name.ToString(), obj.GameObjectId, FriendKey, obj.Address);
                    alreadyInFriendBag = true;
                    ServiceManager.NaviMapManager.AddToBag(personDetails);

                }
            }

            if (fcConfig.Enabled && !alreadyInFcBag)
            {
                if (fc == null)
                {
                    return;
                }
                var tempFc = charPointer->FreeCompanyTag;
                var playerFc = fc;
                    if (playerFc.SequenceEqual(tempFc))
                    {

                        var personDetails = new PersonDetails(obj.Name.ToString(), obj.GameObjectId, FcMembersKey, obj.Address);
                        alreadyInFcBag = true;
                        ServiceManager.NaviMapManager.AddToBag(personDetails);
                    }
            }

            if (!alreadyInFcBag && !alreadyInFriendBag && everyoneConfig.Enabled)
            {
                var personDetails = new PersonDetails(obj.Name.ToString(), obj.GameObjectId, EveryoneKey, obj.Address);

                ServiceManager.NaviMapManager.AddToBag(personDetails);

            }


        }

        public void Dispose()
        {
            ServiceManager.Framework.Update -= Iterate;
        }

    }
}
