using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using ScreenSaver.Common.Constants;
using ScreenSaver.Common.Extensions;
using ScreenSaver.Models;
using ScreenSaver.Services.State.ScreenSaver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;

namespace ScreenSaver.Services.UI.Menus
{
    internal class MenuManager : IMenuManager
    {
        #region Infrastructure

        #region Variables

        private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string IconPath     = Path.Combine(PluginFolder, Files.Icon);

        private readonly IPlayniteAPI _playniteAPI;
        private readonly IGameGroupManager _gameGroupManager;
        private readonly IScreenSaverManager _screenSaverManager;

        private readonly List<GameMenuItem> _gameMenuItems;
        private readonly List<MainMenuItem> _mainMenuItems;

        #endregion

        public MenuManager(IPlayniteAPI playniteAPI, IGameGroupManager gameGroupManager, IScreenSaverManager screenSaverManager)
        {
            Localization.SetPluginLanguage(PluginFolder, playniteAPI.ApplicationSettings.Language);
            _playniteAPI        =        playniteAPI;
            _gameGroupManager   =   gameGroupManager;
            _screenSaverManager = screenSaverManager;

            _gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Action = OpenScreenSaver,
                    Description = Resource.GAME_MENU_PREVIEW,
                    MenuSection = App.Name,
                    Icon = IconPath
                },
                new GameMenuItem
                {
                    Action = CreateGameGroup,
                    Description = Resource.GAME_MENU_CREATE_SELECTED,
                    MenuSection = App.Name
                }
            };

            _mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Action = ManuallyStartScreenSaver,
                    Description = Resource.MAIN_MENU_START,
                    MenuSection = "@" + App.Name,
                    Icon = IconPath
                },
                new MainMenuItem
                {
                    Action = CreateDynamicGameGroup,
                    Description = Resource.MAIN_MENU_DYNAMIC,
                    MenuSection = "@" + App.Name
                }
            };
        }

        #endregion

        #region Interface

        public IEnumerable<GameMenuItem> GetGameMenuItems() => GetGameItems();
        public IEnumerable<MainMenuItem> GetMainMenuItems() => GetMainItems();

        #endregion

        #region Implementation

        #region Game Menu

        private IList<GameMenuItem> GetGameItems()
        {
            var gameMenuItems = new List<GameMenuItem>(_gameMenuItems);

            var baseMenuSection = $"{App.Name}|{Resource.MENU_GROUPS}|";

            if (_gameGroupManager.GameGroups.Any())
            {
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = App.Name,
                    Description = "-"
                });
            }

            foreach (var gameGroup in _gameGroupManager.GameGroups.OrderBy(g => g.Name))
            {
                var groupMenuSection = $"{App.Name}|{GetDisplayName(gameGroup)}";

                gameMenuItems.Add(new GameMenuItem
                {
                    Action = _ => _gameGroupManager.AddGamesToGroup(gameGroup, SelectedGuids),
                    MenuSection = groupMenuSection,
                    Description = Resource.GAME_MENU_ADD_SELECTED
                });
                gameMenuItems.Add(new GameMenuItem
                {
                    Action = _ => _gameGroupManager.RemoveGamesFromGroup(gameGroup, SelectedGuids),
                    MenuSection = groupMenuSection,
                    Description = Resource.GAME_MENU_REMOVE_SELECTED
                });
            }

            return gameMenuItems;
        }

        private void OpenScreenSaver(GameMenuItemActionArgs args)
        {
            if (SelectedGuids.Count() != 1) throw new Exception(Resource.GAME_MENU_PREVIEW_ERROR);

            _screenSaverManager.PreviewScreenSaver(SelectedGames.First());
        }

        private void CreateGameGroup(object _)
        {
            var name = PromptForGroupName(string.Empty);
            if (name is null)
            {
                return;
            }

            var sortField = PromptForSortingField();
            if (sortField is null)
            {
                return;
            }

            var group = new GameGroup
            { 
                Name = name,
                IsActive = GetBoolFromYesNoDialog(Resource.GAME_MENU_PROMPT_ACTIVE), 
                GameGuids = SelectedGuids.ToHashSet(),
                SortField = sortField,
                Ascending = PromptForAscending(sortField)
            };

            _gameGroupManager.CreateGameGroup(group);
        }

        private static readonly List<GenericItemOption> SortableFields = new List<GenericItemOption>
        {
            new GenericItemOption(            "None", string.Empty),
            new GenericItemOption(           "Added", string.Empty),
            new GenericItemOption(      "Categories", string.Empty),
            new GenericItemOption(  "CommunityScore", string.Empty),
            new GenericItemOption("CompletionStatus", string.Empty),
            new GenericItemOption(     "CriticScore", string.Empty),
            new GenericItemOption(        "Favorite", string.Empty),
            new GenericItemOption(          "Genres", string.Empty),
            new GenericItemOption(     "IsInstalled", string.Empty),
            new GenericItemOption(    "LastActivity", string.Empty),
            new GenericItemOption(        "Modified", string.Empty),
            new GenericItemOption(        "Playtime", string.Empty),
            new GenericItemOption(       "Platforms", string.Empty),
            new GenericItemOption(          "Random", string.Empty),
            new GenericItemOption(     "ReleaseDate", string.Empty),
            new GenericItemOption(          "Series", string.Empty),
            new GenericItemOption(     "SortingName", string.Empty),
            new GenericItemOption(          "Source", string.Empty),
            new GenericItemOption(            "Tags", string.Empty),
            new GenericItemOption(       "UserScore", string.Empty),
            new GenericItemOption(         "Version", string.Empty)
        };

        private string PromptForSortingField()
            => _playniteAPI.Dialogs.ChooseItemWithSearch(
                SortableFields,
                s => SortableFields.OrderBy(f => f.Name.StartsWith(s)).ToList(),
                string.Empty,
                Resource.MENU_PROMPT_SORT)?.Name;

        #endregion

        #region Main Menu

        private IEnumerable<MainMenuItem> GetMainItems()
        {
            foreach (var menuItem in _mainMenuItems) yield return menuItem;

            if (_gameGroupManager.GameGroups.Any()) yield return new MainMenuItem
            {
                MenuSection = $"@{App.Name}",
                Description = "-"
            };

            foreach (var gameGroup in _gameGroupManager.GameGroups.OrderBy(g => g.Name))
            {
                var groupMenuSection = $"@{App.Name}|{GetDisplayName(gameGroup)}";

                //mainMenuItems.Add(new MainMenuItem
                //{
                //    Action = null,
                //    MenuSection = $"@ScreenSaver|Groups|{gameGroup.Name}",
                //    Description = "Preview",
                //    Icon = IconPath
                //});
                yield return new MainMenuItem
                {
                    Action = _ => _gameGroupManager.ToggleGameGroupActiveStatus(gameGroup),
                    MenuSection = groupMenuSection,
                    Description = Resource.MAIN_MENU_TOGGLE
                };
                yield return new MainMenuItem
                {
                    Action = _ => RenameGameGroup(gameGroup),
                    MenuSection = groupMenuSection,
                    Description = Resource.MAIN_MENU_RENAME
                };
                yield return new MainMenuItem
                {
                    Action = _ => DeleteGroup(gameGroup),
                    MenuSection = groupMenuSection,
                    Description = Resource.MENU_DELETE
                };

                var sortStatus = string.Empty;
                if (gameGroup.SortField != null)
                {
                    var sortPrefix = gameGroup.Ascending ? Resource.MAIN_MENU_ASC : Resource.MAIN_MENU_DESC;
                    sortStatus = $" ({gameGroup.SortField}:{sortPrefix})";
                }
                yield return new MainMenuItem
                {
                    Action = _ => SetSortField(gameGroup),
                    MenuSection = groupMenuSection,
                    Description = Resource.MAIN_MENU_SORT + sortStatus
                };

                yield return new MainMenuItem
                {
                    Action = _ => SetGameGroupFilter(gameGroup),
                    MenuSection = $"{groupMenuSection}",
                    Description = Resource.MAIN_MENU_SET_FILTER,
                    Icon = IconPath
                };

                if (gameGroup.Filter?.Settings != null) yield return new MainMenuItem
                {
                    Action = _ => _playniteAPI.MainView.ApplyFilterPreset(gameGroup.Filter),
                    MenuSection = $"{groupMenuSection}",
                    Description = Resource.MAIN_MENU_LOAD_FILTER
                };


                var SelectedSection = $"{groupMenuSection}|{Resource.MAIN_MENU_SELECTED_GAMES}|";
                var selectedGames = gameGroup.GameGuids.
                    Select(id => Games.FirstOrDefault(g => g.Id == id)).
                    Where(g => g != null).
                    OrderBy(g => g.Name);
                foreach (var game in selectedGames)
                {
                    var gameMenuSection = SelectedSection + game.Name;
                    yield return new MainMenuItem
                    {
                        Action = _ => _playniteAPI.MainView.SelectGame(game.Id),
                        MenuSection = gameMenuSection,
                        Description = Resource.MENU_VIEW
                    };
                    yield return new MainMenuItem
                    {
                        Action = _ => RemoveGameFromGroup(gameGroup, game),
                        MenuSection = gameMenuSection,
                        Description = Resource.MENU_PREVIEW
                    };
                    yield return new MainMenuItem
                    {
                        Action = _ => RemoveGameFromGroup(gameGroup, game),
                        MenuSection = gameMenuSection,
                        Description = Resource.MENU_DELETE
                    };
                }
            }
        }

        private void RenameGameGroup(GameGroup gameGroup)
        {
            var name = PromptForGroupName(gameGroup.Name);
            if (name is null)
            {
                return;
            }

            _gameGroupManager.RenameGameGroup(gameGroup, name);
        }

        private void SetGameGroupFilter(GameGroup gameGroup)
        {
            gameGroup.Filter = new FilterPreset
            {
                Settings = _playniteAPI.MainView.GetCurrentFilterSettings()
            };
            _gameGroupManager.SaveGameGroupChanges();
        }

        private void CreateDynamicGameGroup(object _)
        {
            var name = PromptForGroupName(string.Empty);
            if (name is null)
            {
                return;
            }

            var sortingField = PromptForSortingField();
            if (sortingField is null)
            {
                return;
            }

            var gameGroup = new GameGroup
            {
                Name = name,
                SortField = sortingField,
                Ascending = PromptForAscending(sortingField),
                Filter = new FilterPreset
                {
                    Settings = _playniteAPI.MainView.GetCurrentFilterSettings()
                }
            };

            _gameGroupManager.CreateGameGroup(gameGroup);
        }

        private void SetSortField(GameGroup gameGroup)
        {
            var field = PromptForSortingField();
            if (field is null)
            {
                return;
            }

            _gameGroupManager.SetSortField(gameGroup, field, PromptForAscending(field));
        }

        private void RemoveGameFromGroup(GameGroup gameGroup, Game game)
        { 
            if (gameGroup.GameGuids.Count() is 1 && GetBoolFromYesNoDialog(Resource.MAIN_MENU_GROUP_REMOVE_EMPTY))
            {
                _gameGroupManager.DeleteGameGroup(gameGroup);
                return;
            }

            _gameGroupManager.RemoveGamesFromGroup(gameGroup, new List<Guid> { game.Id });
        }

        private void DeleteGroup(GameGroup gameGroup)
        {
            if (GetBoolFromYesNoDialog(string.Format(Resource.MAIN_MENU_PROMPT_DELETE, gameGroup.Name)))
            {
                _gameGroupManager.DeleteGameGroup(gameGroup);
            }
        }

        private void ManuallyStartScreenSaver(object _) => _screenSaverManager.StartPolling(true, false);

        #endregion

        #region Helpers

        #region Prompts

        private bool PromptForAscending(string sortField) 
            => sortField != Resource.GROUP_SORT_RND && GetBoolFromYesNoDialog(Resource.MENU_PROMPT_ASC);

        private string PromptForGroupName(string suggestedName)
        {
            var result = _playniteAPI.Dialogs.SelectString(Resource.MAIN_MENU_GROUP_NAME, App.Name, suggestedName);
            if (!result.Result)
            {
                return null;
            }

            var groupName = result.SelectedString;
            while (string.IsNullOrWhiteSpace(groupName))
            {
                var groupResult = _playniteAPI.Dialogs.SelectString(
                    Resource.MAIN_MENU_GROUP_NAME_SHAME, App.Name, suggestedName);
                if (!groupResult.Result)
                {
                    return null;
                }
                
                groupName = groupResult.SelectedString;
            }
            return groupName;
        }

        private bool GetBoolFromYesNoDialog(string caption)
            => _playniteAPI.Dialogs.ShowMessage(caption, App.Name, MessageBoxButton.YesNo) is MessageBoxResult.Yes;

        #endregion

        private string GetDisplayName(GameGroup group) => group.IsActive ? group.Name + " •" : group.Name;

        private IEnumerable<Game> Games => _playniteAPI.Database.Games;
        private IEnumerable<Game> SelectedGames => _playniteAPI.MainView.SelectedGames;
        private IEnumerable<Guid> SelectedGuids => SelectedGames.Select(g => g.Id);

        #endregion

        #endregion
    }
}
