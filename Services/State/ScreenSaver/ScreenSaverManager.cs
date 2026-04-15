using Microsoft.Win32;
using Playnite;
using ScreenSaver.Common.Constants;
using ScreenSaver.Common.Imports;
using ScreenSaver.Models.Enums;
using ScreenSaver.Services.State.Poll;
using ScreenSaver.Services.State.Settings;
using ScreenSaver.Services.UI.Windows;
using System.Diagnostics;
using System.Windows;

namespace ScreenSaver.Services.State.ScreenSaver;

internal class ScreenSaverManager : SettingsConsumer, IScreenSaverManager
{
    #region Infrastructure

    private int                      _activeGameCount;
    private readonly IPlayniteApi    _playniteApi;
    private readonly IWindowsManager _windowsManager;
    private readonly IPollManager    _pollManager;
    private readonly bool            _soundsLoaded;

    public ScreenSaverManager(
        IPlayniteApi playniteApi, IWindowsManager windowsManager, IPollManager pollManager, bool soundsLoaded)
    {
        _playniteApi = playniteApi;
        _windowsManager = windowsManager;
        _pollManager = pollManager;
        _soundsLoaded = soundsLoaded;
        _windowsManager.OnStop += UpdatePollState;

    }

    #endregion

    #region Interface

    public void SetupPolling       (                                              ) => Setup   (                        );
    public void StopPolling        (                                              ) => Stop    (                        );
    public void StartPolling       (bool                  manual, bool gameStopped) => Start   (     manual, gameStopped);
    public void PausePolling       (bool             ignoreCheck, bool gameStarted) => Pause   (ignoreCheck, gameStarted);
    public void PreviewScreenSaver (Game                    game                  ) => Preview (       game             );

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

    private void Start(bool manual, bool gameStopped)
    {
        if (gameStopped)
        {
            _activeGameCount--;
        }

        if (manual || ShouldPoll())
        {
            _pollManager.StartPolling(manual);
        }
    }

    #endregion

    #region PausePolling

    private void Pause(bool ignoreCheck, bool gameStarted)
    {
        if (gameStarted)
        {
            _activeGameCount++;
        }

        if (ignoreCheck || (Settings.DisableWhilePlaying && gameStarted))
        {
            _pollManager    .PausePolling    ();
            _windowsManager .StopScreenSaver ();
        }
    }

    #endregion

    #region StopPolling

    private void Stop()
    {
        SystemEvents .PowerModeChanged    -= OnPowerModeChanged;
        Application  .Current.Deactivated -= OnApplicationDeactivate;
        Application  .Current.Activated   -= OnApplicationActivate;

        Application.Current.MainWindow?.StateChanged -= OnWindowStateChanged;

        _pollManager.StopPolling();
    }

    #endregion

    #region PreviewScreenSaver

    public void Preview(Game game)
    {
        Pause(true, false);
        Process? process = null;
        if (_soundsLoaded)
        {
            process = Process.Start(App.SoundsUriPause);
        }

        _windowsManager.PreviewScreenSaver(game, () =>
        {
            Process? playProcess = null;
            if (_soundsLoaded)
            {
                playProcess = Process.Start(App.SoundsUriPlay);
            }
            Start(false, false);
            if (_soundsLoaded)
            {
                playProcess!.WaitForExit();
                playProcess.Dispose();
            }

        });

        if (_soundsLoaded)
        {
            process!.WaitForExit();
            process.Dispose();
        }
    }

    #endregion

    #region UpdatePollState

    public void UpdatePollState()
    {
        if   (ShouldPoll()) _pollManager.StartPolling(false);
        else                _pollManager.PausePolling(     );
    }

    #endregion

    #region Helpers


    private bool ShouldPoll()
    {
        var desktopMode = _playniteApi.AppInfo.Mode is AppMode.Desktop;

        var playOnBoth       = Settings.PlayState is PlayState.Always;
        var playOnFullScreen = Settings.PlayState is PlayState.FullScreen && !desktopMode;
        var playOnDesktop    = Settings.PlayState is PlayState.Desktop    &&  desktopMode;

        var stopDuringGame   = Settings.DisableWhilePlaying  && _activeGameCount > 0;
        var isTop            = !Settings.PauseOnDeactivate || PlayniteIsInForeground();

        return (playOnBoth || playOnFullScreen || playOnDesktop) && !stopDuringGame && isTop;
    }

    private static bool PlayniteIsInForeground()
    {
        var foregroundHandle = User32.GetForegroundWindow();

        return Process.
            GetProcesses().
            Where(p => p.ProcessName.Contains("Playnite")).
            Any(p => p.MainWindowHandle == foregroundHandle);
    }

    #region State Changes

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (Settings.PauseOnDeactivate) switch (Application.Current?.MainWindow?.WindowState)
        {
            case WindowState.Normal   :
            case WindowState.Maximized: Start(false, false); break;
            case WindowState.Minimized: Pause( true, false); break;
        }
    }

    private void OnApplicationDeactivate(object? sender, EventArgs e)
    {
        if (Settings.PauseOnDeactivate) Pause(true, false);
    }

    private void OnApplicationActivate(object? sender, EventArgs e)
    {
        if (Settings.PauseOnDeactivate) Start(false, false);
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs args)
    {
        switch (args.Mode)
        {
            case PowerModes.Resume : Start(false, false); break;
            case PowerModes.Suspend: Pause( true, false); break;
        }
    }

    #endregion

    #endregion

    #endregion
}
