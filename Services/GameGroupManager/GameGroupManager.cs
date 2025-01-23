using Playnite.SDK;
using ScreenSaver.Common.Constants;
using ScreenSaver.Common.Extensions;
using ScreenSaver.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace ScreenSaver.Services
{
    internal class GameGroupManager : IGameGroupManager
    {
        #region Infrastructure

        private static readonly ILogger               logger = LogManager.GetLogger();
        private        readonly string             _dataPath;
        private        readonly IList<GameGroup> _gameGroups;

        public GameGroupManager(string extensionsDataPath)
        {
            _dataPath = Path.Combine(extensionsDataPath, App.Id, Files.Data);
            if (File.Exists(_dataPath))
            {
                _gameGroups = JsonSerializer.Deserialize<IList<GameGroup>>(File.ReadAllText(_dataPath));
            }

            if (_gameGroups is null)
            {
                _gameGroups = new List<GameGroup>();
            }

            _readonlyGameGroups = new ReadOnlyCollection<GameGroup>(_gameGroups);
        }

        #endregion

        #region Interface

        private readonly IReadOnlyCollection<GameGroup> _readonlyGameGroups;
        public IReadOnlyCollection<GameGroup> GameGroups { get => _readonlyGameGroups; }
        public void      CreateGameGroup             (GameGroup         gameGroup                                ) => CreateGroup             (gameGroup              );
        public void      AddGamesToGroup             (GameGroup         gameGroup, IEnumerable<Guid>    gameGuids) => AddGames                (gameGroup,    gameGuids);
        public void      RenameGameGroup             (GameGroup         gameGroup, string            newGroupName) => RenameGroup             (gameGroup, newGroupName);
        public void      SetSortField                (GameGroup         gameGroup, string            newSortField, bool ascending) => SetSort (gameGroup, newSortField, ascending);
        public void      ToggleGameGroupActiveStatus (GameGroup         gameGroup                                ) => ToggleGroupActiveStatus (gameGroup              );
        public void      RemoveGamesFromGroups       (IEnumerable<Guid> gameGuids                                ) => RemoveFromGroups        (gameGuids              );
        public void      RemoveGamesFromGroup        (GameGroup         gameGroup, IEnumerable<Guid>    gameGuids) => RemoveFromGroup         (gameGroup,    gameGuids);
        public void      DeleteGameGroup             (GameGroup         gameGroup                                ) => DeleteGroup             (gameGroup              );
        public GameGroup GetActiveGameGroup          (                                                           ) => GetCurrentGroup         (                       );
        public void      SaveGameGroupChanges        (                                                           ) => SaveGameGroups          (                       );

        #endregion

        #region Implementation

        #region CreateGameGroup

        private void CreateGroup(GameGroup gameGroup)
        {
            ValidateGroupName(gameGroup.Name);

            if (gameGroup.IsActive)
            {
                DisableActiveGameGroup();
            }

            _gameGroups.Add(gameGroup);

            SaveGameGroups();
        }

        #endregion

        #region AddGamesToGroup

        private void AddGames(GameGroup gameGroup, IEnumerable<Guid> gameGuids)
        {
            if (gameGuids.ForAny(gameGroup.GameGuids.Add))
            {
                SaveGameGroupChanges();
            }
        }

        #endregion

        #region RenameGameGroup

        private void RenameGroup(GameGroup gameGroup, string newGroupName)
        {
            ValidateGroupName(newGroupName);
            gameGroup.Name = newGroupName;
            SaveGameGroupChanges();
        }

        #endregion

        #region SetSortField

        private void SetSort(GameGroup gameGroup, string newSortField, bool ascending)
        {
            gameGroup.SortField = newSortField;
            gameGroup.Ascending = ascending;
            SaveGameGroupChanges();
        }

        #endregion

        #region SetGameGroupAsActive

        private void ToggleGroupActiveStatus(GameGroup gameGroup)
        {
            if (!gameGroup.IsActive)
            {
                var currentActiveGroup = _gameGroups.FirstOrDefault(g => g.IsActive);
                if (currentActiveGroup != null)
                {
                    currentActiveGroup.IsActive = false;
                }
            }

            gameGroup.IsActive = !gameGroup.IsActive;
            SaveGameGroupChanges();
        }

        #endregion

        #region RemoveGamesFromGroups

        private void RemoveFromGroups(IEnumerable<Guid> gameGuids)
        {
            if (_gameGroups.ForAny(gr => RemoveGuidsFromGroup(gr, gameGuids)))
            {
                SaveGameGroups();
            }
        }

        #endregion

        #region RemoveGamesFromGroup

        private void RemoveFromGroup(GameGroup gameGroup, IEnumerable<Guid> gameGuids)
        {
            if (gameGuids.ForAny(gameGroup.GameGuids.Remove))
            {
                SaveGameGroupChanges();
            }
        }

        #endregion

        #region DeleteGameGroup

        private void DeleteGroup(GameGroup gameGroup)
        {
            if(_gameGroups.Remove(gameGroup))
            {
                SaveGameGroupChanges();
            }
        }

        #endregion

        #region GetActiveGameGroup

        private GameGroup GetCurrentGroup() => _gameGroups.FirstOrDefault(g => g.IsActive);

        #endregion

        #region Helpers

        private void DisableActiveGameGroup()
        {
            var currentActiveGroup = _gameGroups.FirstOrDefault(g => g.IsActive);
            if (currentActiveGroup != null)
            {
                currentActiveGroup.IsActive = false;
            }
        }

        private void ValidateGroupName(string groupName)
        {
            if (_gameGroups.Any(g => g.Name == groupName))
            {
                throw new Exception(Resource.MAIN_MENU_GROUP_NAME_CONFLICT);
            }
        }

        private static bool RemoveGuidsFromGroup(GameGroup group, IEnumerable<Guid> guids)
        {
            var result = group.GameGuids != null && guids.ForAny(group.GameGuids.Remove);
            if (result && group.GameGuids.Count() is 0)
            {
                group.IsActive = false;
            }
            return result;
        }

        private void SaveGameGroups()
        {
            var groupsJson = JsonSerializer.Serialize(_gameGroups);

            try
            {
                File.WriteAllText(_dataPath, groupsJson);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to update game groups data file with JSON: {groupsJson}");
                throw new Exception(Resource.GROUP_SAVE_FAIL);
            }
        }

        #endregion

        #endregion
    }
}
