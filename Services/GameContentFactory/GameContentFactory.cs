using Playnite;
using ScreenSaver.Common.Constants;
using ScreenSaver.Models;
using ScreenSaver.Services.State.Settings;
using System.IO;

namespace ScreenSaver.Services;

internal class GameContentFactory(IPlayniteApi playniteApi) : SettingsConsumer, IGameContentFactory
{
    #region Infrastructure

    private readonly string ExtraMetaDataPath   =  Path.Combine(playniteApi.AppInfo.ConfigurationDirectory, Files.MetaDataPath);
    private          string VideoFileName       => Settings.UseMicroTrailer ? Files.Micro : Files.Video;
    private          string BackupVideoFileName => Settings.UseMicroTrailer ? Files.Video : Files.Micro;

    #endregion

    #region Interface

    public GameContent ConstructGameContent(Game game) => Construct(game);

    #endregion

    #region Implementation

    #region ConstructGameContent

    private GameContent Construct(Game game)
    {
        var idString = game.Id.ToString();
        return new GameContent
        {
            Source         =                         game,
            LogoPath       = GetLogoPath       (idString),
            MusicPath      = GetMusicPath      (idString),
            VideoPath      = GetVideoPath      (idString),
            BackgroundPath = GetBackgroundPath (    game)
        };
    }

    private string? GetVideoPath(string gameId)
    {
        var path = GetExtraPath(gameId, VideoFileName);
        if (Settings.VideoBackup && path is null)
        {
            path = GetExtraPath(gameId, BackupVideoFileName);
        }
        return path;
    }

    private string? GetLogoPath(string gameId) => GetExtraPath(gameId, Files.Logo);
    private string? GetExtraPath(string gameId, string fileName)
    {
        var extraPath = Path.Combine(ExtraMetaDataPath, gameId, fileName);
        return File.Exists(extraPath)
            ? extraPath
            : null;
    }

    private string? GetMusicPath(string gameId)
    {
        var soundDirectory = Path.Combine(ExtraMetaDataPath, gameId, Files.SoundsDirectory);
        return Directory.Exists(soundDirectory)
            ? Directory.GetFiles(soundDirectory).FirstOrDefault()
            : null;
    }

    private string? GetBackgroundPath(Game game)
    {
        var background = game.MediaFiles?.FirstOrDefault(f => f.Type is "Playnite.DesktopBackground");
        if (background?.Path != null && !background.Path.StartsWith("http"))
        {
            return playniteApi.Library.GetFullFilePath(background.Path);
        }

        if (game.PlatformIds.HasItems())
        {
            var gamePlatforms = playniteApi.Library.Platforms.Get(game.PlatformIds);
            var platformBackground = gamePlatforms.FirstOrDefault(g => g.Background != null)?.Background;
            if (platformBackground != null)
            {
                return playniteApi.Library.GetFullFilePath(platformBackground);
            }
        }

        return null;
    }

    #endregion

    #endregion
}
