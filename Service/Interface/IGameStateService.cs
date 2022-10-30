using Dalamud.Game.ClientState.Objects.SubKinds;

namespace MiniMappingway.Service.Interface;

public interface IGameStateService
{
    bool DebugMode { get; set; }
    bool IsLoading { get; }
    bool IsLoggedIn { get; }
    PlayerCharacter? Player { get; }
    bool ChecksPassed { get; set; }
    bool InCombat { get; }
    bool RunChecks();
}