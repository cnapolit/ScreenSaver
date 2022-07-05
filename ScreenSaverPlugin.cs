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
        private readonly DispatcherTimer timerCloseWindow;

        private static Window currentScreenSaverWindow;
        private static Window secondScreenSaverWindow;
        private static Window blackgroundWindow;
        private static int currentGameIndex;
        private static bool first = false;

        private static object screenSaverLock = new object();


        private readonly List<GameMenuItem> _gameMenuItems;

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

            //videoWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            timerCloseWindow = new DispatcherTimer();
            timerCloseWindow.Interval = TimeSpan.FromSeconds(10);
            timerCloseWindow.Tick += (_, __) => UpdateScreenSavers();

            _gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem { Action = ShowScreenSaver, Description = "Open Window", Icon = IconPath },
                new GameMenuItem { Action = OpenScreenSaver, Description = "Open ScreenSaver", Icon = IconPath },
            };
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

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {

        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {

        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {

        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args) => _gameMenuItems;

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args) => new List<MainMenuItem>();

        #endregion

        private void ResetTimer()
        {

        }

        private void EnableTimer()
        {

        }

        private void DisableTimer()
        {

        }

        private void OpenScreenSaver(GameMenuItemActionArgs args)
        {
            if (_playniteAPI.MainView.SelectedGames.Count() == 1)
            {
                CreateScreenSaverWindow(_playniteAPI.MainView.SelectedGames.First());
            }
        }

        private void CreateScreenSaverWindow(Game game)
        {
            var gameList = _playniteAPI.Database.Games.ToList();
            var gameId = game.Id.ToString();
            currentGameIndex = gameList.IndexOf(game);

            CreateScreenSaverWindows(
                GetBackgroundPath(game.BackgroundImage),
                GetLogoPath(gameId),
                GetVideoPath(gameId),
                GetMusicPath(gameId));
        }

        private void CreateScreenSaverWindows(string backgroundPath, string logoPath, string videoPath, string musicPath)
        {
            // Mutes Playnite Background music to make sure its not playing when video or splash screen image
            // is active and prevents music not stopping when game is already running
            timerCloseWindow.Stop();

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
                Topmost = true
            };
            blackgroundWindow.Show();
            currentScreenSaverWindow = CreateWindow(backgroundPath, logoPath, videoPath, musicPath);
            secondScreenSaverWindow = CreateWindow(null, null, null, null);

            var screenSaver = currentScreenSaverWindow.Content as ScreenSaverImage;
            screenSaver.VideoPlayer.Play();
            screenSaver.MusicPlayer.Play();

            timerCloseWindow.Stop();
            timerCloseWindow.Start();
        }

        private Window CreateWindow(string backgroundPath, string logoPath, string videoPath, string musicPath)
        {
            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Focusable = false,
                // Window is set to topmost to make sure another window won't show over it
                Topmost = true,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var content = new ScreenSaverImage
            {
                ParentWindow = window,
                DataContext = new ScreenSaverViewModel
                {
                    Settings = settings.Settings,
                    BackgroundPath = backgroundPath,
                    LogoPath = logoPath,
                    VideoPath = videoPath,
                    MusicPath = musicPath
                }
            };

            window.Content = content;
            window.Closed += ScreenSaverClosed;

            window.Show();

            return window;
        }

        private void UpdateScreenSavers()
        {
            var newWindow = first ? currentScreenSaverWindow : secondScreenSaverWindow;
            var oldWindow = first ? secondScreenSaverWindow : currentScreenSaverWindow;
            first = !first;

            currentGameIndex = (currentGameIndex + 1) % PlayniteApi.Database.Games.Count();
            var newGame = PlayniteApi.Database.Games.ElementAt(currentGameIndex);
            var gameId = newGame.Id.ToString();

            var newContent = newWindow.Content as ScreenSaverImage;
            var context = newContent.DataContext as ScreenSaverViewModel;
            var oldContent = oldWindow.Content as ScreenSaverImage;

            var background = GetBackgroundPath(newGame.BackgroundImage);
            context.BackgroundPath = background;
            newContent.BackgroundImage.Source = background == null
                ? null
                : new BitmapImage(new Uri(background));

            var lPath = GetLogoPath(gameId);
            context.LogoPath = lPath;
            newContent.LogoImage.Source = lPath == null
                ? null
                : new BitmapImage(new Uri(lPath));

            var vPath = GetVideoPath(gameId);
            context.VideoPath = vPath;
            if (vPath != null)
            {
                newContent.VideoPlayer.Visibility = Visibility.Visible;
                newContent.VideoPlayer.Source = new Uri(vPath);
            }
            else
            {
                newContent.VideoPlayer.Visibility = Visibility.Hidden;
                newContent.VideoPlayer.Source = null;
            }

            var mPath = GetMusicPath(gameId);
            context.MusicPath = mPath;
            newContent.MusicPlayer.Source = mPath != null ? new Uri(mPath) : null;

            var duration = new Duration(TimeSpan.FromSeconds(1));

            var fadeInWindow  = CreateFade(newWindow,              UIElement.OpacityProperty,   duration, 0,   1);
            var fadeOutWindow = CreateFade(oldWindow,              UIElement.OpacityProperty,   duration, 1,   0);
            var fadeInMusic   = CreateFade(newContent.MusicPlayer, MediaElement.VolumeProperty, duration, 0,   0.5);
            var fadeOutMusic  = CreateFade(oldContent.MusicPlayer, MediaElement.VolumeProperty, duration, 0.5, 0);

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

            newContent.MusicPlayer.Play();
            newContent.VideoPlayer.Play();

            storyBoard.Begin();
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

        private void ScreenSaverClosed(object sender, EventArgs e)
        {
            timerCloseWindow.Stop();
            blackgroundWindow.Close();
            
            lock(screenSaverLock)
            {
                if (sender == currentScreenSaverWindow)
                {
                    secondScreenSaverWindow?.Close();
                }
                else
                {
                    currentScreenSaverWindow?.Close();
                }

                if (currentScreenSaverWindow != null)
                {
                    currentScreenSaverWindow.Closed -= ScreenSaverClosed;

                    var content = currentScreenSaverWindow.Content as ScreenSaverImage;
                    var context = content.DataContext as ScreenSaverViewModel;

                    content.VideoPlayer.Stop();
                    content.VideoPlayer.Close();
                    content.MusicPlayer.Stop();
                    content.MusicPlayer.Close();

                    currentScreenSaverWindow = null;
                }

                if (secondScreenSaverWindow != null)
                {
                    secondScreenSaverWindow.Closed -= ScreenSaverClosed;

                    var secondContent = secondScreenSaverWindow.Content as ScreenSaverImage;

                    secondContent.VideoPlayer.Stop();
                    secondContent.VideoPlayer.Close();
                    secondContent.MusicPlayer.Stop();
                    secondContent.MusicPlayer.Close();

                    secondScreenSaverWindow = null;
                }
            }
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
    }
}