using MiniMappingway.Window;

namespace MiniMappingway.Service.Interface;

public interface IWindowService
{
    void AddWindowsToWindowSystem();
    void Dispose();
    NaviMapWindow NaviMapWindow { get; }
    void Draw();
}