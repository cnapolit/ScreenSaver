using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using ScreenSaver.Common.Constants;
using SharpDX.XInput;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Keys = System.Windows.Forms.Keys;

namespace ScreenSaver
{
    public class ScreenSaverPlugin : GenericPlugin
    {
        #region Infrastructure

        private static readonly ILogger logger = LogManager.GetLogger();

        private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string IconPath = Path.Combine(PluginFolder, "icon.png");

        private static readonly object screenSaverLock = new object();

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static Task _screenSaverTask;
        private static Window firstScreenSaverWindow;
        private static Window secondScreenSaverWindow;
        private static Window blackgroundWindow;
        private static IEnumerator<GameContent> GameEnumerator;

        private static bool first;
        private static bool IsPolling;

        private static int? TimeSinceStart = null;
        private static int? _lastInputTimeStampInMs = null;
        private static int _lastChangeTimeStamp;

        private ScreenSaverSettingsViewModel settings { get; set; }
        private readonly string ExtraMetaDataPath;
        private readonly string SoundsPath;

        private readonly List<GameMenuItem> _gameMenuItems;
        private readonly List<MainMenuItem> _mainMenuItems;

        private readonly IPlayniteAPI _playniteAPI;

        private static readonly Keys[] badKeys = new[] { Keys.KeyCode, Keys.Modifiers, Keys.None, Keys.Packet };
        private static readonly IList<Keys> _keys = 
            Enum.GetValues(typeof(Keys)).Cast<Keys>().Where(k => !badKeys.Contains(k)).ToList();
        private readonly IDictionary<Keys, bool> _keyStates = new Dictionary<Keys, bool>();

        public ScreenSaverPlugin(IPlayniteAPI api) : base(api)
        {
            Localization.SetPluginLanguage(PluginFolder, api.ApplicationSettings.Language);
            settings = new ScreenSaverSettingsViewModel(this);
            Properties = new GenericPluginProperties { HasSettings = true };
            _playniteAPI = api;

            ExtraMetaDataPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "ExtraMetadata\\games");
            SoundsPath = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, @"9c960604-b8bc-4407-a4e4-e291c6097c7d\Music Files\Game");

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

            foreach (var key in _keys)
            {
                 _keyStates[key] = false;
            }
        }

        #endregion

        #region Playnite Interface

        public override Guid Id { get; } = Guid.Parse("198510bc-f254-46d5-8ac7-d048e9cd1688");

        public override ISettings GetSettings(bool firstRunSettings) => new ScreenSaverSettingsViewModel(this);

        public override UserControl GetSettingsView(bool firstRunSettings) => new ScreenSaverSettingsView();

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            IsPolling = false;
            UnhookWindowsHookEx(_hookID);
            if (!_screenSaverTask.Wait(1000))
            {
                logger.Error($"Unable to stop polling task on exit.");
            }
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (Settings.DisableWhilePlaying)
            {
                StopPolling();
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args) => StartPolling();

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            _hookID = SetHook(_proc);
            StartPolling();

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            Application.Current.Deactivated += OnApplicationDeactivate;
            Application.Current.Activated += OnApplicationActivate;
            Application.Current.MainWindow.StateChanged += OnWindowStateChanged;
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args) => _gameMenuItems;
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args) => _mainMenuItems;

        #endregion

        #region Polling

        private void PollForInput()
        {
            var controller = new Controller(UserIndex.One);
            int? oldPacketNumber = null;
            _lastChangeTimeStamp = 0;
            _lastInputTimeStampInMs = Environment.TickCount;

            while (IsPolling)
            {
                if (ControllerStateChanged(controller, oldPacketNumber, out var newPacketNumber))
                {
                    _lastInputTimeStampInMs = Environment.TickCount;
                    oldPacketNumber = newPacketNumber;
                }
                else if (KeyStateChanged())
                {
                    _lastInputTimeStampInMs = Environment.TickCount;
                }

                var screenSaverIsRunning = TimeSinceStart is null;
                if (screenSaverIsRunning)
                {
                    var timeSinceLastInput = Environment.TickCount - _lastInputTimeStampInMs;
                    if (timeSinceLastInput > Settings.ScreenSaverInterval * 1000)
                    {
                        TimeSinceStart = Environment.TickCount + 100;
                        _lastChangeTimeStamp = Environment.TickCount;
                        Application.Current.Dispatcher.Invoke(() => StartScreenSaver());
                    }
                }
                else if (_lastInputTimeStampInMs > TimeSinceStart)
                {
                    TimeSinceStart = null;
                    Application.Current.Dispatcher.Invoke(() => firstScreenSaverWindow?.Close());
                }
                else if (Environment.TickCount - _lastChangeTimeStamp > Settings.GameTransitionInterval * 1000)
                lock (screenSaverLock) if (TimeSinceStart != null)
                {
                    _lastChangeTimeStamp = Environment.TickCount;
                    Application.Current.Dispatcher.Invoke(() => UpdateScreenSavers(null, null));
                }
            }
        }

        private bool KeyStateChanged()
        {
            var keyChanged = false;
            foreach(var key in _keys)
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

        private static bool ControllerStateChanged(Controller controller, int? oldPacketNumber, out int newPacketNumber)
        {
            newPacketNumber = 0;
            if (controller.IsConnected)
            {
                newPacketNumber = controller.GetState().PacketNumber;
                return oldPacketNumber != newPacketNumber;
            }
            return false;
        }

        public static bool IsKeyPushedDown(Keys key) => 0 != (GetAsyncKeyState(key) & 0x8000);

        public void StartPolling()
        {
            switch (_screenSaverTask?.Status)
            {
                case null:
                case TaskStatus.RanToCompletion:
                    if (ShouldPoll())
                    {
                        IsPolling = true;
                        _screenSaverTask = Task.Run(PollForInput);
                    }
                    break;
                case TaskStatus.Created:
                case TaskStatus.Running:
                case TaskStatus.WaitingToRun:
                case TaskStatus.WaitingForActivation:
                    break;
                default:
                    logger.Warn($"ScreenSaver Task was in an unexpected state: {_screenSaverTask.Status}");
                    break;
            }
        }

        public void StopPolling()
        {
            IsPolling = false;
            if (_screenSaverTask?.Wait(100) ?? true)
            {
                logger.Info($"Stopped polling task.");
            }
            else
            {
                logger.Error($"Unable to stop polling task.");
            }
        }

        private bool ShouldPoll()
        {
            var desktopMode = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop;

            var playOnBoth = Settings.PlayState == PlayState.Always;
            var playOnFullScreen = Settings.PlayState == PlayState.FullScreen && !desktopMode;
            var playOnDesktop = Settings.PlayState == PlayState.Desktop && desktopMode;

            return playOnBoth || playOnFullScreen || playOnDesktop;
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                _lastInputTimeStampInMs = Environment.TickCount;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            if (Settings.PauseOnDeactivate) switch (Application.Current?.MainWindow?.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    StartPolling();
                    break;
                case WindowState.Minimized:
                    StopPolling();
                    break;
            }
        }

        private void OnApplicationDeactivate(object sender, EventArgs e)
        {
            if (Settings.PauseOnDeactivate)
            {
                StopPolling();
            }
        }

        private void OnApplicationActivate(object sender, EventArgs e)
        {
            if (Settings.PauseOnDeactivate)
            {
                StartPolling();
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            if (args.Mode == PowerModes.Resume)
            {
                StartPolling();
            }
        }

        #endregion

        #region Windows

        private void CreateScreenSaverWindow(Game game)
        {
            var gameContent = ConstructGameContent(game);

            firstScreenSaverWindow = CreateScreenSaverLayerWindow(gameContent);

            var screenSaver = firstScreenSaverWindow.Content as ScreenSaverImage;

            PlayVideo(screenSaver.VideoPlayer, gameContent.MusicPath);
            PlayAudio(screenSaver.MusicPlayer, gameContent.VideoPath);
        }

        private void ManuallyStartScreenSaver(object _ = null)
        {
            TimeSinceStart = Environment.TickCount + 100;
            _lastChangeTimeStamp = Environment.TickCount;
            StartScreenSaver();

            // Kick off the poll if it isn't running already
            StartPolling();
        }

        private void StartScreenSaver()
        {
            GameEnumerator = GetGameContentEnumerator();

            var gameContent = GetNextGameContent();

            CreateScreenSaverWindows(gameContent);
        }


        private void CreateScreenSaverWindows(GameContent gameContent)
        {
            firstScreenSaverWindow?.Close();
            secondScreenSaverWindow?.Close();
            MuteBackgroundMusic();

            blackgroundWindow = new Window
            {
                Background = Brushes.Black,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Focusable = false,
                //Topmost = true
            };
            blackgroundWindow.Show();

            secondScreenSaverWindow = CreateScreenSaverLayerWindow(null);
            firstScreenSaverWindow = CreateScreenSaverLayerWindow(gameContent);

            PlayMedia(firstScreenSaverWindow.Content, gameContent.VideoPath, gameContent.MusicPath);
        }

        private Window CreateScreenSaverLayerWindow(GameContent gameContent)
        {
            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Focusable = true,
                // Window is set to topmost to make sure another window won't show over it
                //Topmost = true,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var content = new ScreenSaverImage
            {
                ParentWindow = window,
                DataContext = new ScreenSaverViewModel
                {
                    Settings = Settings,
                    BackgroundPath = gameContent?.BackgroundPath,
                    LogoPath = gameContent?.LogoPath,
                    VideoPath = gameContent?.VideoPath,
                    MusicPath = gameContent?.MusicPath
                }
            };

            var volume = Settings.Volume / 100.0;
            content.VideoPlayer.Volume = volume;
            content.MusicPlayer.Volume = volume;
            content.LogoImage.Visibility = Settings.IncludeLogo ? Visibility.Visible : Visibility.Hidden;

            window.Content = content;
            window.Closed += ScreenSaverClosed;

            window.Show();
            window.Focus();

            return window;
        }

        private void UpdateScreenSavers(object _, object __)
        {
            var newWindow = first ? firstScreenSaverWindow : secondScreenSaverWindow;
            var oldWindow = first ? secondScreenSaverWindow : firstScreenSaverWindow;
            first = !first;

            var gameContent = GetNextGameContent();

            var newContent = newWindow.Content as ScreenSaverImage;
            var context = newContent.DataContext as ScreenSaverViewModel;

            context.BackgroundPath = gameContent.BackgroundPath;
            newContent.BackgroundImage.Source = gameContent.BackgroundPath is null
                ? null
                : new BitmapImage(new Uri(gameContent.BackgroundPath));

            if (Settings.IncludeLogo)
            {
                context.LogoPath = gameContent.LogoPath;
                newContent.LogoImage.Source = gameContent.LogoPath is null
                    ? null
                    : new BitmapImage(new Uri(gameContent.LogoPath));
            }

            if (Settings.IncludeVideo)
            {
                context.VideoPath = gameContent.VideoPath;
                if (gameContent.VideoPath != null)
                {
                    newContent.VideoPlayer.Visibility = Visibility.Visible;
                    newContent.VideoPlayer.Source = new Uri(gameContent.VideoPath);
                }
                else
                {
                    newContent.VideoPlayer.Visibility = Visibility.Hidden;
                    newContent.VideoPlayer.Source = null;
                }
            }

            if (Settings.AudioSource is AudioSource.Music)
            {
                context.MusicPath = gameContent.MusicPath;
                newContent.MusicPlayer.Source = gameContent.MusicPath is null ? null: new Uri(gameContent.MusicPath);
            }

            var duration = new Duration(TimeSpan.FromSeconds(1));
            var volume = Settings.Volume / 100.0;

            var oldContent = oldWindow.Content as ScreenSaverImage;

            var fadeInWindow  = CreateFade(newWindow,              UIElement.OpacityProperty,   duration, 0,      1);
            var fadeOutWindow = CreateFade(oldWindow,              UIElement.OpacityProperty,   duration, 1,      0);
            var fadeInVideo   = CreateFade(newContent.VideoPlayer, MediaElement.VolumeProperty, duration, 0, volume);
            var fadeOutVideo  = CreateFade(oldContent.VideoPlayer, MediaElement.VolumeProperty, duration, volume, 0);
            var fadeInMusic   = CreateFade(newContent.MusicPlayer, MediaElement.VolumeProperty, duration, 0, volume);
            var fadeoutMusic  = CreateFade(oldContent.MusicPlayer, MediaElement.VolumeProperty, duration, volume, 0);

            var storyBoard = new Storyboard
            { 
                Children = new TimelineCollection 
                {
                    fadeInWindow,
                    fadeOutWindow,
                    fadeInVideo,
                    fadeOutVideo,
                    fadeInMusic,
                    fadeoutMusic
                }
            };

            PlayVideo(newContent.VideoPlayer, gameContent.MusicPath);
            PlayAudio(newContent.MusicPlayer, gameContent.VideoPath);

            storyBoard.Begin();
        }

        private GameContent GetNextGameContent()
        {
                if (!GameEnumerator.MoveNext())
                {
                    // Reset throws NotImplementedException as of 7/5/22
                    GameEnumerator = GetGameContentEnumerator();
                    if (!GameEnumerator.MoveNext())
                    {
                        logger.Warn("No games found while rendering ScreenSaver");
                        return null;
                    }
                }

                return GameEnumerator.Current;
        }

        private void ScreenSaverClosed(object sender, EventArgs _)
        {
            // Could have been manually started, which does not guarantee we should continue polling
            if (!ShouldPoll())
            {
                StopPolling();
            }

            lock (screenSaverLock)
            {
                blackgroundWindow?.Close();
                var window = sender as Window;

                window.Closed -= ScreenSaverClosed;

                var content = window.Content as ScreenSaverImage;
                var context = content.DataContext as ScreenSaverViewModel;

                content.VideoPlayer.Stop();
                content.VideoPlayer.Close();
                content.MusicPlayer.Stop();
                content.MusicPlayer.Close();

                if (sender == firstScreenSaverWindow)
                {
                    StartPolling();
                    UnMuteBackgroundMusic();
                    secondScreenSaverWindow?.Close();
                    firstScreenSaverWindow = null;
                }
                else
                {
                    firstScreenSaverWindow?.Close();
                    secondScreenSaverWindow = null;
                }
                TimeSinceStart = null;
            }
        }

        #region Media

        private void PlayMedia(object content, string videoPath, string musicPath)
        {
            var screenSaver = content as ScreenSaverImage;
            PlayVideo(screenSaver.VideoPlayer, musicPath);
            PlayAudio(screenSaver.MusicPlayer, videoPath);
        }

        private void PlayVideo(MediaElement videoPlayer, string musicPath)
        {
            if (!Settings.IncludeVideo) return;

            if (ShouldPlayVideoAudio(musicPath))
            {
                videoPlayer.IsMuted = false;
            }

            videoPlayer.Play();
        }

        private void PlayAudio(MediaElement musicPlayer, string videoPath)
        {
            if (Settings.AudioSource == AudioSource.Music || (Settings.PlayBackup && videoPath is null))
            {
                musicPlayer.Play();
            }
        }

        private bool ShouldPlayVideoAudio(string musicPath)
            => Settings.AudioSource == AudioSource.Video || (Settings.PlayBackup && musicPath is null);

        private DoubleAnimation CreateFade(DependencyObject window, object property, Duration duration, double from, double to)
        {
            var fade = new DoubleAnimation()
            {
                Duration = duration,
                From = from,
                To = to,
                BeginTime = TimeSpan.Zero
            };

            Storyboard.SetTargetProperty(fade, new PropertyPath(property));
            Storyboard.SetTarget(fade, window);

            return fade;
        }

        private string GetLogoPath(string gameId)
        {
            var logoPathSearch = Path.Combine(ExtraMetaDataPath, gameId.ToString(), "Logo.png");
            return File.Exists(logoPathSearch)
                ? logoPathSearch
                : null;
        }

        private string GetVideoPath(string gameId)
        {
            var videoPath = Path.Combine(ExtraMetaDataPath, gameId.ToString(), "VideoTrailer.mp4");
            return File.Exists(videoPath)
                ? videoPath
                : null;
        }

        private string GetMusicPath(string gameId)
        {
            var soundDirectory = Path.Combine(SoundsPath, gameId.ToString());
            return Directory.Exists(soundDirectory)
                ? Directory.GetFiles(soundDirectory).FirstOrDefault()
                : null;
        }

        private string GetBackgroundPath(Game game)
        {
            if (game.BackgroundImage != null && !game.BackgroundImage.StartsWith("http"))
            {
                return PlayniteApi.Database.GetFullFilePath(game.BackgroundImage);
            }

            if (game.Platforms.HasItems() && game.Platforms[0].Background != null)
            {
                return PlayniteApi.Database.GetFullFilePath(game.Platforms[0].Background);
            }

            return null;
        }

        #endregion

        #endregion

        #region Menus

        #region Game Menu

        private void OpenScreenSaver(GameMenuItemActionArgs args)
        {
            if (_playniteAPI.MainView.SelectedGames.Count() == 1)
            {
                StopPolling();
                CreateScreenSaverWindow(_playniteAPI.MainView.SelectedGames.First());
                firstScreenSaverWindow.Closed += (_, __) => StartPolling();
            }
        }

        #endregion

        #region Main Menu



        #endregion

        #endregion

        #region Imported Methods

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        #endregion

        #region Helpers

        private void MuteBackgroundMusic()
        {
            if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                PlayniteApi.ApplicationSettings.Fullscreen.IsMusicMuted = true;
            }
        }

        private void UnMuteBackgroundMusic()
        {
            if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                PlayniteApi.ApplicationSettings.Fullscreen.IsMusicMuted = false;
            }
        }


        private class GameContent
        {
            public string Id { get; set; }
            public string GameName { get; set; }
            public string LogoPath { get; set; }
            public string MusicPath { get; set; }
            public string VideoPath { get; set; }
            public string BackgroundPath { get; set; }
        }

        private GameContent ConstructGameContent(Game game)
        {
            var id = game.Id.ToString();
            return new GameContent
            {
                Id             = id,
                GameName       = game.Name,
                LogoPath       = GetLogoPath(id),
                MusicPath      = GetMusicPath(id),
                VideoPath      = GetVideoPath(id),
                BackgroundPath = GetBackgroundPath(game),
            };
        }

        private bool ValidGameContent(GameContent gameContent)
            => (!Settings.VideoSkip      || gameContent.VideoPath      != null) &&
               (!Settings.MusicSkip      || gameContent.MusicPath      != null) &&
               (!Settings.LogoSkip       || gameContent.LogoPath       != null) &&
               (!Settings.BackgroundSkip || gameContent.BackgroundPath != null);


        private static readonly Random _Rng = new Random();
        private IEnumerator<GameContent> GetGameContentEnumerator()
            => _playniteAPI.Database.Games.
            Select(ConstructGameContent).
            Where(ValidGameContent).
            OrderBy(_ => _Rng.Next()).
            GetEnumerator();

        private ScreenSaverSettings Settings => settings.Settings;

        #endregion
    }
}