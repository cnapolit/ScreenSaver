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

namespace ScreenSaver
{
    public class ScreenSaverPlugin : GenericPlugin, IScreenSaverSettings
    {
        #region Infrastructure

        private ScreenSaverSettingsViewModel SettingsViewModel { get; set; }

        private readonly IMenuManager               _menuManager;
        private readonly IScreenSaverManager _screenSaverManager;

        public ScreenSaverPlugin(IPlayniteAPI api) : base(api)
        {
            Properties          = new GenericPluginProperties      {      HasSettings = true       };
            SettingsViewModel   = new ScreenSaverSettingsViewModel (                           this);
            _screenSaverManager = new ScreenSaverManager           (api, SettingsViewModel.Settings);
            _menuManager        = new MenuManager                  (api,        _screenSaverManager);

        }

        #endregion

        #region Playnite Interface
        public override Guid Id { get; } = Guid.Parse("198510bc-f254-46d5-8ac7-d048e9cd1688");
        public override ISettings                 GetSettings          (bool           firstRunSettings) => SettingsViewModel;
        public override UserControl               GetSettingsView      (bool           firstRunSettings) => new ScreenSaverSettingsView();
        public override void                      OnGameStarting       (OnGameStartingEventArgs       _) => _screenSaverManager. PausePolling     (   false);
        public override void                      OnGameStopped        (OnGameStoppedEventArgs        _) => _screenSaverManager. StartPolling     (   false);
        public override void                      OnApplicationStarted (OnApplicationStartedEventArgs _) => _screenSaverManager. SetupPolling     (        );
        public override void                      OnApplicationStopped (OnApplicationStoppedEventArgs _) => _screenSaverManager. StopPolling      (        );
        public override IEnumerable<GameMenuItem> GetGameMenuItems     (GetGameMenuItemsArgs          _) => _menuManager.        GetGameMenuItems (        );
        public override IEnumerable<MainMenuItem> GetMainMenuItems     (GetMainMenuItemsArgs          _) => _menuManager.        GetMainMenuItems (        );
        public          void                      UpdateSettings       (ScreenSaverSettings    settings) => _screenSaverManager. UpdateSettings   (settings);

        #endregion
    }
}