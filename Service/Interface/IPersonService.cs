using System.Collections.Concurrent;
using System.Collections.Generic;
using MiniMappingway.Model;

namespace MiniMappingway.Service.Interface;

public interface IPersonService
{
    bool ClearBag(string sourceName);
    bool OverwriteWholeBag(string sourceName, List<PersonDetails> list);
    bool RemoveFromBag(uint id, string sourceName);
    bool RemoveFromBag(string name, string sourceName);
    void Dispose();
    ConcurrentDictionary<string, ConcurrentDictionary<int, PersonDetails>> PersonDict { get; }
    public const string FcMembersKey = "FC Members";
    public const string FriendKey = "Friends";
    public const string EveryoneKey = "Everyone";
    bool InCombat { get; set; }
    void InitialiseBag(string sourceName);
    bool AddToBag(PersonDetails details);
}