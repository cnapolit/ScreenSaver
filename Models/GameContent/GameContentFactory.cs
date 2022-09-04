using Playnite.SDK;
using Playnite.SDK.Models;
using ScreenSaver.Common.Constants;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenSaver.Models.GameContent
{
    internal class GameContentFactory : IGameContentFactory
    {
        private readonly IPlayniteAPI      _playniteApi;
        private readonly string       ExtraMetaDataPath;
        private readonly string              SoundsPath;
        public GameContentFactory(IPlayniteAPI playniteApi)
        {
            _playniteApi = playniteApi;
            ExtraMetaDataPath = Path.Combine( _playniteApi.Paths.ConfigurationPath, Files. MetaDataPath);
            SoundsPath        = Path.Combine(_playniteApi.Paths.ExtensionsDataPath, Files.   SoundsPath);
        }

        public GameContent ConstructGameContent(Game game)
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

        private string GetLogoPath(string gameId) => GetExtraPath(gameId, Files.Logo);
        private string GetVideoPath(string gameId) => GetExtraPath(gameId, Files.Video);
        private string GetExtraPath(string gameId, string fileName)
        {
            var videoPath = Path.Combine(ExtraMetaDataPath, gameId, fileName);
            return File.Exists(videoPath)
                ? videoPath
                : null;
        }

        private string GetMusicPath(string gameId)
        {
            var soundDirectory = Path.Combine(SoundsPath, gameId);
            return Directory.Exists(soundDirectory)
                ? Directory.GetFiles(soundDirectory).FirstOrDefault()
                : null;
        }

        private string GetBackgroundPath(Game game)
        {
            if (game.BackgroundImage != null && !game.BackgroundImage.StartsWith("http"))
            {
                return _playniteApi.Database.GetFullFilePath(game.BackgroundImage);
            }

            if (game.Platforms.HasItems() && game.Platforms[0].Background != null)
            {
                return _playniteApi.Database.GetFullFilePath(game.Platforms[0].Background);
            }

            return null;
        }
    }
}
