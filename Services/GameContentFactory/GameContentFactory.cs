using Playnite.SDK;
using Playnite.SDK.Models;
using ScreenSaver.Common.Constants;
using ScreenSaver.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenSaver.Services
{
    internal class GameContentFactory : IGameContentFactory
    {
        #region Infrastructure

        private readonly IPlayniteAPI                _playniteApi;
        private          ScreenSaverSettings            _settings;
        private readonly string                 ExtraMetaDataPath;
        private readonly string                        SoundsPath;
        private          string                    _videoFileName;
        private          string              _backupVideoFileName;

        public GameContentFactory(IPlayniteAPI playniteApi, ScreenSaverSettings settings)
        {
            _playniteApi = playniteApi;
            ExtraMetaDataPath = Path.Combine( _playniteApi.Paths.ConfigurationPath, Files. MetaDataPath);
            SoundsPath        = Path.Combine(_playniteApi.Paths.ExtensionsDataPath, Files.   SoundsPath);
            Update(settings);
        }

        #endregion

        #region Interface

        public GameContent ConstructGameContent(Game game) => Construct(game);
        public void UpdateSettings(ScreenSaverSettings settings) => Update(settings);

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

        private string GetVideoPath(string gameId)
        {
            var path = GetExtraPath(gameId, _videoFileName);
            if (_settings.VideoBackup && path is null)
            {
                path = GetExtraPath(gameId, _backupVideoFileName);
            }
            return path;
        }

        private string GetLogoPath(string gameId) => GetExtraPath(gameId, Files.Logo);
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

        #endregion

        #region UpdateSettings

        private void Update(ScreenSaverSettings settings)
        {
            _settings = settings;
            _videoFileName = settings.UseMicroTrailer ? Files.Video : Files.Micro;
            _backupVideoFileName = settings.UseMicroTrailer ? Files.Micro : Files.Video;
        }

        #endregion

        #endregion
    }
}
