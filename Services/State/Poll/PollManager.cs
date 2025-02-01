using Playnite.SDK;
using ScreenSaver.Common.Constants;
using ScreenSaver.Services.UI.Windows;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ScreenSaver.Common.Imports;
using ScreenSaver.Models;
using static ScreenSaver.Common.Imports.User32;
using Keys = System.Windows.Forms.Keys;
using static SDL3.SDL;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ScreenSaver.Services.State.Poll
{
    internal class PollManager : IPollManager
    {
        #region Infrastructure

        #region Variables

        private static readonly ILogger     logger  =                                         LogManager.GetLogger();
        private static readonly Keys[]      badKeys = new[] { Keys.KeyCode, Keys.Modifiers, Keys.None, Keys.Packet };
        private static readonly IList<Keys> _keys   = Enum.
            GetValues(typeof(Keys)).
            Cast<Keys>().
            Where(k => !badKeys.Contains(k)).
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

            var result = SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
            if (!result)
            {
                logger.Error($"SDL_SetHint Error: {SDL_GetError()}");
                return;
            }

            result = SDL_Init(SDL_InitFlags.SDL_INIT_JOYSTICK | SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_EVENTS);
            var err = SDL_GetError();
            if (!result)
            {
                logger.Error($"SDL_Init Error: {SDL_GetError()}");
                return;
            }

            result = SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_JOYSTICK | SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_EVENTS);
            err = SDL_GetError();
            if (!result)
            {
                logger.Error($"SDL_InitSubSystem Error: {SDL_GetError()}");
                return;
            }

            result = SDL_AddEventWatch(OnSdlEvent, IntPtr.Zero);
            err = SDL_GetError();
            if (!result)
            {
                logger.Error($"SDL_AddEventWatch Error: {SDL_GetError()}");
                return;
            }

            SDL_SetEventEnabled((uint)SDL_EventType.SDL_EVENT_KEY_DOWN, new SDLBool(1));
            SDL_SetEventEnabled((uint)SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN, new SDLBool(1));
        }

        private unsafe bool OnSdlEvent(IntPtr userdata, SDL_Event* evt)
            => OnSdlEvent(userdata, *evt);


        private bool OnSdlEvent(IntPtr userdata, SDL_Event evt)
        {
            switch ((SDL_EventType)evt.type)
            {
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                case SDL_EventType.SDL_EVENT_KEY_UP:
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
                case SDL_EventType.SDL_EVENT_JOYSTICK_BALL_MOTION:
                case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION:
                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
                case SDL_EventType.SDL_EVENT_JOYSTICK_UPDATE_COMPLETE:
                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMAPPED:
                case SDL_EventType.SDL_EVENT_GAMEPAD_TOUCHPAD_DOWN:
                case SDL_EventType.SDL_EVENT_GAMEPAD_TOUCHPAD_MOTION:
                case SDL_EventType.SDL_EVENT_GAMEPAD_TOUCHPAD_UP:
                case SDL_EventType.SDL_EVENT_GAMEPAD_UPDATE_COMPLETE:
                case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                case SDL_EventType.SDL_EVENT_FINGER_UP:
                case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                case SDL_EventType.SDL_EVENT_FINGER_CANCELED:
                case SDL_EventType.SDL_EVENT_SENSOR_UPDATE:
                case SDL_EventType.SDL_EVENT_PEN_DOWN:
                case SDL_EventType.SDL_EVENT_PEN_UP:
                case SDL_EventType.SDL_EVENT_PEN_BUTTON_DOWN:
                case SDL_EventType.SDL_EVENT_PEN_BUTTON_UP:
                case SDL_EventType.SDL_EVENT_PEN_MOTION:
                    _lastInputTimeStampInMs = Environment.TickCount;
                    return true;


                case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
                    break;
                case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
                    break;
                case SDL_EventType.SDL_EVENT_KEYBOARD_ADDED:
                    break;
                case SDL_EventType.SDL_EVENT_KEYBOARD_REMOVED:
                    break;
                case SDL_EventType.SDL_EVENT_MOUSE_ADDED:
                    break;
                case SDL_EventType.SDL_EVENT_MOUSE_REMOVED:
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    break;
            }
            return false;
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
                    logger.Warn($"ScreenSaver Task was in an unexpected state: {_screenSaverTask.Status}");
                    break;
            }
        }

        // I would rather rely on a timer, but that isn't ideal until a event handler for controllers available.
        // Otherwise, we would still need to poll for gamepad input to close the screen saver
        // and I'm not polling and managing a timer together.
        private void PollForInput(bool startImmediately) 
        { 
            try { PollLoop(startImmediately); } 
            catch (Exception e) { logger.Error(e, "Something Went Wrong while running ScreenSaver Poll."); } 
        }

        private void PollLoop(bool startImediately)
        {
            var controllers = new Controller[]
            {
                new Controller(UserIndex.   One),
                new Controller(UserIndex.   Two),
                new Controller(UserIndex. Three),
                new Controller(UserIndex.  Four)
            };

            var packetNumbers = new int?[4];
            _lastScreenChangeTimeStampInMs = 0;
            _lastInputTimeStampInMs = startImediately ? 0 : Environment.TickCount;
            DateTime time = default;

            while (_isPolling)
            {
                UpdateScreenSaverState(ref time);
                Task.Delay(16);

                if (AKeyStateChanged())
                {
                    _lastInputTimeStampInMs = Environment.TickCount;
                }

                var pads = SDL_GetGamepads(out var count);
                if (pads != null && count != 0)
                {
                    var managedArray = new byte[count * 4];
                    Marshal.Copy(pads, managedArray, 0, count * 4);
                    foreach (var pad in managedArray)
                    {
                        var gamepad = SDL_GetGamepadFromID((uint)pad);
                        if (pad != 0)
                        {
                            _lastInputTimeStampInMs = Environment.TickCount;
                            break;
                        }
                    }
                    SDL_free(pads);
                }

                if (SDL_PollEvent(out var sdlEvent) && OnSdlEvent(IntPtr.Zero, sdlEvent))
                {
                    _lastInputTimeStampInMs = Environment.TickCount;
                }

                for (int i = 0; i < 4; i++)
                {
                    var newPacketNumber = 0;
                    var controller = controllers[i];
                    var oldPacketNumber = packetNumbers[i];

                    if (ControllerStateChanged(controller, oldPacketNumber, ref newPacketNumber))
                    {
                        _lastInputTimeStampInMs = Environment.TickCount;
                        packetNumbers[i] = newPacketNumber;
                    }
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

        // Does not support DirectInput (Ps4/5, Switch, etc.). Doesn't matter until Playnite supports it.
        // May eventually ditch SharpDx for dll imports if need for guide button arises.
        private static bool ControllerStateChanged(Controller controller, int? oldPacketNumber, ref int newPacketNumber)
            => controller.IsConnected && (newPacketNumber = controller.GetState().PacketNumber) != oldPacketNumber;

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
