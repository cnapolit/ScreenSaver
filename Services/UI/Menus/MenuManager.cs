using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using ScreenSaver.Common.Constants;
using ScreenSaver.Models;
using ScreenSaver.Services.State.ScreenSaver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                    MenuSection = App.Name,
                    Icon = IconPath
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
            foreach (var gameGroup in _gameGroupManager.GameGroups.OrderBy(g => g.Name))
            {
                var groupMenuSection = baseMenuSection + GetDisplayName(gameGroup);
                _gameMenuItems.Add(new GameMenuItem
                {
                    Action = _ => _gameGroupManager.AddGamesToGroup(gameGroup, SelectedGuids),
                    MenuSection = groupMenuSection,
                    Description = Resource.GAME_MENU_ADD_SELECTED,
                    Icon = IconPath
                });
                _gameMenuItems.Add(new GameMenuItem
                {
                    Action = _ => _gameGroupManager.RemoveGamesFromGroup(gameGroup, SelectedGuids),
                    MenuSection = groupMenuSection,
                    Description = Resource.GAME_MENU_REMOVE_SELECTED,
                    Icon = IconPath
                });
            }

            return gameMenuItems;
        }

        private void OpenScreenSaver(GameMenuItemActionArgs args)
        {
            if (SelectedGuids.Count() is 1)
            {
                _screenSaverManager.PreviewScreenSaver(SelectedGames.First());
            }
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
        {
            var result = _playniteAPI.Dialogs.ChooseItemWithSearch(
                SortableFields, s => SortableFields.OrderBy(f => f.Name.StartsWith(s)).ToList(), string.Empty, Resource.MENU_PROMPT_SORT);

            return result?.Name;
        }

        #endregion

        #region Main Menu

        private IList<MainMenuItem> GetMainItems()
        {
            var mainMenuItems = new List<MainMenuItem>(_mainMenuItems);

            var baseMenuSection = $"@{App.Name}|{Resource.MENU_GROUPS}|";
            foreach (var gameGroup in _gameGroupManager.GameGroups.OrderBy(g => g.Name))
            {
                var groupMenuSection = baseMenuSection + GetDisplayName(gameGroup);

                //mainMenuItems.Add(new MainMenuItem
                //{
                //    Action = null,
                //    MenuSection = $"@ScreenSaver|Groups|{gameGroup.Name}",
                //    Description = "Preview",
                //    Icon = IconPath
                //});
                mainMenuItems.Add(new MainMenuItem
                {
                    Action = _ => _gameGroupManager.ToggleGameGroupActiveStatus(gameGroup),
                    MenuSection = groupMenuSection,
                    Description = Resource.MAIN_MENU_TOGGLE,
                    Icon = IconPath
                });
                mainMenuItems.Add(new MainMenuItem
                {
                    Action = _ => RenameGameGroup(gameGroup),
                    MenuSection = groupMenuSection,
                    Description = Resource.MAIN_MENU_RENAME,
                    Icon = IconPath
                });
                mainMenuItems.Add(new MainMenuItem
                {
                    Action = _ => DeleteGroup(gameGroup),
                    MenuSection = groupMenuSection,
                    Description = Resource.MENU_DELETE,
                    Icon = IconPath
                });

                var sortPrefix = gameGroup.Ascending ? Resource.MAIN_MENU_ASC : Resource.MAIN_MENU_DESC;
                mainMenuItems.Add(new MainMenuItem
                {
                    Action = _ => SetSortField(gameGroup),
                    MenuSection = $"{groupMenuSection}|{sortPrefix}:{gameGroup.SortField}",
                    Description = Resource.MAIN_MENU_SORT,
                    Icon = IconPath
                });

                var games = gameGroup.GameGuids.
                    Select(id => Games.FirstOrDefault(g => g.Id == id)).
                    Where(g => g != null).
                    OrderBy(g => g.Name);

                foreach (var game in games)
                {
                    var gameMenuSection = $"{groupMenuSection}|{Resource.GAME_MENU_GAMES}|{game.Name}";
                    mainMenuItems.Add(new MainMenuItem
                    {
                        Action = _ => _screenSaverManager.PreviewScreenSaver(game),
                        MenuSection = gameMenuSection,
                        Description = Resource.MENU_PREVIEW,
                        Icon = IconPath
                    });
                    mainMenuItems.Add(new MainMenuItem
                    {
                        Action = _ => RemoveGameFromGroup(gameGroup, game),
                        MenuSection = gameMenuSection,
                        Description = Resource.MENU_DELETE,
                        Icon = IconPath
                    });
                }
            }

            return mainMenuItems;
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

        private void SetSortField(GameGroup gameGroup)
        {
            var field = PromptForSortingField();
            if (field is null)
            {
                return;
            }

            var ascending = PromptForAscending(field);

            _gameGroupManager.SetSortField(gameGroup, field, ascending);
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
                groupName = _playniteAPI.Dialogs.SelectString(Resource.MAIN_MENU_GROUP_NAME_SHAME, App.Name, suggestedName).SelectedString;
                if (!result.Result)
                {
                    return null;
                }
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
