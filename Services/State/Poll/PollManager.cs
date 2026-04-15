using ScreenSaver.Common.Constants;
using ScreenSaver.Services.UI.Windows;
using System.Diagnostics;
using System.Windows;
using ScreenSaver.Common.Imports;
using static ScreenSaver.Common.Imports.User32;
using Playnite;
using ScreenSaver.Services.State.Settings;

namespace ScreenSaver.Services.State.Poll;

internal class PollManager(IWindowsManager screenSaverManager, bool soundsLoaded) : SettingsConsumer, IPollManager
{
    #region Infrastructure

    #region Variables

    private static readonly ILogger _logger  = LogManager.GetLogger();

    private static          IntPtr                                         _hookID;

    private static          Task?                                 _screenSaverTask;

    private static          int                            _lastInputTimeStampInMs;
    private static          int                     _lastScreenChangeTimeStampInMs;
    private static readonly List<Keys>                                       _keys = Enum.GetValues(typeof(Keys)).Cast<Keys>().ToList();
    private        readonly Dictionary<Keys, bool>                      _keyStates = _keys.ToDictionary(k => k, _ => false);
    private bool                                                        _isPolling;
    private                 int?                                   _timeSinceStart;

    #endregion

    #endregion

    #region Interface

    public void SetupPolling      (                            ) =>  Setup  (           );
    public void StartPolling      (bool             immediately) =>  Start  (immediately);
    public void PausePolling      (                            ) =>  Pause  (           );
    public void StopPolling       (                            ) =>   Stop  (           );

    #endregion

    #region Implementation

    #region SetupPolling

    private static void Setup() => _hookID = SetHook(_proc);

    private static readonly LowLevelMouseProc _proc = (nCode, wParam, lParam) =>
    {
        if (nCode >= 0)
        {
            _lastInputTimeStampInMs = Environment.TickCount;
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    };

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        if (curProcess.MainModule is null)
        {
            return IntPtr.Zero;
        }
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(Imports.WH_MOUSE_LL, proc, Kernel32.GetModuleHandle(curModule.ModuleName), 0);
    }

    #endregion

    #region StartPolling

    private void Start(bool startImmediately)
    {
        switch (_screenSaverTask?.Status)
        {
            case null:
            case TaskStatus.RanToCompletion:
                _isPolling = true;
                _screenSaverTask = PollForInputAsync(startImmediately);
                break;
            case TaskStatus.Created:
            case TaskStatus.Running:
            case TaskStatus.WaitingToRun:
            case TaskStatus.WaitingForActivation:
                if (startImmediately)
                {
                    _timeSinceStart = Environment.TickCount + 100;
                    _lastScreenChangeTimeStampInMs = Environment.TickCount;
                    screenSaverManager.StartScreenSaverAsync();
                }
                break;
            default:
                _logger.Warn($"ScreenSaver Task was in an unexpected state: {_screenSaverTask.Status}");
                break;
        }
    }

    // TODO: Replace with debounce logic
    private async Task PollForInputAsync(bool startImmediately) 
    { 
        try                 { await PollLoop(startImmediately); } 
        catch (Exception e) { _logger.Error(e, "Something Went Wrong while running ScreenSaver Poll."); }
    }


    private async Task PollLoop(bool startImediately)
    {
        _lastScreenChangeTimeStampInMs = 0;
        _lastInputTimeStampInMs = startImediately ? 0 : Environment.TickCount;
        DateTime time = default;
        while (_isPolling)
        {
            UpdateScreenSaverState(ref time);

            // don't need to be as responsive if the screen saver is not visible
            await Task.Delay(_timeSinceStart is null ? 1000 : 100);

            var aKeyStateChanged = AKeyStateChanged();

            if (aKeyStateChanged)
            {
                _lastInputTimeStampInMs = Environment.TickCount;
            }
        }
    }

    private void UpdateScreenSaverState(ref DateTime time)
    {
        var screenSaverIsNotRunning = _timeSinceStart is null;

        if (screenSaverIsNotRunning && TimeToStart)
        {
            Process? process = null;
            if (soundsLoaded)
            {
                process = Process.Start(App.SoundsUriPause);
            }

            Application.Current.Dispatcher.Invoke(screenSaverManager.StartScreenSaverAsync);
            _timeSinceStart = Environment.TickCount + 100;
            _lastScreenChangeTimeStampInMs = Environment.TickCount;
            time = DateTime.Now;

            if (soundsLoaded)
            {
                process!.WaitForExit();
                process.Dispose();
            }
            
        }
        else if (_lastInputTimeStampInMs > _timeSinceStart)
        {
            Process? process = null;
            if (soundsLoaded)
            {
                process = Process.Start(App.SoundsUriPlay);
            }
            _timeSinceStart = null;
            Application.Current.Dispatcher.InvokeAsync(screenSaverManager.StopScreenSaver);

            if (soundsLoaded)
            {
                process!.WaitForExit();
                process.Dispose();
            }
        }
        else if (!screenSaverIsNotRunning && TimeToUpdate)
        {
            _lastScreenChangeTimeStampInMs = Environment.TickCount;
            Application.Current.Dispatcher.Invoke(screenSaverManager.UpdateScreenSaverAsync);
            time = DateTime.Now;
        }

        var currentTime = DateTime.Now;
        if (currentTime.Second is 0 && currentTime - time > TimeSpan.FromMinutes(1))
        {
            time = DateTime.Now;
            Application.Current.Dispatcher.Invoke(screenSaverManager.UpdateScreenSaverTime);
        }
    }

    private bool  TimeToStart => Environment.TickCount - _lastInputTimeStampInMs
                               > Settings.ScreenSaverInterval * 1000;
    private bool TimeToUpdate => Environment.TickCount - _lastScreenChangeTimeStampInMs
                               > Settings.GameTransitionInterval * 1000;

    // A keyboard hook would be better, but not necessary until we can find an event or hook for controllers
    private bool AKeyStateChanged()
    {
        var keyChanged = false;

        foreach (var key in _keys)
        {
            var keyState = _keyStates[key];
            if (IsKeyPushedDown(key) != keyState)
            {
                _keyStates[key] = !keyState;
                keyChanged = true;
            }
        }

        return keyChanged;
    }

    public static bool IsKeyPushedDown(Keys key) => 0 != (GetAsyncKeyState(key) & 0x8000);

    #endregion

    #region PausePolling

    private void Pause() => _isPolling = false;

    #endregion

    #region StopPolling

    private void Stop()
    {
        if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID);
        Pause();
    }

    #endregion

    #region OnButtonPress

    public void OnButtonPress() => _lastInputTimeStampInMs = Environment.TickCount;

    #endregion

    #endregion
}