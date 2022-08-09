using Playnite.SDK;
using Playnite.SDK.Plugins;
using ScreenSaver.Common.Constants;
using ScreenSaver.Services.State.ScreenSaver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ScreenSaver.Services.UI.Menus
{
    internal class MenuManager : IMenuManager
    {
        #region Infrastructure

        #region Variables

        private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string IconPath = Path.Combine(PluginFolder, "icon.png");

        private readonly IPlayniteAPI _playniteAPI;
        private readonly IScreenSaverManager _screenSaverManager;

        private readonly List<GameMenuItem> _gameMenuItems;
        private readonly List<MainMenuItem> _mainMenuItems;

        #endregion

        public MenuManager(IPlayniteAPI playniteAPI, IScreenSaverManager screenSaverManager)
        {
            Localization.SetPluginLanguage(PluginFolder, playniteAPI.ApplicationSettings.Language);
            _playniteAPI = playniteAPI;
            _screenSaverManager = screenSaverManager;

            _gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Action = OpenScreenSaver,
                    Description = Resource.GAME_MENU_PREVIEW,
                    MenuSection = "ScreenSaver",
                    Icon = IconPath
                },
            };

            _mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Action = ManuallyStartScreenSaver,
                    Description = Resource.MAIN_MENU_START,
                    MenuSection = "@ScreenSaver",
                    Icon = IconPath
                }
            };
        }

        #endregion

        #region Interface

        public IEnumerable<GameMenuItem> GetGameMenuItems() => _gameMenuItems;
        public IEnumerable<MainMenuItem> GetMainMenuItems() => _mainMenuItems;

        #endregion

        #region Implementation

        #region Game Menu

        private void OpenScreenSaver(GameMenuItemActionArgs args)
        {
            if (_playniteAPI.MainView.SelectedGames.Count() == 1)
            {
                _screenSaverManager.PreviewScreenSaver(_playniteAPI.MainView.SelectedGames.First());
            }
        }

        #endregion

        #region Main Menu

        private void ManuallyStartScreenSaver(object _ = null)
        {
            _screenSaverManager.StartPolling(true);
        }

        #endregion

        #endregion
    }
}
