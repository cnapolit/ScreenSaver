using Playnite.SDK.Models;
using ScreenSaver.Models;

namespace ScreenSaver.Services
{
    internal interface IGameContentFactory : IScreenSaverSettings
    {
        GameContent ConstructGameContent(Game game);
    }
}
