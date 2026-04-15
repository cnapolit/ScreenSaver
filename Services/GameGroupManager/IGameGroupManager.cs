using ScreenSaver.Models;

namespace ScreenSaver.Services;

internal interface IGameGroupManager
{
    IReadOnlyCollection<GameGroup> GameGroups { get; }
    void CreateGameGroup(GameGroup gameGroup);
    void AddGamesToGroup(GameGroup gameGroup, IEnumerable<string> gameGuids);
    void RenameGameGroup(GameGroup gameGroup, string newGroupName);
    void SetSortField(GameGroup gameGroup, string newFieldName, bool ascending);
    void ToggleGameGroupActiveStatus(GameGroup gameGroup);
    void RemoveGamesFromGroups(IEnumerable<string> gameGuids);
    void RemoveGamesFromGroup(GameGroup gameGroup, IEnumerable<string> gameGuids);
    void DeleteGameGroup(GameGroup gameGroup);
    GameGroup? GetActiveGameGroup();
    void SaveGameGroupChanges();
}
