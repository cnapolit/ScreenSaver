using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using ScreenSaver.Services.State.ScreenSaver;
using ScreenSaver.Services.UI.Menus;
using ScreenSaver.Services;
using ScreenSaver.Models;
using ScreenSaver.Views.Layouts;
using ScreenSaver.Views.Models;
using System.Linq;

namespace ScreenSaver
{
    public class ScreenSaverPlugin : GenericPlugin, IScreenSaverSettings
    {
        #region Infrastructure

        private ScreenSaverSettingsViewModel SettingsViewModel { get; set; }

        private readonly IMenuManager               _menuManager;
        private readonly IGameGroupManager     _gameGroupManager;
        private readonly IScreenSaverManager _screenSaverManager;

        public ScreenSaverPlugin(IPlayniteAPI api) : base(api)
        {
            Properties          = new GenericPluginProperties      {                 HasSettings = true                 };
            SettingsViewModel   = new ScreenSaverSettingsViewModel (                                                this);
            _gameGroupManager   = new GameGroupManager             (                        api.Paths.ExtensionsDataPath);
            _screenSaverManager = new ScreenSaverManager           (api, _gameGroupManager,   SettingsViewModel.Settings);
            _menuManager        = new MenuManager                  (api, _gameGroupManager,          _screenSaverManager);

            PlayniteApi.Database.Games.ItemCollectionChanged +=
                (_, args) => _gameGroupManager.RemoveGamesFromGroups(args.RemovedItems.Select(g => g.Id));
        }

        #endregion

        #region Playnite Interface

        public override Guid Id { get; } = Guid.Parse(Common.Constants.App.Id);
        public override ISettings                 GetSettings          (bool           firstRunSettings) => SettingsViewModel;
        public override UserControl               GetSettingsView      (bool           firstRunSettings) => new ScreenSaverSettingsView();
        public override void                      OnGameStarting       (OnGameStartingEventArgs       _) => _screenSaverManager. PausePolling     (false, true);
        public override void                      OnGameStopped        (OnGameStoppedEventArgs        _) => _screenSaverManager. StartPolling     (false, true);
        public override void                      OnApplicationStarted (OnApplicationStartedEventArgs _) => _screenSaverManager. SetupPolling     (           );
        public override void                      OnApplicationStopped (OnApplicationStoppedEventArgs _) => _screenSaverManager. StopPolling      (           );
        public override IEnumerable<GameMenuItem> GetGameMenuItems     (GetGameMenuItemsArgs          _) => _menuManager.        GetGameMenuItems (           );
        public override IEnumerable<MainMenuItem> GetMainMenuItems     (GetMainMenuItemsArgs          _) => _menuManager.        GetMainMenuItems (           );
        public          void                      UpdateSettings       (ScreenSaverSettings    settings) => _screenSaverManager. UpdateSettings   (   settings);

        #endregion
    }
}