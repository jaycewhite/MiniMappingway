using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service.Interface;

namespace MiniMappingway.Service
{
    public sealed class FinderService : IDisposable, IFinderService
    {
        private readonly ISourceService _sourceService;
        private readonly IPersonService _personService;
        private readonly IGameStateService _gameStateService;
        private readonly IEnumerable<int> _enumerable;
        private IEnumerator<int> _enumerator;
        private int _index;



        public FinderService(ISourceService sourceService, IPersonService personService, IGameStateService gameStateService)
        {
            _sourceService = sourceService;
            _personService = personService;
            _gameStateService = gameStateService;
            _sourceService.AddOrUpdateSource(IPersonService.FriendKey, new Vector4(0.957f,0.533f,0.051f,1));
            _sourceService.AddOrUpdateSource(IPersonService.FcMembersKey, new Vector4(1, 0, 0, 1));
            _sourceService.AddOrUpdateSource(IPersonService.EveryoneKey, new Vector4(0, 0.7f, 0.7f, 1));
            ServiceManager.Framework.Update += Iterate;


            _enumerable = Enumerable.Range(2, 200).Where(x => x % 2 == 0);
            _enumerator = _enumerable.GetEnumerator();
            _enumerator.MoveNext();
            _index = _enumerator.Current;




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

        private void CheckSamePerson(in int i)
        {
            foreach (var dict in _personService.PersonDict)
            {
                if (!_sourceService.SourceDataDict[dict.Key].Enabled)
                {
                    continue;
                }
                dict.Value.TryGetValue(i, out var person);
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
                    _personService.RemoveFromBag(person.Id,dict.Key);
                }
            }
        }
        private void CheckNewPeople(in int i)
        {
            if (_gameStateService.ChecksPassed)
            {
                LookFor(i);
            }
        }

        private unsafe void LookFor(int i)
        {
            _sourceService.SourceDataDict.TryGetValue(IPersonService.FriendKey, out var friendConfig);
            _sourceService.SourceDataDict.TryGetValue(IPersonService.FcMembersKey, out var fcConfig);
            _sourceService.SourceDataDict.TryGetValue(IPersonService.EveryoneKey, out var everyoneConfig);
            if (fcConfig == null || friendConfig == null || everyoneConfig == null)
            {
                return;
            }
            if (!fcConfig.Enabled && !friendConfig.Enabled && !everyoneConfig.Enabled)
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
                
                if (_gameStateService.InCombat)
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

            _personService.PersonDict.TryGetValue(IPersonService.FriendKey, out var friendDict);
            _personService.PersonDict.TryGetValue(IPersonService.FcMembersKey, out var fcDict);

            if (friendDict == null || fcDict == null)
            {
                return;
            }

            if (friendDict.Any(x => x.Value.Id == obj.ObjectId) && friendConfig.Enabled)
            {
                alreadyInFriendBag = true;
            }

            if (fcDict.Any(x => x.Value.Id == obj.ObjectId) && fcConfig.Enabled)
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

            if (friendConfig.Enabled && !alreadyInFriendBag)
            {
                //IsCasting currently means friend
                if (((StatusFlags)charPointer->StatusFlags).HasFlag(StatusFlags.IsCasting))
                {
                    var personDetails = new PersonDetails(obj.Name.ToString(), obj.ObjectId, IPersonService.FriendKey, obj.Address);
                    alreadyInFriendBag = true;
                    _personService.AddToBag(personDetails);

                }
            }

            if (fcConfig.Enabled && !alreadyInFcBag)
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

                    var personDetails = new PersonDetails(obj.Name.ToString(), obj.ObjectId, IPersonService.FcMembersKey, obj.Address);
                    alreadyInFcBag = true;
                    _personService.AddToBag(personDetails);
                    


                }
            }

            if (!alreadyInFcBag && !alreadyInFriendBag && everyoneConfig.Enabled)
            {
                var personDetails = new PersonDetails(obj.Name.ToString(), obj.ObjectId, IPersonService.EveryoneKey, obj.Address);

                _personService.AddToBag(personDetails);

            }


        }

        public void Dispose()
        {
            ServiceManager.Framework.Update -= Iterate;
            //ServiceManager.Framework.Update -= CheckStillInObjectTable;
        }

    }
}
