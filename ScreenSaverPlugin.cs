using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScreenSaver
{
    public class ScreenSaverPlugin : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ScreenSaverSettingsViewModel settings { get; set; }

        private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string IconPath = Path.Combine(PluginFolder, "icon.png");
        private readonly string ImagesPath;
        private readonly string ExtraMetaDataPath;
        private readonly string SoundsPath;

        private readonly DispatcherTimer screenSaverStateTimer;
        private static Window currentScreenSaverWindow;
        private static Window secondScreenSaverWindow;
        private static Window blackgroundWindow;
        private static IEnumerator<Game> GameEnumerator;
        private static bool first = false;

        private static object screenSaverLock = new object();

        private readonly List<GameMenuItem> _gameMenuItems;
        private readonly List<MainMenuItem> _mainMenuItems;

        private readonly IPlayniteAPI _playniteAPI;

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseWindow(IntPtr hWnd); 
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyWindow(IntPtr hwnd);

        public ScreenSaverPlugin(IPlayniteAPI api) : base(api)
        {
            settings = new ScreenSaverSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            _playniteAPI = api;

            ExtraMetaDataPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "ExtraMetadata\\games");
            SoundsPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, @"ExtensionsData\9c960604-b8bc-4407-a4e4-e291c6097c7d\Music Files\Game");
            ImagesPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "library\\files");

            screenSaverStateTimer = new DispatcherTimer();

            _gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem { Action = ShowScreenSaver, Description = "Open Window",         Icon = IconPath },
                new GameMenuItem { Action = OpenScreenSaver, Description = "Preview ScreenSaver", Icon = IconPath },
            };

            _mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem { Action =  StartScreenSaver, Description = "Start ScreenSaver", Icon = IconPath }
            };
        }

        private void SetTimerToPoll()
        {
            if (Settings.DisablePoll) return;

            screenSaverStateTimer.Interval = TimeSpan.FromSeconds(Settings.PollInterval);
            screenSaverStateTimer.Tick -= UpdateScreenSavers;
            screenSaverStateTimer.Tick += StartScreenSaverIfNoInput;
            screenSaverStateTimer.Start();
        }

        private void SetTimerToTransition()
        {
            screenSaverStateTimer.Interval = TimeSpan.FromSeconds(Settings.GameTransitionInterval);
            screenSaverStateTimer.Tick -= StartScreenSaverIfNoInput;
            screenSaverStateTimer.Tick += UpdateScreenSavers;
            screenSaverStateTimer.Start();
        }

        private static void ShowScreenSaver(GameMenuItemActionArgs args)
        {
            var saver = new ScreenSaverWindow();
            saver.StartMedia();
            saver.Show();

            var processes = Process.GetProcesses();
            logger.Info($"Processes: {string.Join(", ", processes.Select(x => x.ProcessName))}");
            var playnite = processes.Where(x => x.ProcessName.StartsWith("Playnite.")).FirstOrDefault();
            if (playnite != null)
            {
                var r = CloseWindow(playnite.Handle);
                logger.Info($"attempted to close Playnite: {playnite.ProcessName} {r}");
            }
            else
            {
                logger.Info("Unable to find playnite");
            }
        }

        #region Playnite Interface

        public override Guid Id { get; } = Guid.Parse("198510bc-f254-46d5-8ac7-d048e9cd1688");

        public override ISettings GetSettings(bool firstRunSettings) => new ScreenSaverSettingsViewModel(this);

        public override UserControl GetSettingsView(bool firstRunSettings) => new ScreenSaverSettingsView();

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (Settings.DisableWhilePlaying)
            { 
                screenSaverStateTimer.Stop();
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args) => SetTimerToPoll();

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args) => SetTimerToPoll();

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args) => _gameMenuItems;

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args) => _mainMenuItems;

        #endregion

        private void OpenScreenSaver(GameMenuItemActionArgs args)
        {
            if (_playniteAPI.MainView.SelectedGames.Count() == 1)
            {
                CreateScreenSaverWindow(_playniteAPI.MainView.SelectedGames.First());
            }
        }

        private void CreateScreenSaverWindow(Game game)
        {
            screenSaverStateTimer.Tick -= StartScreenSaverIfNoInput;
            screenSaverStateTimer.Stop();

            var gameId = game.Id.ToString();

            var videoPath = GetVideoPath(gameId);
            var musicPath = GetMusicPath(gameId);

            currentScreenSaverWindow = CreateWindow(
                GetBackgroundPath(game.BackgroundImage),
                GetLogoPath(gameId),
                videoPath,
                musicPath);

            var screenSaver = currentScreenSaverWindow.Content as ScreenSaverImage;

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
            screenSaverStateTimer.Stop();

            currentScreenSaverWindow?.Close();
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
            currentScreenSaverWindow = CreateWindow(backgroundPath, logoPath, videoPath, musicPath);
            secondScreenSaverWindow = CreateWindow(null, null, null, null);
            PlayMedia(currentScreenSaverWindow.Content, videoPath, musicPath);

            SetTimerToTransition();
        }

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

        private Window CreateWindow(string backgroundPath, string logoPath, string videoPath, string musicPath)
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

            return window;
        }

        // Based on this implementation: http://www.pinvoke.net/default.aspx/user32.GetLastInputInfo
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public uint cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        static uint GetLastInputTimeMs()
        {
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }

            return idleTime;
        }

        private void StartScreenSaverIfNoInput(object _, object __)
        {
            if (GetLastInputTimeMs() > Settings.ScreenSaverInterval * 1000)
            {
                StartScreenSaver();
            }
        }

        private void UpdateScreenSavers(object _, object __)
        {
            var newWindow = first ? currentScreenSaverWindow : secondScreenSaverWindow;
            var oldWindow = first ? secondScreenSaverWindow : currentScreenSaverWindow;
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
            var fadeInMusic   = CreateFade(newAudioPlayer, MediaElement.VolumeProperty, duration, 0, volume);
            var fadeOutMusic  = CreateFade(oldAudioPlayer, MediaElement.VolumeProperty, duration, volume, 0);

            var storyBoard = new Storyboard
            {
                Children = new TimelineCollection
                {
                    fadeInWindow,
                    fadeOutWindow,
                    fadeInMusic,
                    fadeOutMusic
                }
            };

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

        private void ScreenSaverClosed(object sender, EventArgs _)
        {
            screenSaverStateTimer.Stop();
            blackgroundWindow?.Close();
            
            lock(screenSaverLock)
            {
                if (sender == currentScreenSaverWindow)
                {
                    SetTimerToPoll();
                    secondScreenSaverWindow?.Close();
                }
                else
                {
                    currentScreenSaverWindow?.Close();
                }

                var window = sender as Window;

                window.Closed -= ScreenSaverClosed;

                var content = window.Content as ScreenSaverImage;
                var context = content.DataContext as ScreenSaverViewModel;

                content.VideoPlayer.Stop();
                content.VideoPlayer.Close();
                content.MusicPlayer.Stop();
                content.MusicPlayer.Close();
            }
        }

        #region Helpers

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
            var backgroundPath = Path.Combine(ImagesPath, localPath);
            return File.Exists(backgroundPath)
                ? backgroundPath
                : null;
        }

        private void MuteBackgroundMusic()
        {
            if (PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Fullscreen)
            {
                return;
            }

            if (PlayniteApi.ApplicationSettings.Fullscreen.IsMusicMuted == false)
            {
                PlayniteApi.ApplicationSettings.Fullscreen.IsMusicMuted = true;
            }
        }

        private static readonly Random _Rng = new Random();
        private IEnumerator<Game> GetCurrentGroupEnumerator() 
            => _playniteAPI.Database.Games.OrderBy(_ => _Rng.Next()).GetEnumerator();

        private ScreenSaverSettings Settings => settings.Settings;

        #endregion
    }
}