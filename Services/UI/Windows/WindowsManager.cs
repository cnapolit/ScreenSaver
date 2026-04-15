using Playnite;
using ScreenSaver.Common.Constants;
using ScreenSaver.Common.Extensions;
using ScreenSaver.Models;
using ScreenSaver.Models.Enums;
using ScreenSaver.Services.State.Settings;
using ScreenSaver.Views.Layouts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfScreenHelper;

namespace ScreenSaver.Services.UI.Windows;

internal class WindowsManager(
    IPlayniteApi playniteApi, IGameGroupManager gameGroupManager, IGameContentFactory gameContentFactory)
    : SettingsConsumer, IWindowsManager
{
    #region Infrastructure

    #region Variables

    private static readonly ILogger logger = LogManager.GetLogger();
    private static readonly Random _rng = new();
    private static readonly Lock screenSaverLock = new();
    private static readonly Duration duration = new(TimeSpan.FromSeconds(1));
    private bool showFirstWindow;

    private Window? firstScreenSaverWindow;
    private Window? secondScreenSaverWindow;
    private Window? blackgroundWindow;

    private IEnumerator<GameContent>? GameEnumerator;

    #endregion

    #endregion

    #region Interface

    public Task StartScreenSaverAsync() => StartAsync();
    public void StopScreenSaver() => Stop();
    public Task UpdateScreenSaverAsync() => UpdateAsync();
    public void UpdateScreenSaverTime() => UpdateTime();
    public void PreviewScreenSaver(Game game, Action closeCallBack) => Preview(game, closeCallBack);

    public delegate void OnStopCallback();

    public event OnStopCallback? OnStop;

    #endregion

    #region Implementation

    #region StartScreenSaver

    private async Task StartAsync()
    {
        if (!await InitializeEnumeratorAsync())
        {
            logger.Warn("No games found while starting ScreenSaver");
            return;
        }

        var gameContent = GameEnumerator!.Current;

        firstScreenSaverWindow?.Close();
        secondScreenSaverWindow?.Close();
        MuteBackgroundMusic();

        var screens = Screen.AllScreens.ToList();
        if (screens.Count is 0) return;
        var screen = screens.FirstOrDefault() ?? screens.First();
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
            Top = screen.WorkingArea.Top,
            Left = screen.WorkingArea.Left
        };

        blackgroundWindow.Show();
        blackgroundWindow.WindowState = WindowState.Maximized;

        secondScreenSaverWindow = CreateScreenSaverLayerWindow(null, screen);
        firstScreenSaverWindow = CreateScreenSaverLayerWindow(gameContent, screen);

        var newContent = (ScreenSaverImage)firstScreenSaverWindow.Content;
        var volume = Settings.Volume / 100.0;

        var fadeInBlack = CreateFade(blackgroundWindow, UIElement.OpacityProperty, duration, 0, 1);
        var fadeInWindow = CreateFade(firstScreenSaverWindow, UIElement.OpacityProperty, duration, 0, 1);
        var fadeInVideo = CreateFade(newContent.VideoPlayer, MediaElement.VolumeProperty, duration, 0, volume);
        var fadeInMusic = CreateFade(newContent.MusicPlayer, MediaElement.VolumeProperty, duration, 0, volume);

        Storyboard storyBoard = new()
        {
            Children =
            [
                fadeInBlack,
                fadeInWindow,
                fadeInVideo,
                fadeInMusic
            ]
        };

        storyBoard.Begin();
        PlayMedia(firstScreenSaverWindow.Content, gameContent);

        // Startup can take some time, update clock just in case
        UpdateTime();
    }

    #endregion

    #region StopScreenSaver

    private void Stop() => firstScreenSaverWindow?.Close();

    private void ScreenSaverClosed(object? sender, EventArgs _)
    {
        lock (screenSaverLock)
        {
            if (sender is not Window window) return;

            showFirstWindow = false;

            blackgroundWindow?.Close();

            window.Closed -= ScreenSaverClosed;

            var content = (ScreenSaverImage)window.Content;

            content.VideoPlayer.Stop();
            content.VideoPlayer.Close();
            content.MusicPlayer.Stop();
            content.MusicPlayer.Close();

            if (sender == firstScreenSaverWindow)
            {
                UnMuteBackgroundMusic();
                secondScreenSaverWindow?.Close();
                firstScreenSaverWindow = null;
                OnStop?.Invoke();
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
        if (playniteApi.AppInfo.Mode is AppMode.Fullscreen)
        {
            //_playniteApi.AppInfo.Fullscreen.IsMusicMuted = true;
        }
    }

    private void UnMuteBackgroundMusic()
    {
        if (playniteApi.AppInfo.Mode is AppMode.Fullscreen)
        {
            //_playniteApi.AppInfo.Fullscreen.IsMusicMuted = false;
        }
    }

    #endregion

    #region UpdateScreenSaver

    private async Task UpdateAsync()
    {
        var gameContent = await GetNextGameContentAsync();
        lock (screenSaverLock)
        {
            UpdateWindows(gameContent);
        }
    }
    private void UpdateWindows(GameContent? gameContent)
    {
        if (firstScreenSaverWindow is null || secondScreenSaverWindow is null || gameContent is null) return;

        var newWindow = showFirstWindow ? firstScreenSaverWindow : secondScreenSaverWindow;
        var oldWindow = showFirstWindow ? secondScreenSaverWindow : firstScreenSaverWindow;
        showFirstWindow = !showFirstWindow;

        var newContent = (ScreenSaverImage)newWindow.Content;
        var context = (ScreenSaverViewModel)newContent.DataContext;

        context.BackgroundPath = gameContent.BackgroundPath;
        newContent.BackgroundImage.Source = gameContent.BackgroundPath is null
            ? null
            : new BitmapImage(new Uri(gameContent.BackgroundPath));

        if (Settings.DisplayLogo)
        {
            context.LogoPath = gameContent.LogoPath;
            newContent.LogoImage.Source = gameContent.LogoPath is null
                ? null
                : new BitmapImage(new Uri(gameContent.LogoPath));
        }

        if (Settings.DisplayVideo)
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
            newContent.MusicPlayer.Source = gameContent.MusicPath is null ? null : new Uri(gameContent.MusicPath);
        }

        var volume = Settings.Volume / 100.0;
        var oldContent = (ScreenSaverImage)oldWindow.Content;

        var fadeInWindow = CreateFade(newWindow, UIElement.OpacityProperty, duration, 0, 1);
        var fadeOutWindow = CreateFade(oldWindow, UIElement.OpacityProperty, duration, 1, 0);
        var fadeInVideo = CreateFade(newContent.VideoPlayer, MediaElement.VolumeProperty, duration, 0, volume);
        var fadeOutVideo = CreateFade(oldContent.VideoPlayer, MediaElement.VolumeProperty, duration, volume, 0);
        var fadeInMusic = CreateFade(newContent.MusicPlayer, MediaElement.VolumeProperty, duration, 0, volume);
        var fadeoutMusic = CreateFade(oldContent.MusicPlayer, MediaElement.VolumeProperty, duration, volume, 0);

        var storyBoard = new Storyboard
        {
            Children =
            [
                fadeInWindow,
                fadeOutWindow,
                fadeInVideo,
                fadeOutVideo,
                fadeInMusic,
                fadeoutMusic
            ]
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
        var screens = Screen.AllScreens.ToList();
        if (screens.Count is 0) return;
        var screen = screens.FirstOrDefault() ?? screens.First();
        var gameContent = gameContentFactory.ConstructGameContent(game);
        firstScreenSaverWindow = CreateScreenSaverLayerWindow(gameContent, screen);
        firstScreenSaverWindow.Closed += (_, __) => onCloseCallBack();
        firstScreenSaverWindow.Opacity = 1;
        PlayMedia(firstScreenSaverWindow.Content, gameContent);
    }

    #endregion

    #region UpdateScreenSaverTime

    private void UpdateTime()
    {
        if (!Settings.DisplayClock) return;

        var screenOne = firstScreenSaverWindow?.Content as ScreenSaverImage;
        var screenTwo = secondScreenSaverWindow?.Content as ScreenSaverImage;

        if (screenOne?.DataContext is ScreenSaverViewModel viewOne
         && screenTwo?.DataContext is ScreenSaverViewModel viewTwo)
        {
            var time = DateTime.Now;
            var clockText = CreateClockText(time);
            var dateText = CreateDateText(time);
            viewOne.ClockText = clockText;
            viewOne.DateText = dateText;
            viewTwo.ClockText = clockText;
            viewTwo.DateText = dateText;
        }
    }

    #endregion

    #region Helpers

    private Window CreateScreenSaverLayerWindow(GameContent? gameContent, Screen screen)
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
            Top = screen.WorkingArea.Top,
            Left = screen.WorkingArea.Left
        };

        var time = DateTime.Now;
        var content = new ScreenSaverImage
        {
            ParentWindow = window,
            DataContext = new ScreenSaverViewModel
            {
                Settings = Settings,
                BackgroundPath = gameContent?.BackgroundPath,
                LogoPath = gameContent?.LogoPath,
                VideoPath = gameContent?.VideoPath,
                MusicPath = gameContent?.MusicPath,
                ClockText = CreateClockText(time),
                DateText = CreateDateText(time)
            }
        };

        var volume = Settings.Volume / 100.0;
        content.VideoPlayer.Volume = volume;
        content.MusicPlayer.Volume = volume;

        if (Settings.DisplayLogo)
        {
            Grid.SetRow(content.LogoGrid, Settings.DisplayVideo ? 0 : 1);
            content.LogoImage.Visibility = Visibility.Visible;
        }
        else
        {
            content.LogoImage.Visibility = Visibility.Hidden;
        }

        content.Video.Visibility = Settings.DisplayVideo ? Visibility.Visible : Visibility.Hidden;
        content.Clock.Visibility = Settings.DisplayClock ? Visibility.Visible : Visibility.Hidden;

        window.Content = content;
        window.Closed += ScreenSaverClosed;

        window.Show();
        window.WindowState = WindowState.Maximized;
        window.Focus();

        return window;
    }

    private static string CreateClockText(DateTime time) => time.ToString("t");
    private static string CreateDateText(DateTime time) => time.ToString("dddd, MMMM dd");

    private async Task<GameContent?> GetNextGameContentAsync()
    {
        if ((!GameEnumerator?.MoveNext() ?? true) && ! await InitializeEnumeratorAsync())
        {
            logger.Warn("No games found while rendering ScreenSaver");
            return null;
        }

        return GameEnumerator!.Current;
    }

    private async Task<bool> InitializeEnumeratorAsync()
    {
        IEnumerable<Game> games = playniteApi.Library.Games;

        var hasSelectedGames = false;
        var currentGameGroup = gameGroupManager.GetActiveGameGroup();
        if (currentGameGroup != null)
        {
            List<Game>? groupGames = null;            
            
            if (currentGameGroup.Filter != null)
            {
                // TODO: (P11) GetFilteredGames(FILTER) not yet supported
                //groupGames = Settings.RetrieveDynamicGroupsInOrder
                //    ? GetSortedDynamicListAsync(currentGameGroup.Filter)
                //    : _playniteApi.Database.GetFilteredGames(currentGameGroup.Filter).ToList();
                groupGames = await GetSortedDynamicListAsync(currentGameGroup.Filter);
            }

            if (currentGameGroup.GameGuids.Any())
            {
                var selectedGames = currentGameGroup.GameGuids
                    .Select(playniteApi.Library.Games.Get)
                    .WhereNotNull();

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

        var content = games.Select(gameContentFactory.ConstructGameContent).
                            Where(ValidGameContent);

        var sortField = string.IsNullOrWhiteSpace(currentGameGroup?.SortField)  
            ? Resource.GROUP_SORT_RND 
            : currentGameGroup.SortField;

        if (sortField != "None")
        {
            content = content.Order(GetSelector(sortField), currentGameGroup?.Ascending ?? true);
        }
        //else if (hasSelectedGames && currentGameGroup?.Filter != null)
        //{
            // TODO: (P11) currentGameGroup?.Filter?.SortingOrder not available
            // TODO: match sort of filter for selected games
            // If there is no sort override, a filter with a sorting order is present and the game group has selected games,
            // then the games must be re-ordered using the filter order
        //    logger.Warn("Selected and filtered games are in use, but the sort field is not specified. Selected games will be appended.");
        //}

        GameEnumerator = content.GetEnumerator();

        return GameEnumerator.MoveNext();
    }

    private async Task<List<Game>> GetSortedDynamicListAsync(FilteringConfiguration filter)
    {
        var activeFilter = playniteApi.MainView.GetCurrentFilters() ?? new();
        var selectedGames = playniteApi.MainView.GetSelectedGames().Select(g => g.Id).ToList();

        await playniteApi.MainView.ApplyFiltersAsync(filter);
        var filteredGames = playniteApi.MainView.GetFilteredGames().ToList();

        await playniteApi.MainView.ApplyFiltersAsync(activeFilter);
        playniteApi.MainView.SelectGames(selectedGames);

        return filteredGames;
    }

    private static Func<GameContent, object?> GetSelector(string propertyName)
        => propertyName is "Random"
         ? (_ => _rng.Next()) 
         : (g => typeof(Game).GetProperty(propertyName)?.GetValue(g.Source));

    private bool ValidGameContent(GameContent gameContent)
        => (!Settings.VideoSkip      || gameContent.VideoPath      != null)
        && (!Settings.MusicSkip      || gameContent.MusicPath      != null)
        && (!Settings.LogoSkip       || gameContent.LogoPath       != null)
        && (!Settings.BackgroundSkip || gameContent.BackgroundPath != null);

    #region Media Control

    private void PlayMedia(object content, GameContent gameContent)
    {
        var screenSaver = (ScreenSaverImage)content;
        PlayVideo(screenSaver.VideoPlayer, gameContent.MusicPath);
        PlayAudio(screenSaver.MusicPlayer, gameContent.VideoPath);
    }

    private void PlayVideo(MediaElement videoPlayer, string? musicPath)
    {
        if (Settings.DisplayVideo)
        {
            videoPlayer.IsMuted = !ShouldPlayVideoAudio(musicPath);
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Stop();
        }
    }

    private void PlayAudio(MediaElement musicPlayer, string? videoPath)
    {
        if   (ShouldPlayMusic(videoPath)) musicPlayer.Play();
        else                              musicPlayer.Stop();
    }

    private bool ShouldPlayMusic(string? videoPath)      => ShouldPlayAudio(AudioSource.Music, videoPath);
    private bool ShouldPlayVideoAudio(string? musicPath) => ShouldPlayAudio(AudioSource.Video, musicPath);
    private bool ShouldPlayAudio(AudioSource source, string? otherAudioPath)
        => Settings.AudioSource == source || (Settings.PlayBackup && otherAudioPath is null);

    private static DoubleAnimation CreateFade(DependencyObject window, object property, Duration duration, double from, double to)
    {
        DoubleAnimation fade = new()
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
