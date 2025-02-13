﻿using Playnite.SDK;
using Playnite.SDK.Models;
using ScreenSaver.Common.Constants;
using ScreenSaver.Common.Extensions;
using ScreenSaver.Models;
using ScreenSaver.Models.Enums;
using ScreenSaver.Views.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ScreenSaver.Services.UI.Windows
{
    internal class WindowsManager : IWindowsManager
    {
        #region Infrastructure

        #region Variables

        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly Random _rng = new Random();
        private static readonly object screenSaverLock = new object();
        private static readonly Duration duration = new Duration(TimeSpan.FromSeconds(1));

        private readonly IPlayniteAPI _playniteApi;
        private readonly IGameContentFactory _gameContentFactory;
        private readonly IGameGroupManager _gameGroupManager;
        private readonly Action _onStopCallBack;

        private bool showFirstWindow;

        private ScreenSaverSettings _settings;
        private Window firstScreenSaverWindow;
        private Window secondScreenSaverWindow;
        private Window blackgroundWindow;

        private IEnumerator<GameContent> GameEnumerator;

        #endregion

        public WindowsManager(IPlayniteAPI playniteApi, IGameGroupManager gameGroupManager, ScreenSaverSettings settings, Action onStopCallBack)
        {
            _settings = settings;
            _playniteApi = playniteApi;
            _onStopCallBack = onStopCallBack;
            _gameGroupManager = gameGroupManager;
            _gameContentFactory = new GameContentFactory(playniteApi, settings);
        }

        #endregion

        #region Interface

        public void StartScreenSaver() => Start();
        public void StopScreenSaver() => Stop();
        public void UpdateScreenSaver() => Update();
        public void UpdateScreenSaverTime() => UpdateTime();
        public void PreviewScreenSaver(Game game, Action closeCallBack) => Preview(game, closeCallBack);
        public void UpdateSettings(ScreenSaverSettings settings) => UpdateSetting(settings);

        #endregion

        #region Implementation

        #region StartScreenSaver

        private void Start()
        {
            if (!InitializeEnumerator())
            {
                logger.Warn("No games found while starting ScreenSaver");
                return;
            }

            var gameContent = GameEnumerator.Current;

            firstScreenSaverWindow?.Close();
            secondScreenSaverWindow?.Close();
            MuteBackgroundMusic();

            var ratio = CalculateScreenRatio();
            var screen = Screen.AllScreens[_settings.ScreenIndex];
            logger.Info($"Displaying ScreenSaver to screen '{screen.DeviceName}' with dimensions {screen.WorkingArea.Width}X{screen.WorkingArea.Height}, top position '{screen.WorkingArea.Top}', and left position '{screen.WorkingArea.Left}'");

            blackgroundWindow = new Window
            {
                Background = Brushes.Black,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Focusable = false,
                AllowsTransparency = true,
                Opacity = 0,
                Topmost = true,
                Top = screen.WorkingArea.Top / ratio,
                Left = screen.WorkingArea.Left / ratio
            };

            blackgroundWindow.Show();
            blackgroundWindow.WindowState = WindowState.Maximized;

            secondScreenSaverWindow = CreateScreenSaverLayerWindow(null, screen, ratio);
            firstScreenSaverWindow = CreateScreenSaverLayerWindow(gameContent, screen, ratio);

            var newContent = firstScreenSaverWindow.Content as ScreenSaverImage;
            var volume = _settings.Volume / 100.0;

            var fadeInBlack = CreateFade(blackgroundWindow, UIElement.OpacityProperty, duration, 0, 1);
            var fadeInWindow = CreateFade(firstScreenSaverWindow, UIElement.OpacityProperty, duration, 0, 1);
            var fadeInVideo = CreateFade(newContent.VideoPlayer, MediaElement.VolumeProperty, duration, 0, volume);
            var fadeInMusic = CreateFade(newContent.MusicPlayer, MediaElement.VolumeProperty, duration, 0, volume);

            var storyBoard = new Storyboard
            {
                Children = new TimelineCollection
                {
                    fadeInBlack,
                    fadeInWindow,
                    fadeInVideo,
                    fadeInMusic
                }
            };

            storyBoard.Begin();
            PlayMedia(firstScreenSaverWindow.Content, gameContent);

            // Startup can take some time, update clock just in case
            UpdateTime();
        }

        #endregion

        #region StopScreenSaver

        private void Stop()
        {
            firstScreenSaverWindow?.Close();
        }

        private void ScreenSaverClosed(object sender, EventArgs _)
        {
            lock (screenSaverLock)
            {
                showFirstWindow = false;

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
                    UnMuteBackgroundMusic();
                    secondScreenSaverWindow?.Close();
                    firstScreenSaverWindow = null;
                    _onStopCallBack();
                }
                else
                {
                    firstScreenSaverWindow?.Close();
                    secondScreenSaverWindow = null;
                }
            }
        }

        private void MuteBackgroundMusic()
        {
            if (_playniteApi.ApplicationInfo.Mode is ApplicationMode.Fullscreen)
            {
                _playniteApi.ApplicationSettings.Fullscreen.IsMusicMuted = true;
            }
        }

        private void UnMuteBackgroundMusic()
        {
            if (_playniteApi.ApplicationInfo.Mode is ApplicationMode.Fullscreen)
            {
                _playniteApi.ApplicationSettings.Fullscreen.IsMusicMuted = false;
            }
        }

        #endregion

        #region UpdateScreenSaver

        private void Update() { lock (screenSaverLock) { UpdateWindows(); } }
        private void UpdateWindows()
        {
            var gameContent = GetNextGameContent();

            if (firstScreenSaverWindow is null || gameContent is null) return;

            var newWindow = showFirstWindow ? firstScreenSaverWindow : secondScreenSaverWindow;
            var oldWindow = showFirstWindow ? secondScreenSaverWindow : firstScreenSaverWindow;
            showFirstWindow = !showFirstWindow;

            var newContent = newWindow.Content as ScreenSaverImage;
            var context = newContent.DataContext as ScreenSaverViewModel;

            context.BackgroundPath = gameContent.BackgroundPath;
            newContent.BackgroundImage.Source = gameContent.BackgroundPath is null
                ? null
                : new BitmapImage(new Uri(gameContent.BackgroundPath));

            if (_settings.DisplayLogo)
            {
                context.LogoPath = gameContent.LogoPath;
                newContent.LogoImage.Source = gameContent.LogoPath is null
                    ? null
                    : new BitmapImage(new Uri(gameContent.LogoPath));
            }

            if (_settings.DisplayVideo)
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

            if (_settings.AudioSource is AudioSource.Music)
            {
                context.MusicPath = gameContent.MusicPath;
                newContent.MusicPlayer.Source = gameContent.MusicPath is null ? null : new Uri(gameContent.MusicPath);
            }

            var volume = _settings.Volume / 100.0;
            var oldContent = oldWindow.Content as ScreenSaverImage;

            var fadeInWindow = CreateFade(newWindow, UIElement.OpacityProperty, duration, 0, 1);
            var fadeOutWindow = CreateFade(oldWindow, UIElement.OpacityProperty, duration, 1, 0);
            var fadeInVideo = CreateFade(newContent.VideoPlayer, MediaElement.VolumeProperty, duration, 0, volume);
            var fadeOutVideo = CreateFade(oldContent.VideoPlayer, MediaElement.VolumeProperty, duration, volume, 0);
            var fadeInMusic = CreateFade(newContent.MusicPlayer, MediaElement.VolumeProperty, duration, 0, volume);
            var fadeoutMusic = CreateFade(oldContent.MusicPlayer, MediaElement.VolumeProperty, duration, volume, 0);

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

            UpdateTime();
            storyBoard.Begin();

            PlayVideo(newContent.VideoPlayer, gameContent.MusicPath);
            PlayAudio(newContent.MusicPlayer, gameContent.VideoPath);
            UpdateTime();
        }

        #endregion

        #region PreviewScreenSaver

        private void Preview(Game game, Action onCloseCallBack)
        {
            var screen = Screen.AllScreens[_settings.ScreenIndex];
            var gameContent = _gameContentFactory.ConstructGameContent(game);
            firstScreenSaverWindow = CreateScreenSaverLayerWindow(gameContent, screen, CalculateScreenRatio());
            firstScreenSaverWindow.Closed += (_, __) => onCloseCallBack();
            firstScreenSaverWindow.Opacity = 1;
            PlayMedia(firstScreenSaverWindow.Content, gameContent);
        }

        #endregion

        #region UpdateScreenSaverTime

        private void UpdateTime()
        {
            if (!_settings.DisplayClock) return;

            var firstScreen = firstScreenSaverWindow?.Content as ScreenSaverImage;
            var secondScreen = secondScreenSaverWindow?.Content as ScreenSaverImage;

            if (firstScreen?.DataContext is ScreenSaverViewModel firstView && secondScreen?.DataContext is ScreenSaverViewModel secondView)
            {
                var time = DateTime.Now;
                var clockText = CreateClockText(time);
                var dateText = CreateDateText(time);
                firstView.ClockText = clockText;
                firstView.DateText = dateText;
                secondView.ClockText = clockText;
                secondView.DateText = dateText;
            }
        }

        #endregion

        #region UpdateSettings
        private void UpdateSetting(ScreenSaverSettings settings)
        {
            _settings = settings;
            _gameContentFactory.UpdateSettings(settings);
        }
        #endregion

        #region Helpers

        private Window CreateScreenSaverLayerWindow(GameContent gameContent, Screen screen, double ratio)
        {
            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Focusable = false,
                Topmost = true,
                AllowsTransparency = true,
                Opacity = 0,
                Background = Brushes.Transparent,
                Top = screen.WorkingArea.Top / ratio,
                Left = screen.WorkingArea.Left / ratio
            };

            var time = DateTime.Now;
            var content = new ScreenSaverImage
            {
                ParentWindow = window,
                DataContext = new ScreenSaverViewModel
                {
                    Settings = _settings,
                    BackgroundPath = gameContent?.BackgroundPath,
                    LogoPath = gameContent?.LogoPath,
                    VideoPath = gameContent?.VideoPath,
                    MusicPath = gameContent?.MusicPath,
                    ClockText = CreateClockText(time),
                    DateText = CreateDateText(time)
                }
            };

            var volume = _settings.Volume / 100.0;
            content.VideoPlayer.Volume = volume;
            content.MusicPlayer.Volume = volume;

            if (_settings.DisplayLogo)
            {
                Grid.SetRow(content.LogoGrid, _settings.DisplayVideo ? 0 : 1);
                content.LogoImage.Visibility = Visibility.Visible;
            }
            else
            {
                content.LogoImage.Visibility = Visibility.Hidden;
            }

            content.Video.Visibility = _settings.DisplayVideo ? Visibility.Visible : Visibility.Hidden;
            content.Clock.Visibility = _settings.DisplayClock ? Visibility.Visible : Visibility.Hidden;

            window.Content = content;
            window.Closed += ScreenSaverClosed;

            window.Show();
            window.WindowState = WindowState.Maximized;
            window.Focus();

            return window;
        }

        private static double CalculateScreenRatio()
        { 
            var widthRatio = Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.PrimaryScreenWidth;
            var heightRatio = Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.PrimaryScreenHeight;
            if (widthRatio > heightRatio)
            {
                logger.Info($"Using widthRatio '{widthRatio}' with workingArea width of '{Screen.PrimaryScreen.WorkingArea.Width}' and primary screen width of '{SystemParameters.PrimaryScreenWidth}'");
                return widthRatio;
            }

            logger.Info($"Using heightRatio '{heightRatio}' with workingArea Height of '{Screen.PrimaryScreen.WorkingArea.Height}' and primary screen Height of '{SystemParameters.PrimaryScreenHeight}'");
            return heightRatio;
        }

        string CreateClockText(DateTime time) => time.ToString("t");
        string CreateDateText(DateTime time) => time.ToString("dddd, MMMM dd");

        private GameContent GetNextGameContent()
        {
            if ((!GameEnumerator?.MoveNext() ?? true) && !InitializeEnumerator())
            {
                logger.Warn("No games found while rendering ScreenSaver");
                return null;
            }

            return GameEnumerator.Current;
        }

        private bool InitializeEnumerator()
        {

            IEnumerable<Game> games = _playniteApi.Database.Games;

            var hasSelectedGames = false;
            var currentGameGroup = _gameGroupManager.GetActiveGameGroup();
            if (currentGameGroup != null)
            {
                List<Game> groupGames = null;            
                
                if (currentGameGroup.Filter != null)
                {
                    groupGames = _settings.RetrieveDynamicGroupsInOrder
                        ? GetSortedDynamicList(currentGameGroup.Filter.Id)
                        : _playniteApi.Database.GetFilteredGames(currentGameGroup.Filter.Settings).ToList();
                }

                if (currentGameGroup.GameGuids.Any())
                {
                    var selectedGames = currentGameGroup.GameGuids.
                        Select(guid => _playniteApi.Database.Games.FirstOrDefault(g => g.Id == guid)).
                        Where(g => g != null);

                    hasSelectedGames = selectedGames.Any();
                    if (hasSelectedGames) /* Then */
                    if (groupGames is null)
                    {
                        groupGames = selectedGames.ToList();
                    }
                    else
                    {
                        groupGames.AddRange(selectedGames);
                    }
                }

                if (groupGames is null)
                {
                    return false;
                }

                games = groupGames;
            }

            if (currentGameGroup?.Filter is null)
            {
                games = games.Where(g => !g.Hidden);
            }

            var content = games.Select(_gameContentFactory.ConstructGameContent).
                                Where(ValidGameContent);

            var sortField = string.IsNullOrWhiteSpace(currentGameGroup?.SortField)  
                ? Resource.GROUP_SORT_RND 
                : currentGameGroup.SortField;

            if (sortField != "None")
            {
                content = content.Order(GetSelector(sortField), currentGameGroup?.Ascending ?? true);
            }
            else if (hasSelectedGames && currentGameGroup?.Filter?.SortingOrder != null)
            {
                // TODO: match sort of filter for selected games
                // If there is no sort override, a filter with a sorting order is present and the game group has selected games,
                // then the games must be re-ordered using the filter order
                logger.Warn("Selected and filtered games are in use, but the sort field is not specified. Selected games will be appended.");
            }

            GameEnumerator = content.GetEnumerator();

            return GameEnumerator.MoveNext();
        }

        private List<Game> GetSortedDynamicList(Guid filterId)
        {
            var activeFilter = _playniteApi.MainView.GetCurrentFilterSettings();
            var selectedGames = _playniteApi.MainView.SelectedGames.Select(g => g.Id).ToList();

            _playniteApi.MainView.ApplyFilterPreset(filterId);
            var filteredGames = _playniteApi.MainView.FilteredGames;

            _playniteApi.MainView.ApplyFilterPreset(new FilterPreset { Settings = activeFilter });
            _playniteApi.MainView.SelectGames(selectedGames);

            return filteredGames;
        }

        private Func<GameContent, object> GetSelector(string propertyName)
        {
            // C# 7.3 does not support conditional expressions here, only C# 9.0+ does
            if (propertyName == "Random")
            {
                return _ => _rng.Next();
            }

            return g => typeof(Game).GetProperty(propertyName).GetValue(g.Source);
        }

        private bool ValidGameContent(GameContent gameContent)
            => (!_settings.VideoSkip      || gameContent.VideoPath      != null) &&
               (!_settings.MusicSkip      || gameContent.MusicPath      != null) &&
               (!_settings.LogoSkip       || gameContent.LogoPath       != null) &&
               (!_settings.BackgroundSkip || gameContent.BackgroundPath != null);

        #region Media Control

        private void PlayMedia(object content, GameContent gameContent)
        {
            var screenSaver = content as ScreenSaverImage;
            PlayVideo(screenSaver.VideoPlayer, gameContent.MusicPath);
            PlayAudio(screenSaver.MusicPlayer, gameContent.VideoPath);
        }

        private void PlayVideo(MediaElement videoPlayer, string musicPath)
        {
            if (_settings.DisplayVideo)
            {
                videoPlayer.IsMuted = !ShouldPlayVideoAudio(musicPath);
                videoPlayer.Play();
            }
            else
            {
                videoPlayer.Stop();
            }
        }

        private void PlayAudio(MediaElement musicPlayer, string videoPath)
        {
            if   (ShouldPlayMusic(videoPath)) musicPlayer.Play();
            else                              musicPlayer.Stop();
        }

        private bool ShouldPlayMusic(string videoPath)      => ShouldPlayAudio(AudioSource.Music, videoPath);
        private bool ShouldPlayVideoAudio(string musicPath) => ShouldPlayAudio(AudioSource.Video, musicPath);
        private bool ShouldPlayAudio(AudioSource source, string otherAudioPath)
            => _settings.AudioSource == source || (_settings.PlayBackup && otherAudioPath is null);

        private DoubleAnimation CreateFade(DependencyObject window, object property, Duration duration, double from, double to)
        {
            var fade = new DoubleAnimation
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

        #endregion

        #endregion

        #endregion
    }
}
