using ScreenSaver;
using Playnite.Markup;

namespace Playnite;

public class LocalizedString : LocStringMarkup
{
    public LocalizedString() : base(ScreenSaverPlugin.Id)
    {
    }

    public LocalizedString(string stringId) : base(ScreenSaverPlugin.Id, stringId)
    {
    }
}

public static partial class Loc
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static IPlayniteApi? Api = null;
#pragma warning restore CA2211 // Non-constant fields should not be visible

    public static string GetString(string stringId) => Api?.GetLocalizedString(stringId) ?? "";

    public static string GetString(string stringId, params (string name, object value)[] args)
        => Api?.GetLocalizedString(stringId, args) ?? "";

    public static bool IsStringId(string id) => LocId.StringIds.Contains(id);
}