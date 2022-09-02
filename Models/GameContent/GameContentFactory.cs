using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenSaver.Models.GameContent
{
    internal class GameContentFactory : IGameContentFactory
    {
        private readonly IPlayniteAPI _playniteApi;
        private readonly string ExtraMetaDataPath;
        private readonly string SoundsPath;
        public GameContentFactory(IPlayniteAPI playniteApi)
        {
            _playniteApi = playniteApi;
            ExtraMetaDataPath = Path.Combine(_playniteApi.Paths.ConfigurationPath, "ExtraMetadata\\games");
            SoundsPath = Path.Combine(_playniteApi.Paths.ExtensionsDataPath, @"9c960604-b8bc-4407-a4e4-e291c6097c7d\Music Files\Game");
        }

        public GameContent ConstructGameContent(Game game)
        {
            var idString = game.Id.ToString();
            return new GameContent
            {
                Id             =                      game.Id,
                GameName       =                    game.Name,
                LogoPath       = GetLogoPath       (idString),
                MusicPath      = GetMusicPath      (idString),
                VideoPath      = GetVideoPath      (idString),
                BackgroundPath = GetBackgroundPath (    game)
            };
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
