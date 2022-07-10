using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using Keys = System.Windows.Forms.Keys;

namespace ScreenSaver
{
    public class ScreenSaverPlugin : GenericPlugin
    {
        #region Infrastructure

        private static readonly ILogger logger = LogManager.GetLogger();

        private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string IconPath = Path.Combine(PluginFolder, "icon.png");

        private static object screenSaverLock = new object();
        private static Task _screenSaverTask;
        private static Window firstScreenSaverWindow;
        private static Window secondScreenSaverWindow;
        private static Window blackgroundWindow;
        private static IEnumerator<Game> GameEnumerator;

        private static bool first = false;
        private static bool IsPolling;

        private static int? TimeSinceStart = null;
        private static int? _lastInputTimeStampInMs = null;

        private ScreenSaverSettingsViewModel settings { get; set; }
        private readonly string ImagesPath;
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
            settings = new ScreenSaverSettingsViewModel(this);
            Properties = new GenericPluginProperties { HasSettings = true };
            _playniteAPI = api;

            ExtraMetaDataPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "ExtraMetadata\\games");
            SoundsPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, @"ExtensionsData\9c960604-b8bc-4407-a4e4-e291c6097c7d\Music Files\Game");
            ImagesPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "library\\files");

            _gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem { Action = OpenScreenSaver, Description = "Preview ScreenSaver", Icon = IconPath },
            };

            _mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem { Action =  StartScreenSaver, Description = "Start ScreenSaver", Icon = IconPath }
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
            StartPolling();
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
            int lastChangeTimeStamp = 0;

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
                        TimeSinceStart = Environment.TickCount;
                        lastChangeTimeStamp = Environment.TickCount;
                        Application.Current.Dispatcher.Invoke(() => StartScreenSaver());
                    }
                }
                else if (_lastInputTimeStampInMs > TimeSinceStart)
                {
                    TimeSinceStart = null;
                    Application.Current.Dispatcher.Invoke(() => firstScreenSaverWindow.Close());
                }
                else if (Environment.TickCount - lastChangeTimeStamp > Settings.GameTransitionInterval * 1000)
                lock (screenSaverLock) if (TimeSinceStart != null)
                {
                    lastChangeTimeStamp = Environment.TickCount;
                    Application.Current.Dispatcher.Invoke(() => UpdateScreenSavers(null, null));
                }
            }

            // Close the ScreenSaver, just in case
            Application.Current.Dispatcher.Invoke(() => firstScreenSaverWindow.Close());
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

        private void StartPolling()
        {
            switch (_screenSaverTask?.Status)
            {
                case null:
                case TaskStatus.RanToCompletion:
                    if (ShouldPoll())
                    {
                        IsPolling = true;
                        _screenSaverTask = Task.Run(() => PollForInput());
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

        private void StopPolling()
        {
            IsPolling = false;
            if (!_screenSaverTask.Wait(100))
            {
                logger.Error($"Unable to stop polling task on exit.");
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

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            if (Settings.PauseOnMinimize) switch (Application.Current?.MainWindow?.WindowState)
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

        #endregion

        #region Windows

        private void CreateScreenSaverWindow(Game game)
        {
            var gameId = game.Id.ToString();

            var videoPath = GetVideoPath(gameId);
            var musicPath = GetMusicPath(gameId);

            firstScreenSaverWindow = CreateScreenSaverLayerWindow(
                GetBackgroundPath(game.BackgroundImage),
                GetLogoPath(gameId),
                videoPath,
                musicPath);

            var screenSaver = firstScreenSaverWindow.Content as ScreenSaverImage;

            PlayVideo(screenSaver.VideoPlayer, musicPath);
            PlayAudio(screenSaver.MusicPlayer, videoPath);
        }

        private void StartScreenSaver(object _ = null)
        {
            GameEnumerator = GetCurrentGroupEnumerator();

            GetNextGameContent(
                out var background,
                out var logoPath,
                out var videoPath,
                out var musicPath);

            CreateScreenSaverWindows(background, logoPath, videoPath, musicPath);
        }


        private void CreateScreenSaverWindows(string backgroundPath, string logoPath, string videoPath, string musicPath)
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
            secondScreenSaverWindow = CreateScreenSaverLayerWindow(null, null, null, null);
            firstScreenSaverWindow = CreateScreenSaverLayerWindow(backgroundPath, logoPath, videoPath, musicPath);
            PlayMedia(firstScreenSaverWindow.Content, videoPath, musicPath);
        }

        private Window CreateScreenSaverLayerWindow(string backgroundPath, string logoPath, string videoPath, string musicPath)
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
                    BackgroundPath = backgroundPath,
                    LogoPath = logoPath,
                    VideoPath = videoPath,
                    MusicPath = musicPath
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

            GetNextGameContent(
                out var background, 
                out var videPath,
                out var musicPath,
                out var logoPath);

            var newContent = newWindow.Content as ScreenSaverImage;
            var context = newContent.DataContext as ScreenSaverViewModel;

            context.BackgroundPath = background;
            newContent.BackgroundImage.Source = background == null
                ? null
                : new BitmapImage(new Uri(background));

            if (Settings.IncludeLogo)
            {
                context.LogoPath = logoPath;
                newContent.LogoImage.Source = logoPath == null
                    ? null
                    : new BitmapImage(new Uri(logoPath));
            }

            if (Settings.IncludeVideo)
            {
                context.VideoPath = videPath;
                if (videPath != null)
                {
                    newContent.VideoPlayer.Visibility = Visibility.Visible;
                    newContent.VideoPlayer.Source = new Uri(videPath);
                }
                else
                {
                    newContent.VideoPlayer.Visibility = Visibility.Hidden;
                    newContent.VideoPlayer.Source = null;
                }
            }

            if (Settings.AudioSource is AudioSource.Music)
            {
                context.MusicPath = musicPath;
                newContent.MusicPlayer.Source = musicPath != null ? new Uri(musicPath) : null;
            }

            var duration = new Duration(TimeSpan.FromSeconds(1));
            var volume = Settings.Volume / 100.0;

            var shouldPlayVideoAudio = ShouldPlayVideoAudio(musicPath);
            var oldContent = oldWindow.Content as ScreenSaverImage;

            var newAudioPlayer = shouldPlayVideoAudio ? newContent.VideoPlayer : newContent.MusicPlayer;
            var oldAudioPlayer = shouldPlayVideoAudio ? oldContent.VideoPlayer : oldContent.MusicPlayer;

            var fadeInWindow  = CreateFade(newWindow,      UIElement.OpacityProperty,   duration, 0,      1);
            var fadeOutWindow = CreateFade(oldWindow,      UIElement.OpacityProperty,   duration, 1,      0);
            var fadeInAudio   = CreateFade(newAudioPlayer, MediaElement.VolumeProperty, duration, 0, volume);
            var fadeOutAudio  = CreateFade(oldAudioPlayer, MediaElement.VolumeProperty, duration, volume, 0);

            var storyBoard = new Storyboard
            { Children = new TimelineCollection { fadeInWindow, fadeOutWindow, fadeInAudio, fadeOutAudio } };

            PlayVideo(newContent.VideoPlayer, musicPath);
            PlayAudio(newContent.MusicPlayer, videPath);

            storyBoard.Begin();
        }

        private void GetNextGameContent(
            out string backgroundPath, out string videoPath, out string musicPath, out string logoPath)
        {
            do
            {
                if (!GameEnumerator.MoveNext())
                {
                    // Reset throws NotImplementedException as of 7/5/22
                    GameEnumerator = GetCurrentGroupEnumerator();
                    if (!GameEnumerator.MoveNext())
                    {
                        logger.Warn("No games found while rendering ScreenSaver");
                        backgroundPath = videoPath = musicPath = logoPath = null;
                        return;
                    }
                }

                var newGame = GameEnumerator.Current;
                var gameId = newGame.Id.ToString();

                backgroundPath = GetBackgroundPath(newGame.BackgroundImage);
                musicPath = GetMusicPath(gameId);
                logoPath  = GetLogoPath(gameId);
                videoPath = GetVideoPath(gameId);
            } while ((Settings.VideoSkip      && videoPath      is null) ||
                     (Settings.MusicSkip      && musicPath      is null) ||
                     (Settings.LogoSkip       && logoPath       is null) ||
                     (Settings.BackgroundSkip && backgroundPath is null));
        }

        private void ScreenSaverClosed(object sender, EventArgs _)
        {
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

        private string GetBackgroundPath(string localPath)
        {
            if (localPath is null) return null;
            var backgroundPath = Path.Combine(ImagesPath, localPath);
            return File.Exists(backgroundPath)
                ? backgroundPath
                : null;
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

        private static readonly Random _Rng = new Random();
        private IEnumerator<Game> GetCurrentGroupEnumerator() 
            => _playniteAPI.Database.Games.OrderBy(_ => _Rng.Next()).GetEnumerator();

        private ScreenSaverSettings Settings => settings.Settings;

        #endregion
    }
}