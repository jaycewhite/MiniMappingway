using System.Collections.Concurrent;
using System.Collections.Generic;
using MiniMappingway.Model;

namespace MiniMappingway.Service.Interface;

public interface IMarkerService
{
    void CalculateMarkers();
    ConcurrentDictionary<int, Queue<Marker>> MarkerData { get; set; }
}