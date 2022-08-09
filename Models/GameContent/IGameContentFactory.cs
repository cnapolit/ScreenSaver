using Playnite.SDK.Models;

namespace ScreenSaver.Models.GameContent
{
    internal interface IGameContentFactory
    {
        GameContent ConstructGameContent(Game game);
    }
}
