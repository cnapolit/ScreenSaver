using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace ScreenSaver.Services.UI.Menus
{
    internal interface IMenuManager
    {
        IEnumerable<GameMenuItem> GetGameMenuItems();
        IEnumerable<MainMenuItem> GetMainMenuItems();
    }
}
