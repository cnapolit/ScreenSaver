using Playnite;

namespace ScreenSaver.Common.Constants;

public class Resource
{
    public static IPlayniteApi? Api { get; set; }
    public static string SETTINGS_MONITOR_PRIMARY_INDICATOR => _SETTINGS_MONITOR_PRIMARY_INDICATOR.Value;
    private static readonly Lazy<string> _SETTINGS_MONITOR_PRIMARY_INDICATOR = new(() => ToId("settings_monitor_primary_indicator"));

    public static string MENU_GROUPS => _MENU_GROUPS.Value;
    private static readonly Lazy<string> _MENU_GROUPS = new(() => ToId("menu_groups"));

    public static string MENU_PREVIEW => _MENU_PREVIEW.Value;
    private static readonly Lazy<string> _MENU_PREVIEW = new(() => ToId("menu_preview"));

    public static string MENU_VIEW => _MENU_VIEW.Value;
    private static readonly Lazy<string> _MENU_VIEW = new(() => ToId("menu_view"));

    public static string MENU_DELETE => _MENU_DELETE.Value;
    private static readonly Lazy<string> _MENU_DELETE = new(() => ToId("menu_delete"));

    public static string MENU_PROMPT_SORT => _MENU_PROMPT_SORT.Value;
    private static readonly Lazy<string> _MENU_PROMPT_SORT = new(() => ToId("menu_prompt_sort"));

    public static string MENU_PROMPT_ASC => _MENU_PROMPT_ASC.Value;
    private static readonly Lazy<string> _MENU_PROMPT_ASC = new(() => ToId("menu_prompt_asc"));

    public static string MAIN_MENU_SORT => _MAIN_MENU_SORT.Value;
    private static readonly Lazy<string> _MAIN_MENU_SORT = new(() => ToId("main_menu_sort"));

    public static string MAIN_MENU_ASC => _MAIN_MENU_ASC.Value;
    private static readonly Lazy<string> _MAIN_MENU_ASC = new(() => ToId("main_menu_asc"));

    public static string MAIN_MENU_DESC => _MAIN_MENU_DESC.Value;
    private static readonly Lazy<string> _MAIN_MENU_DESC = new(() => ToId("main_menu_desc"));

    public static string MAIN_MENU_TOGGLE => _MAIN_MENU_TOGGLE.Value;
    private static readonly Lazy<string> _MAIN_MENU_TOGGLE = new(() => ToId("main_menu_toggle"));

    public static string MAIN_MENU_RENAME => _MAIN_MENU_RENAME.Value;
    private static readonly Lazy<string> _MAIN_MENU_RENAME = new(() => ToId("main_menu_rename"));

    public static string MAIN_MENU_START => _MAIN_MENU_START.Value;
    private static readonly Lazy<string> _MAIN_MENU_START = new(() => ToId("main_menu_start"));

    public static string MAIN_MENU_DYNAMIC => _MAIN_MENU_DYNAMIC.Value;
    private static readonly Lazy<string> _MAIN_MENU_DYNAMIC = new(() => ToId("main_menu_dynamic"));

    public static string MAIN_MENU_LOAD_FILTER => _MAIN_MENU_LOAD_FILTER.Value;
    private static readonly Lazy<string> _MAIN_MENU_LOAD_FILTER = new(() => ToId("main_menu_load_filter"));

    public static string MAIN_MENU_SET_FILTER => _MAIN_MENU_SET_FILTER.Value;
    private static readonly Lazy<string> _MAIN_MENU_SET_FILTER = new(() => ToId("main_menu_set_filter"));

    public static string MAIN_MENU_VIEW_FILTER => _MAIN_MENU_VIEW_FILTER.Value;
    private static readonly Lazy<string> _MAIN_MENU_VIEW_FILTER = new(() => ToId("main_menu_view_filter"));

    public static string MAIN_MENU_GROUP_NAME => _MAIN_MENU_GROUP_NAME.Value;
    private static readonly Lazy<string> _MAIN_MENU_GROUP_NAME = new(() => ToId("main_menu_group_name"));

    public static string MAIN_MENU_GROUP_NAME_SHAME => _MAIN_MENU_GROUP_NAME_SHAME.Value;
    private static readonly Lazy<string> _MAIN_MENU_GROUP_NAME_SHAME = new(() => ToId("main_menu_group_name_shame"));

    public static string MAIN_MENU_GROUP_NAME_CONFLICT => _MAIN_MENU_GROUP_NAME_CONFLICT.Value;
    private static readonly Lazy<string> _MAIN_MENU_GROUP_NAME_CONFLICT = new(() => ToId("main_menu_group_name_conflict"));

    public static string MAIN_MENU_PROMPT_DELETE => _MAIN_MENU_PROMPT_DELETE.Value;
    private static readonly Lazy<string> _MAIN_MENU_PROMPT_DELETE = new(() => ToId("main_menu_prompt_delete"));

    public static string MAIN_MENU_GROUP_REMOVE_EMPTY => _MAIN_MENU_GROUP_REMOVE_EMPTY.Value;
    private static readonly Lazy<string> _MAIN_MENU_GROUP_REMOVE_EMPTY = new(() => ToId("main_menu_group_remove_empty"));

    public static string MAIN_MENU_SELECTED_GAMES => _MAIN_MENU_SELECTED_GAMES.Value;
    private static readonly Lazy<string> _MAIN_MENU_SELECTED_GAMES = new(() => ToId("main_menu_selected_games"));

    public static string GAME_MENU_PREVIEW => _GAME_MENU_PREVIEW.Value;
    private static readonly Lazy<string> _GAME_MENU_PREVIEW = new(() => ToId("game_menu_preview"));

    public static string GAME_MENU_PREVIEW_ERROR => _GAME_MENU_PREVIEW_ERROR.Value;
    private static readonly Lazy<string> _GAME_MENU_PREVIEW_ERROR = new(() => ToId("game_menu_preview_error"));

    public static string GAME_MENU_PROMPT_ACTIVE => _GAME_MENU_PROMPT_ACTIVE.Value;
    private static readonly Lazy<string> _GAME_MENU_PROMPT_ACTIVE = new(() => ToId("game_menu_prompt_active"));

    public static string GAME_MENU_ADD_SELECTED => _GAME_MENU_ADD_SELECTED.Value;
    private static readonly Lazy<string> _GAME_MENU_ADD_SELECTED = new(() => ToId("game_menu_add_selected"));

    public static string GAME_MENU_REMOVE_SELECTED => _GAME_MENU_REMOVE_SELECTED.Value;
    private static readonly Lazy<string> _GAME_MENU_REMOVE_SELECTED = new(() => ToId("game_menu_remove_selected"));

    public static string GAME_MENU_CREATE_SELECTED => _GAME_MENU_CREATE_SELECTED.Value;
    private static readonly Lazy<string> _GAME_MENU_CREATE_SELECTED = new(() => ToId("game_menu_create_selected"));

    public static string GAME_MENU_GAMES => _GAME_MENU_GAMES.Value;
    private static readonly Lazy<string> _GAME_MENU_GAMES = new(() => ToId("game_menu_games"));

    public static string GROUP_SAVE_FAIL => _GROUP_SAVE_FAIL.Value;
    private static readonly Lazy<string> _GROUP_SAVE_FAIL = new(() => ToId("group_save_fail"));

    public static string GROUP_SORT_RND => _GROUP_SORT_RND.Value;
    private static readonly Lazy<string> _GROUP_SORT_RND = new(() => ToId("group_sort_rnd"));

    private static string ToId(string id) => Api!.GetLocalizedString(id);
}
