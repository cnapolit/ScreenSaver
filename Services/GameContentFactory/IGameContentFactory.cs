using Playnite;
using ScreenSaver.Models;

namespace ScreenSaver.Services;

internal interface IGameContentFactory
{
    GameContent ConstructGameContent(Game game);
}
