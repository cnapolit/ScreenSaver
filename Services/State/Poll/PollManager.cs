using Playnite.SDK;
using ScreenSaver.Common.Constants;
using ScreenSaver.Services.UI.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ScreenSaver.Common.Imports;
using ScreenSaver.Models;
using static ScreenSaver.Common.Imports.User32;
using static SDL2.SDL;
using Keys = System.Windows.Forms.Keys;

namespace ScreenSaver.Services.State.Poll
{
    internal class PollManager : IPollManager
    {
        #region Infrastructure

        #region Variables

        private static readonly ILogger     _logger  =                                         LogManager.GetLogger();
        private static readonly Keys[]      _badKeys = new[] { Keys.KeyCode, Keys.Modifiers, Keys.None, Keys.Packet };
        private static readonly IList<Keys> _keys   = Enum.
            GetValues(typeof(Keys)).
            Cast<Keys>().
            Where(k => !_badKeys.Contains(k)).
            ToList();

        private static          IntPtr                                         _hookID;

        private static          Task                                  _screenSaverTask;

        private static          int                            _lastInputTimeStampInMs;
        private static          int                     _lastScreenChangeTimeStampInMs;
        private        readonly bool                                     _soundsLoaded;
        private        readonly IDictionary<Keys, bool>                     _keyStates;
        private        readonly IWindowsManager                    _screenSaverManager;

        private                 bool                                        _isPolling;
        private                 int?                                   _timeSinceStart;
        private                 uint                                 _gameIntervalInMs;
        private                 uint                          _ScreenSaverIntervalInMs;

        private                 ScreenSaverSettings                          _settings;

        #endregion

        public PollManager(ScreenSaverSettings settings, IWindowsManager screenSaverManager, bool soundsLoaded)
        {
            Update(settings); 
            _screenSaverManager = screenSaverManager;
            _soundsLoaded = soundsLoaded;

            // I'm too lazy to type this out. Besides, what if it changes ¯\_(ツ)_/¯
            _keyStates = new Dictionary<Keys, bool>();
            foreach (var key in _keys) _keyStates[key] = false;
        }

        #endregion

        #region Interface

        public void SetupPolling      (                            ) =>  Setup  (           );
        public void StartPolling      (bool             immediately) =>  Start  (immediately);
        public void PausePolling      (                            ) =>  Pause  (           );
        public void StopPolling       (                            ) =>   Stop  (           );
        public void UpdateSettings    (ScreenSaverSettings settings) =>  Update (   settings);

        #endregion

        #region Implementation

        #region SetupPolling

        private unsafe void Setup()
        {
            _hookID = SetHook(_proc);
            SDL_Init(SDL_INIT_GAMECONTROLLER | SDL_INIT_JOYSTICK);
        }

        private static readonly LowLevelMouseProc _proc = (int nCode, IntPtr wParam, IntPtr lParam) =>
        {
            if (nCode >= 0)
            {
                _lastInputTimeStampInMs = Environment.TickCount;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        };

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
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
                    _screenSaverTask = Task.Run(() => PollForInput(startImmediately));
                    break;
                case TaskStatus.Created:
                case TaskStatus.Running:
                case TaskStatus.WaitingToRun:
                case TaskStatus.WaitingForActivation:
                    if (startImmediately)
                    {
                        _timeSinceStart = Environment.TickCount + 100;
                        _lastScreenChangeTimeStampInMs = Environment.TickCount;
                        _screenSaverManager.StartScreenSaver();
                    }
                    break;
                default:
                    _logger.Warn($"ScreenSaver Task was in an unexpected state: {_screenSaverTask.Status}");
                    break;
            }
        }

        // I would rather rely on a timer, but that isn't ideal until a event handler for controllers available.
        // Otherwise, we would still need to poll for gamepad input to close the screen saver
        // and I'm not polling and managing a timer together.
        private void PollForInput(bool startImmediately) 
        { 
            try { PollLoop(startImmediately); } 
            catch (Exception e) { _logger.Error(e, "Something Went Wrong while running ScreenSaver Poll."); }
        }


        private void PollLoop(bool startImediately)
        {

            var count = SDL_NumJoysticks();
            var sdlControllers = new List<GameController>(count);
            for (var i = 0; i < count; i++)
            {
                if (SDL_IsGameController(i) != SDL_bool.SDL_TRUE) continue;
                sdlControllers.Add(new GameController(i));
            }

            var packetNumbers = new int?[4];
            _lastScreenChangeTimeStampInMs = 0;
            _lastInputTimeStampInMs = startImediately ? 0 : Environment.TickCount;
            DateTime time = default;
            while (_isPolling)
            {
                UpdateScreenSaverState(ref time);

                // don't need to be as responsive if the screen saver is not visible
                Task.Delay(_timeSinceStart is null ? 1000 : 100).Wait();

                if (AKeyStateChanged())
                {
                    _lastInputTimeStampInMs = Environment.TickCount;
                }

                while (SDL_PollEvent(out var sdlEv) == 1)
                {
                    var index = sdlEv.cdevice.which;
                    switch (sdlEv.type)
                    {
                        case SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                            if (sdlControllers.All(c => c.InstanceId != index))
                            {
                                sdlControllers.Add(new GameController(index));
                            }
                            break;
                        case SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                            var controller = sdlControllers.FirstOrDefault(c => c.InstanceId == index);
                            if (controller != null)
                            {
                                sdlControllers.Remove(controller);
                            }
                            break;
                    }
                }

                SDL_GameControllerUpdate();
                if (sdlControllers.Where(controller => controller.ProcessState()).ToList().Any())
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
                Process process = null;
                if (_soundsLoaded)
                {
                    process = Process.Start(App.SoundsUriPause);
                }

                Application.Current.Dispatcher.Invoke(_screenSaverManager.StartScreenSaver);
                _timeSinceStart = Environment.TickCount + 100;
                _lastScreenChangeTimeStampInMs = Environment.TickCount;
                time = DateTime.Now;

                if (_soundsLoaded)
                {
                    process.WaitForExit();
                    process.Dispose();
                }
                
            }
            else if (_lastInputTimeStampInMs > _timeSinceStart)
            {
                Process process = null;
                if (_soundsLoaded)
                {
                    process = Process.Start(App.SoundsUriPlay);
                }
                _timeSinceStart = null;
                Application.Current.Dispatcher.Invoke(_screenSaverManager.StopScreenSaver);

                if (_soundsLoaded)
                {
                    process.WaitForExit();
                    process.Dispose();
                }
            }
            else if (!screenSaverIsNotRunning && TimeToUpdate)
            {
                _lastScreenChangeTimeStampInMs = Environment.TickCount;
                Application.Current.Dispatcher.Invoke(_screenSaverManager.UpdateScreenSaver);
                time = DateTime.Now;
            }

            var currentTime = DateTime.Now;
            if (currentTime.Second is 0 && currentTime - time > TimeSpan.FromMinutes(1))
            {
                time = DateTime.Now;
                Application.Current.Dispatcher.Invoke(_screenSaverManager.UpdateScreenSaverTime);
            }
        }

        private bool  TimeToStart => Environment.TickCount -        _lastInputTimeStampInMs > _ScreenSaverIntervalInMs;
        private bool TimeToUpdate => Environment.TickCount - _lastScreenChangeTimeStampInMs >        _gameIntervalInMs;

        // A keyboard hook would be better, but not necessary until we can find an event or hook for controllers
        private bool AKeyStateChanged()
        {
            bool keyChanged = false;

            foreach (var key in _keys)
            {
                var keyState = _keyStates[key];
                if (IsKeyPushedDown(key) != _keyStates[key])
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

        private void Pause()
        {
            _isPolling = false;
        }

        #endregion

        #region StopPolling

        private void Stop()
        {
            UnhookWindowsHookEx(_hookID);
            Pause();
        }

        #endregion

        #region UpdateSettings

        private void Update(ScreenSaverSettings settings)
        {
            _settings                =                                 settings;
            _gameIntervalInMs        = _settings. GameTransitionInterval * 1000;
            _ScreenSaverIntervalInMs = _settings.    ScreenSaverInterval * 1000;
        }

        #endregion

        #endregion
    }
}
