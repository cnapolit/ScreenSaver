using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Models;
using ScreenSaver.Models;
using ScreenSaver.Models.Enums;
using ScreenSaver.Services.State.Poll;
using ScreenSaver.Services.UI.Windows;
using System;
using System.Windows;

namespace ScreenSaver.Services.State.ScreenSaver
{
    internal class ScreenSaverManager : IScreenSaverManager
    {
        private readonly IPlayniteAPI           _playniteApi;
        private readonly IPollManager           _pollManager;
        private readonly IWindowsManager     _windowsManager;
        private          ScreenSaverSettings       _settings;

        public ScreenSaverManager(IPlayniteAPI playniteApi, ScreenSaverSettings settings)
        {
            _playniteApi = playniteApi;
            _settings    =    settings;

            _windowsManager = new WindowsManager (playniteApi, settings,  OnStopCallBack);
            _pollManager    = new PollManager    (             settings, _windowsManager);
        }

        #region Interface

        public void SetupPolling       (                            ) => Setup   (           );
        public void StopPolling        (                            ) => Stop    (           );
        public void StartPolling       (bool                  manual) => Start   (     manual);
        public void PausePolling       (bool             ignoreCheck) => Pause   (ignoreCheck);
        public void UpdateSettings     (ScreenSaverSettings settings) => Update  (   settings);
        public void PreviewScreenSaver (Game                    game) => Preview (       game);

        #endregion

        #region Implementation

        #region SetupPolling

        private void Setup()
        {
            SystemEvents .PowerModeChanged                += OnPowerModeChanged;
            Application  .Current.Deactivated             += OnApplicationDeactivate;
            Application  .Current.Activated               += OnApplicationActivate;
            Application  .Current.MainWindow.StateChanged += OnWindowStateChanged;

            _pollManager.SetupPolling();

            if (ShouldPoll())
            {
                _pollManager.StartPolling(false);
            }
        }

        #endregion

        #region StartPolling

        private void Start(bool manual)
        {
            if (manual || ShouldPoll())
            {
                _pollManager.StartPolling(manual);
            }
        }

        #endregion

        #region PausePolling

        private void Pause(bool ignoreCheck)
        {
            if (ignoreCheck || _settings.DisableWhilePlaying)
            {
                _pollManager.    PausePolling    ();
                _windowsManager. StopScreenSaver ();
            }
        }

        #endregion

        #region StopPolling

        private void Stop()
        {
            SystemEvents .PowerModeChanged                -= OnPowerModeChanged;
            Application  .Current.Deactivated             -= OnApplicationDeactivate;
            Application  .Current.Activated               -= OnApplicationActivate;
            Application  .Current.MainWindow.StateChanged -= OnWindowStateChanged;

            _pollManager.StopPolling();
        }

        #endregion

        #region PreviewScreenSaver

        public void Preview(Game game)
        {
            Pause(true);
            _windowsManager.PreviewScreenSaver(game, () => Start(false));
        }

        #endregion

        #region UpdateSettings

        private void Update(ScreenSaverSettings settings)
        {
            _windowsManager. UpdateSettings(settings);
            _pollManager.    UpdateSettings(settings);

            if (settings.PlayState != _settings.PlayState) switch (settings.PlayState)
            {
                case    PlayState.Never: _pollManager.PausePolling(     ); break;
                default                : _pollManager.StartPolling(false); break;
            }

            _settings = settings;
        }

        #endregion

        #region Helpers

        private void OnStopCallBack()
        {
            if   (ShouldPoll()) _pollManager.StartPolling(false);
            else                _pollManager.PausePolling(     );
        }

        private bool ShouldPoll()
        {
            var desktopMode = _playniteApi.ApplicationInfo.Mode is ApplicationMode.Desktop;

            var playOnBoth       = _settings.PlayState is PlayState.Always;
            var playOnFullScreen = _settings.PlayState is PlayState.FullScreen && !desktopMode;
            var playOnDesktop    = _settings.PlayState is PlayState.Desktop    &&  desktopMode;

            return playOnBoth || playOnFullScreen || playOnDesktop;
        }

        #region State Changes

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate) switch (Application.Current?.MainWindow?.WindowState)
            {
                case WindowState.Normal   :
                case WindowState.Maximized: _pollManager.StartPolling(false); break;
                case WindowState.Minimized: _pollManager.PausePolling(     ); break;
            }
        }

        private void OnApplicationDeactivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate) _pollManager.PausePolling();
        }

        private void OnApplicationActivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate) _pollManager.StartPolling(false);
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            switch (args.Mode)
            {
                case PowerModes.Resume : _pollManager.StartPolling(false); break;
                case PowerModes.Suspend: _pollManager.PausePolling(     ); break;
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
