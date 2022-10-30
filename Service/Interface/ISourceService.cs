using System.Collections.Concurrent;
using System.Numerics;
using MiniMappingway.Model;

namespace MiniMappingway.Service.Interface;

public interface ISourceService
{
    bool AddOrUpdateSource(string sourceName, Vector4 colour);
    bool RemoveSourceAndPeople(string sourceName);
    int GetNextFreePriority();
    bool AddOrUpdateSource(string sourceName, uint colour);
    ConcurrentDictionary<string, SourceData> SourceDataDict { get; set; }
}