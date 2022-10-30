using MiniMappingway.Model;

namespace MiniMappingway.Service.Interface;

public interface IMapService
{
    MapData Data { get; }
    bool IsLoading { get; }
    unsafe bool UpdateNaviMap();
    void UpdateMap();
    void Dispose();
}