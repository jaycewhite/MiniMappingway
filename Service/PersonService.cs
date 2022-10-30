using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using MiniMappingway.Model;
using MiniMappingway.Service.Interface;
namespace MiniMappingway.Service
{
    internal class PersonService : IPersonService
    {
        private readonly ObjectTable _objectTable;

        public PersonService(ObjectTable objectTable)
        {
            _objectTable = objectTable;
        }

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, PersonDetails>> _personDict = new();

        public bool InCombat { get; set; }

        public ConcurrentDictionary<string, ConcurrentDictionary<int, PersonDetails>> PersonDict => _personDict;

        public bool ClearBag(string sourceName)
        {
            _personDict.TryGetValue(sourceName, out var dict);
            if (dict == null)
            {
                return false;
            }
            dict.Clear();
            return true;
        }
        public void InitialiseBag(string sourceName)
        {
            if (_personDict.ContainsKey(sourceName))
            {
                return;
            }
            _personDict.AddOrUpdate(sourceName, new ConcurrentDictionary<int, PersonDetails>(),
                (_, _) => new ConcurrentDictionary<int, PersonDetails>());
        }

        public bool AddToBag(PersonDetails details)
        {
            PersonDict.TryGetValue(details.SourceName, out var dict);

            if (dict == null)
            {
                return false;
            }

            var personIndex = GetObjIndexById(details.Id);
            if (personIndex == null)
            {
                return false;
            }
            return dict.TryAdd((int)personIndex, details);
        }

        public bool OverwriteWholeBag(string sourceName, List<PersonDetails> list)
        {
            ClearBag(sourceName);

            var success = true;

            _personDict.TryGetValue(sourceName, out var dict);

            if (dict == null)
            {
                return false;
            }

            foreach (var person in list)
            {
                var personIndex = GetObjIndexById(person.Id);

                if (personIndex == null)
                {
                    continue;
                }
                if (!dict.TryAdd((int)personIndex, person))
                {
                    success = false;
                }
            }
            return success;
        }



        public bool RemoveFromBag(uint id, string sourceName)
        {
            _personDict.TryGetValue(sourceName, out var dict);
            if (dict == null)
            {
                return false;
            }
            var entry = dict.First(x => x.Value.Id == id);
            return dict.TryRemove(entry);

        }

        public bool RemoveFromBag(string name, string sourceName)
        {
            _personDict.TryGetValue(sourceName, out var dict);
            if (dict == null)
            {
                return false;
            }
            var entry = dict.First(x => x.Value.Name == name);
            return dict.TryRemove(entry);
        }


        public void Dispose()
        {
            _personDict.Clear();
        }

        private int? GetObjIndexById(uint objId)
        {
            foreach (var x in Enumerable.Range(2, 200).Where(x => x % 2 == 0))
            {
                if (_objectTable[x] != null && _objectTable[x]?.ObjectId == objId)
                {
                    return x;
                }
            }

            return null;
        }
    }
}
