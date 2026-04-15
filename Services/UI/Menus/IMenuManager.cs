using Playnite;
using static Playnite.Plugin;

namespace ScreenSaver.Services.UI.Menus;

internal interface IMenuManager
{
    ICollection<MenuItemImpl> GetGameMenuItems(GetGameMenuItemsArgs args);
    ICollection<MenuItemImpl> GetMainMenuItems(GetAppMenuItemsArgs args);
    ICollection<MenuItemDescriptor>? GetAppMenuItemDescriptors(GetAppMenuItemDescriptorsArgs args);
    ICollection<MenuItemDescriptor> GetGameMenuItemDescriptors(GetGameMenuItemDescriptorsArgs args);
}
