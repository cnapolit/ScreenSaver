namespace Playnite;

public static partial class Loc
{

    /// <summary>
    /// Settings
    /// </summary>
    public static string settings_header() => GetString("settings_header");
    /// <summary>
    /// General
    /// </summary>
    public static string settings_header_general() => GetString("settings_header_general");
    /// <summary>
    /// Video
    /// </summary>
    public static string settings_header_video() => GetString("settings_header_video");
    /// <summary>
    /// Audio
    /// </summary>
    public static string settings_header_audio() => GetString("settings_header_audio");
    /// <summary>
    /// Clock
    /// </summary>
    public static string settings_header_clock() => GetString("settings_header_clock");
    /// <summary>
    /// Audio Volume
    /// </summary>
    public static string settings_audio_volume() => GetString("settings_audio_volume");
    /// <summary>
    /// Monitor
    /// </summary>
    public static string settings_monitor() => GetString("settings_monitor");
    /// <summary>
    /// Source For Audio
    /// </summary>
    public static string settings_audio_source() => GetString("settings_audio_source");
    /// <summary>
    /// None
    /// </summary>
    public static string settings_enum_audio_source_none() => GetString("settings_enum_audio_source_none");
    /// <summary>
    /// Video
    /// </summary>
    public static string settings_enum_audio_source_video() => GetString("settings_enum_audio_source_video");
    /// <summary>
    /// Music
    /// </summary>
    public static string settings_enum_audio_source_music() => GetString("settings_enum_audio_source_music");
    /// <summary>
    /// When To Play
    /// </summary>
    public static string settings_play_state() => GetString("settings_play_state");
    /// <summary>
    /// Never
    /// </summary>
    public static string settings_enum_play_state_never() => GetString("settings_enum_play_state_never");
    /// <summary>
    /// Desktop
    /// </summary>
    public static string settings_enum_play_state_desktop() => GetString("settings_enum_play_state_desktop");
    /// <summary>
    /// FullScreen
    /// </summary>
    public static string settings_enum_play_state_fullscreen() => GetString("settings_enum_play_state_fullscreen");
    /// <summary>
    /// Always
    /// </summary>
    public static string settings_enum_play_state_always() => GetString("settings_enum_play_state_always");
    /// <summary>
    /// Time Between Game Transitions in Seconds
    /// </summary>
    public static string settings_game_trans() => GetString("settings_game_trans");
    /// <summary>
    /// Time for ScreenSaver to Appear in Seconds
    /// </summary>
    public static string settings_svr_interval() => GetString("settings_svr_interval");
    /// <summary>
    /// Video Corner Radius
    /// </summary>
    public static string settings_video_corner() => GetString("settings_video_corner");
    /// <summary>
    /// Clock Corner Radius
    /// </summary>
    public static string settings_clock_corner() => GetString("settings_clock_corner");
    /// <summary>
    /// Clock Font
    /// </summary>
    public static string settings_clock_font() => GetString("settings_clock_font");
    /// <summary>
    /// Clock Font Size
    /// </summary>
    public static string settings_clock_font_size() => GetString("settings_clock_font_size");
    /// <summary>
    /// Clock Sub-Font Size
    /// </summary>
    public static string settings_clock_sub_font_size() => GetString("settings_clock_sub_font_size");
    /// <summary>
    /// Skip Games Missing a Background
    /// </summary>
    public static string settings_bckgrd_skip() => GetString("settings_bckgrd_skip");
    /// <summary>
    /// Skip Games Missing Video
    /// </summary>
    public static string settings_video_skip() => GetString("settings_video_skip");
    /// <summary>
    /// Skip Games Missing Music
    /// </summary>
    public static string settings_music_skip() => GetString("settings_music_skip");
    /// <summary>
    /// Skip Games Missing Logos
    /// </summary>
    public static string settings_logo_skip() => GetString("settings_logo_skip");
    /// <summary>
    /// Play backup audio if selected is missing
    /// </summary>
    public static string settings_play_backup() => GetString("settings_play_backup");
    /// <summary>
    /// Use Micro Trailer
    /// </summary>
    public static string settings_use_micro() => GetString("settings_use_micro");
    /// <summary>
    /// Play backup video if selected is missing
    /// </summary>
    public static string settings_video_backup() => GetString("settings_video_backup");
    /// <summary>
    /// Disable ScreenSaver while Playing
    /// </summary>
    public static string settings_disable_while_play() => GetString("settings_disable_while_play");
    /// <summary>
    /// Disable ScreenSaver When Not In Use
    /// </summary>
    public static string settings_pause_on_deac() => GetString("settings_pause_on_deac");
    /// <summary>
    /// Display Clock
    /// </summary>
    public static string settings_display_clock() => GetString("settings_display_clock");
    /// <summary>
    /// Display Video
    /// </summary>
    public static string settings_display_video() => GetString("settings_display_video");
    /// <summary>
    /// Display Logo
    /// </summary>
    public static string settings_display_logo() => GetString("settings_display_logo");
    /// <summary>
    /// Retrieve Dynmaic Groups in Order
    /// </summary>
    public static string settings_dynamic_sort() => GetString("settings_dynamic_sort");
    /// <summary>
    /// Enabling has the downside of briefly altering the main view UI
    /// </summary>
    public static string settings_dynamic_sort_tip() => GetString("settings_dynamic_sort_tip");
    /// <summary>
    /// (Primary)
    /// </summary>
    public static string settings_monitor_primary_indicator() => GetString("settings_monitor_primary_indicator");
    /// <summary>
    /// Groups
    /// </summary>
    public static string menu_groups() => GetString("menu_groups");
    /// <summary>
    /// Preview
    /// </summary>
    public static string menu_preview() => GetString("menu_preview");
    /// <summary>
    /// View
    /// </summary>
    public static string menu_view() => GetString("menu_view");
    /// <summary>
    /// Delete
    /// </summary>
    public static string menu_delete() => GetString("menu_delete");
    /// <summary>
    /// Please select a field to sort by:
    /// </summary>
    public static string menu_prompt_sort() => GetString("menu_prompt_sort");
    /// <summary>
    /// Should the group be sorted in ascending order?
    /// </summary>
    public static string menu_prompt_asc() => GetString("menu_prompt_asc");
    /// <summary>
    /// Change Sorting Field
    /// </summary>
    public static string main_menu_sort() => GetString("main_menu_sort");
    /// <summary>
    /// ASC
    /// </summary>
    public static string main_menu_asc() => GetString("main_menu_asc");
    /// <summary>
    /// DESC
    /// </summary>
    public static string main_menu_desc() => GetString("main_menu_desc");
    /// <summary>
    /// Toggle Active Group Status
    /// </summary>
    public static string main_menu_toggle() => GetString("main_menu_toggle");
    /// <summary>
    /// Rename
    /// </summary>
    public static string main_menu_rename() => GetString("main_menu_rename");
    /// <summary>
    /// Start ScreenSaver
    /// </summary>
    public static string main_menu_start() => GetString("main_menu_start");
    /// <summary>
    /// Create Dynamic Group
    /// </summary>
    public static string main_menu_dynamic() => GetString("main_menu_dynamic");
    /// <summary>
    /// Load Filter
    /// </summary>
    public static string main_menu_load_filter() => GetString("main_menu_load_filter");
    /// <summary>
    /// Set Filter To Active
    /// </summary>
    public static string main_menu_set_filter() => GetString("main_menu_set_filter");
    /// <summary>
    /// View Filter
    /// </summary>
    public static string main_menu_view_filter() => GetString("main_menu_view_filter");
    /// <summary>
    /// Please enter a name for this group:
    /// </summary>
    public static string main_menu_group_name() => GetString("main_menu_group_name");
    /// <summary>
    /// Please enter a non-empty name for this group:
    /// </summary>
    public static string main_menu_group_name_shame() => GetString("main_menu_group_name_shame");
    /// <summary>
    /// A group of the same name already exists.
    /// </summary>
    public static string main_menu_group_name_conflict() => GetString("main_menu_group_name_conflict");
    /// <summary>
    /// Are you sure you want to delete group '{$arg0}'? This action cannot be undone.
    /// </summary>
    public static string main_menu_prompt_delete(object arg0) => GetString("main_menu_prompt_delete", ("arg0", arg0));
    /// <summary>
    /// This group will have no games after this removal. Would you like to delete the group?\nOtherwise, the group will be labelled as inactive.
    /// </summary>
    public static string main_menu_group_remove_empty() => GetString("main_menu_group_remove_empty");
    /// <summary>
    /// Selected Games
    /// </summary>
    public static string main_menu_selected_games() => GetString("main_menu_selected_games");
    /// <summary>
    /// Preview ScreenSaver
    /// </summary>
    public static string game_menu_preview() => GetString("game_menu_preview");
    /// <summary>
    /// Only one game can be previewed at a time.
    /// </summary>
    public static string game_menu_preview_error() => GetString("game_menu_preview_error");
    /// <summary>
    /// Should this be the new active group?
    /// </summary>
    public static string game_menu_prompt_active() => GetString("game_menu_prompt_active");
    /// <summary>
    /// Add Selected
    /// </summary>
    public static string game_menu_add_selected() => GetString("game_menu_add_selected");
    /// <summary>
    /// Remove Selected
    /// </summary>
    public static string game_menu_remove_selected() => GetString("game_menu_remove_selected");
    /// <summary>
    /// Create Group From Selected
    /// </summary>
    public static string game_menu_create_selected() => GetString("game_menu_create_selected");
    /// <summary>
    /// Games
    /// </summary>
    public static string game_menu_games() => GetString("game_menu_games");
    /// <summary>
    /// Failed to save the update to the groups due to the following error: {$arg0}
    /// </summary>
    public static string group_save_fail(object arg0) => GetString("group_save_fail", ("arg0", arg0));
    /// <summary>
    /// Random
    /// </summary>
    public static string group_sort_rnd() => GetString("group_sort_rnd");
}

public static partial class LocId
{
    public static readonly HashSet<string> StringIds = new()
    {
        "settings_header", 
        "settings_header_general", 
        "settings_header_video", 
        "settings_header_audio", 
        "settings_header_clock", 
        "settings_audio_volume", 
        "settings_monitor", 
        "settings_audio_source", 
        "settings_enum_audio_source_none", 
        "settings_enum_audio_source_video", 
        "settings_enum_audio_source_music", 
        "settings_play_state", 
        "settings_enum_play_state_never", 
        "settings_enum_play_state_desktop", 
        "settings_enum_play_state_fullscreen", 
        "settings_enum_play_state_always", 
        "settings_game_trans", 
        "settings_svr_interval", 
        "settings_video_corner", 
        "settings_clock_corner", 
        "settings_clock_font", 
        "settings_clock_font_size", 
        "settings_clock_sub_font_size", 
        "settings_bckgrd_skip", 
        "settings_video_skip", 
        "settings_music_skip", 
        "settings_logo_skip", 
        "settings_play_backup", 
        "settings_use_micro", 
        "settings_video_backup", 
        "settings_disable_while_play", 
        "settings_pause_on_deac", 
        "settings_display_clock", 
        "settings_display_video", 
        "settings_display_logo", 
        "settings_dynamic_sort", 
        "settings_dynamic_sort_tip", 
        "settings_monitor_primary_indicator", 
        "menu_groups", 
        "menu_preview", 
        "menu_view", 
        "menu_delete", 
        "menu_prompt_sort", 
        "menu_prompt_asc", 
        "main_menu_sort", 
        "main_menu_asc", 
        "main_menu_desc", 
        "main_menu_toggle", 
        "main_menu_rename", 
        "main_menu_start", 
        "main_menu_dynamic", 
        "main_menu_load_filter", 
        "main_menu_set_filter", 
        "main_menu_view_filter", 
        "main_menu_group_name", 
        "main_menu_group_name_shame", 
        "main_menu_group_name_conflict", 
        "main_menu_prompt_delete", 
        "main_menu_group_remove_empty", 
        "main_menu_selected_games", 
        "game_menu_preview", 
        "game_menu_preview_error", 
        "game_menu_prompt_active", 
        "game_menu_add_selected", 
        "game_menu_remove_selected", 
        "game_menu_create_selected", 
        "game_menu_games", 
        "group_save_fail", 
        "group_sort_rnd"
    };

    /// <summary>
    /// Settings
    /// </summary>
    public const string settings_header = "settings_header";
    /// <summary>
    /// General
    /// </summary>
    public const string settings_header_general = "settings_header_general";
    /// <summary>
    /// Video
    /// </summary>
    public const string settings_header_video = "settings_header_video";
    /// <summary>
    /// Audio
    /// </summary>
    public const string settings_header_audio = "settings_header_audio";
    /// <summary>
    /// Clock
    /// </summary>
    public const string settings_header_clock = "settings_header_clock";
    /// <summary>
    /// Audio Volume
    /// </summary>
    public const string settings_audio_volume = "settings_audio_volume";
    /// <summary>
    /// Monitor
    /// </summary>
    public const string settings_monitor = "settings_monitor";
    /// <summary>
    /// Source For Audio
    /// </summary>
    public const string settings_audio_source = "settings_audio_source";
    /// <summary>
    /// None
    /// </summary>
    public const string settings_enum_audio_source_none = "settings_enum_audio_source_none";
    /// <summary>
    /// Video
    /// </summary>
    public const string settings_enum_audio_source_video = "settings_enum_audio_source_video";
    /// <summary>
    /// Music
    /// </summary>
    public const string settings_enum_audio_source_music = "settings_enum_audio_source_music";
    /// <summary>
    /// When To Play
    /// </summary>
    public const string settings_play_state = "settings_play_state";
    /// <summary>
    /// Never
    /// </summary>
    public const string settings_enum_play_state_never = "settings_enum_play_state_never";
    /// <summary>
    /// Desktop
    /// </summary>
    public const string settings_enum_play_state_desktop = "settings_enum_play_state_desktop";
    /// <summary>
    /// FullScreen
    /// </summary>
    public const string settings_enum_play_state_fullscreen = "settings_enum_play_state_fullscreen";
    /// <summary>
    /// Always
    /// </summary>
    public const string settings_enum_play_state_always = "settings_enum_play_state_always";
    /// <summary>
    /// Time Between Game Transitions in Seconds
    /// </summary>
    public const string settings_game_trans = "settings_game_trans";
    /// <summary>
    /// Time for ScreenSaver to Appear in Seconds
    /// </summary>
    public const string settings_svr_interval = "settings_svr_interval";
    /// <summary>
    /// Video Corner Radius
    /// </summary>
    public const string settings_video_corner = "settings_video_corner";
    /// <summary>
    /// Clock Corner Radius
    /// </summary>
    public const string settings_clock_corner = "settings_clock_corner";
    /// <summary>
    /// Clock Font
    /// </summary>
    public const string settings_clock_font = "settings_clock_font";
    /// <summary>
    /// Clock Font Size
    /// </summary>
    public const string settings_clock_font_size = "settings_clock_font_size";
    /// <summary>
    /// Clock Sub-Font Size
    /// </summary>
    public const string settings_clock_sub_font_size = "settings_clock_sub_font_size";
    /// <summary>
    /// Skip Games Missing a Background
    /// </summary>
    public const string settings_bckgrd_skip = "settings_bckgrd_skip";
    /// <summary>
    /// Skip Games Missing Video
    /// </summary>
    public const string settings_video_skip = "settings_video_skip";
    /// <summary>
    /// Skip Games Missing Music
    /// </summary>
    public const string settings_music_skip = "settings_music_skip";
    /// <summary>
    /// Skip Games Missing Logos
    /// </summary>
    public const string settings_logo_skip = "settings_logo_skip";
    /// <summary>
    /// Play backup audio if selected is missing
    /// </summary>
    public const string settings_play_backup = "settings_play_backup";
    /// <summary>
    /// Use Micro Trailer
    /// </summary>
    public const string settings_use_micro = "settings_use_micro";
    /// <summary>
    /// Play backup video if selected is missing
    /// </summary>
    public const string settings_video_backup = "settings_video_backup";
    /// <summary>
    /// Disable ScreenSaver while Playing
    /// </summary>
    public const string settings_disable_while_play = "settings_disable_while_play";
    /// <summary>
    /// Disable ScreenSaver When Not In Use
    /// </summary>
    public const string settings_pause_on_deac = "settings_pause_on_deac";
    /// <summary>
    /// Display Clock
    /// </summary>
    public const string settings_display_clock = "settings_display_clock";
    /// <summary>
    /// Display Video
    /// </summary>
    public const string settings_display_video = "settings_display_video";
    /// <summary>
    /// Display Logo
    /// </summary>
    public const string settings_display_logo = "settings_display_logo";
    /// <summary>
    /// Retrieve Dynmaic Groups in Order
    /// </summary>
    public const string settings_dynamic_sort = "settings_dynamic_sort";
    /// <summary>
    /// Enabling has the downside of briefly altering the main view UI
    /// </summary>
    public const string settings_dynamic_sort_tip = "settings_dynamic_sort_tip";
    /// <summary>
    /// (Primary)
    /// </summary>
    public const string settings_monitor_primary_indicator = "settings_monitor_primary_indicator";
    /// <summary>
    /// Groups
    /// </summary>
    public const string menu_groups = "menu_groups";
    /// <summary>
    /// Preview
    /// </summary>
    public const string menu_preview = "menu_preview";
    /// <summary>
    /// View
    /// </summary>
    public const string menu_view = "menu_view";
    /// <summary>
    /// Delete
    /// </summary>
    public const string menu_delete = "menu_delete";
    /// <summary>
    /// Please select a field to sort by:
    /// </summary>
    public const string menu_prompt_sort = "menu_prompt_sort";
    /// <summary>
    /// Should the group be sorted in ascending order?
    /// </summary>
    public const string menu_prompt_asc = "menu_prompt_asc";
    /// <summary>
    /// Change Sorting Field
    /// </summary>
    public const string main_menu_sort = "main_menu_sort";
    /// <summary>
    /// ASC
    /// </summary>
    public const string main_menu_asc = "main_menu_asc";
    /// <summary>
    /// DESC
    /// </summary>
    public const string main_menu_desc = "main_menu_desc";
    /// <summary>
    /// Toggle Active Group Status
    /// </summary>
    public const string main_menu_toggle = "main_menu_toggle";
    /// <summary>
    /// Rename
    /// </summary>
    public const string main_menu_rename = "main_menu_rename";
    /// <summary>
    /// Start ScreenSaver
    /// </summary>
    public const string main_menu_start = "main_menu_start";
    /// <summary>
    /// Create Dynamic Group
    /// </summary>
    public const string main_menu_dynamic = "main_menu_dynamic";
    /// <summary>
    /// Load Filter
    /// </summary>
    public const string main_menu_load_filter = "main_menu_load_filter";
    /// <summary>
    /// Set Filter To Active
    /// </summary>
    public const string main_menu_set_filter = "main_menu_set_filter";
    /// <summary>
    /// View Filter
    /// </summary>
    public const string main_menu_view_filter = "main_menu_view_filter";
    /// <summary>
    /// Please enter a name for this group:
    /// </summary>
    public const string main_menu_group_name = "main_menu_group_name";
    /// <summary>
    /// Please enter a non-empty name for this group:
    /// </summary>
    public const string main_menu_group_name_shame = "main_menu_group_name_shame";
    /// <summary>
    /// A group of the same name already exists.
    /// </summary>
    public const string main_menu_group_name_conflict = "main_menu_group_name_conflict";
    /// <summary>
    /// Are you sure you want to delete group '{$arg0}'? This action cannot be undone.
    /// </summary>
    public const string main_menu_prompt_delete = "main_menu_prompt_delete";
    /// <summary>
    /// This group will have no games after this removal. Would you like to delete the group?\nOtherwise, the group will be labelled as inactive.
    /// </summary>
    public const string main_menu_group_remove_empty = "main_menu_group_remove_empty";
    /// <summary>
    /// Selected Games
    /// </summary>
    public const string main_menu_selected_games = "main_menu_selected_games";
    /// <summary>
    /// Preview ScreenSaver
    /// </summary>
    public const string game_menu_preview = "game_menu_preview";
    /// <summary>
    /// Only one game can be previewed at a time.
    /// </summary>
    public const string game_menu_preview_error = "game_menu_preview_error";
    /// <summary>
    /// Should this be the new active group?
    /// </summary>
    public const string game_menu_prompt_active = "game_menu_prompt_active";
    /// <summary>
    /// Add Selected
    /// </summary>
    public const string game_menu_add_selected = "game_menu_add_selected";
    /// <summary>
    /// Remove Selected
    /// </summary>
    public const string game_menu_remove_selected = "game_menu_remove_selected";
    /// <summary>
    /// Create Group From Selected
    /// </summary>
    public const string game_menu_create_selected = "game_menu_create_selected";
    /// <summary>
    /// Games
    /// </summary>
    public const string game_menu_games = "game_menu_games";
    /// <summary>
    /// Failed to save the update to the groups due to the following error: {$arg0}
    /// </summary>
    public const string group_save_fail = "group_save_fail";
    /// <summary>
    /// Random
    /// </summary>
    public const string group_sort_rnd = "group_sort_rnd";
}
