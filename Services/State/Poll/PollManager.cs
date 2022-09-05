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
using static ScreenSaver.Common.Imports.User32;
using Keys = System.Windows.Forms.Keys;
using ScreenSaver.Models;
using Sounds;

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

        private        readonly IDictionary<Keys, bool>                     _keyStates;
        private        readonly IWindowsManager                    _screenSaverManager;

        private                 bool                                        _isPolling;
        private                 int?                                   _timeSinceStart;
        private                 uint                                 _gameIntervalInMs;
        private                 uint                          _ScreenSaverIntervalInMs;

        private                 ScreenSaverSettings                          _settings;
        private                 ISounds                                        _sounds;

        #endregion

        public PollManager(ScreenSaverSettings settings, IWindowsManager screenSaverManager)
        {
            Update(settings);
            _screenSaverManager = screenSaverManager;
            
            // I'm too lazy to type this out. Besides, what if it changes ¯\_(ツ)_/¯
            _keyStates = new Dictionary<Keys, bool>();
            foreach (var key in _keys) _keyStates[key] = false;
        }

        #endregion

        #region Interface

        public void SetupPolling      (ISounds               sounds) =>  Setup  (     sounds);
        public void StartPolling      (bool             immediately) =>  Start  (immediately);
        public void PausePolling      (                            ) =>  Pause  (           );
        public void StopPolling       (                            ) =>   Stop  (           );
        public void UpdateSettings    (ScreenSaverSettings settings) =>  Update (   settings);

        #endregion

        #region Implementation

        #region SetupPolling

        private void Setup(ISounds sounds)
        {
            _sounds = sounds;
            _hookID = SetHook(_proc);
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
                        _timeSinceStart            = Environment.TickCount + 100;
                        _lastScreenChangeTimeStampInMs = Environment.TickCount;
                        _screenSaverManager.StartScreenSaver();
                    }
                    break;
                default:
                    logger.Warn($"ScreenSaver Task was in an unexpected state: {_screenSaverTask.Status}");
                    break;
            }
        }

        // I would rather rely on a timer, but that isn't ideal until a event handler for controllers is discovered.
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

            while (_isPolling)
            {
                UpdateScreenSaverState();
                Task.Delay(16);

                if (AKeyStateChanged())
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

        private void UpdateScreenSaverState()
        {
            var screenSaverIsNotRunning = _timeSinceStart is null;

            if (screenSaverIsNotRunning && TimeToStart)
            {
                if (_sounds != null)
                {
                    Application.Current.Dispatcher.Invoke(_sounds.Pause);
                }

                Application.Current.Dispatcher.Invoke(_screenSaverManager.StartScreenSaver);
                _timeSinceStart = Environment.TickCount + 100;
                _lastScreenChangeTimeStampInMs = Environment.TickCount;
                
            }
            else if (_lastInputTimeStampInMs > _timeSinceStart)
            {
                _timeSinceStart = null;
                Application.Current.Dispatcher.Invoke(_screenSaverManager.StopScreenSaver);
                if (_sounds != null)
                {
                    Application.Current.Dispatcher.Invoke(_sounds.Play);
                }
            }
            else if (!screenSaverIsNotRunning && TimeToUpdate)
            {
                _lastScreenChangeTimeStampInMs = Environment.TickCount;
                Application.Current.Dispatcher.Invoke(_screenSaverManager.UpdateScreenSaver);
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
