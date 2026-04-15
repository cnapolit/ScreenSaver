using Playnite;
using ScreenSaver.Common.Constants;
using ScreenSaver.Services.State.ScreenSaver;
using System.IO;
using System.Reflection;
using static Playnite.MenuItemImpl;
using static Playnite.Plugin;
using GameGroup = ScreenSaver.Models.GameGroup;

namespace ScreenSaver.Services.UI.Menus;

internal class MenuManager(
    IPlayniteApi playniteAPI, IGameGroupManager gameGroupManager, IScreenSaverManager screenSaverManager) : IMenuManager
{
    #region Infrastructure

    #region Variables

    private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    private static readonly string IconPath     = Path.Combine(PluginFolder, Files.Icon);

    #endregion

    #endregion

    #region Implementation

    #region GetGameMenuItems

    public ICollection<MenuItemImpl> GetGameMenuItems(GetGameMenuItemsArgs args) => args.ItemId switch
    {
        GAME_MENU_PREVIEW => [new(Resource.GAME_MENU_PREVIEW, OpenScreenSaver, false, UIIcon.FromBitmapFile(IconPath))],
        GAME_MENU_CREATE_SELECTED => [new(Resource.GAME_MENU_CREATE_SELECTED, CreateGameGroupAsync)],
        MENU_GROUPS => gameGroupManager.GameGroups.Count is 0 ? [] : [new(Resource.MENU_GROUPS, GetGameGroupsItems)],
        _ => throw new ArgumentException($"Do not recognize id {args.ItemId}"),
    };
    private IEnumerable<MenuItemImpl> GetGameGroupsItems(GetChildrenArgs _) => 
        from gameGroup in gameGroupManager.GameGroups.OrderBy(g => g.Name)
        select new MenuItemImpl(GetDisplayName(gameGroup), () => GetGameGroupItems(gameGroup));

    private IEnumerable<MenuItemImpl> GetGameGroupItems(GameGroup gameGroup)
    {
        yield return new(Resource.GAME_MENU_ADD_SELECTED,
            () => gameGroupManager.AddGamesToGroup(gameGroup, SelectedGuids));
        yield return new(Resource.GAME_MENU_REMOVE_SELECTED,
            () => gameGroupManager.RemoveGamesFromGroup(gameGroup, SelectedGuids));
    }

    private void OpenScreenSaver()
    {
        if (SelectedGames.Count() != 1) throw new Exception(Resource.GAME_MENU_PREVIEW_ERROR);

        screenSaverManager.PreviewScreenSaver(SelectedGames.First());
    }

    private async Task CreateGameGroupAsync()
    {
        var name = await PromptForGroupNameAsync(string.Empty);
        if (name is null)
        {
            return;
        }

        var sortField = await PromptForSortingFieldAsync();
        if (sortField is null)
        {
            return;
        }

        var group = new GameGroup
        { 
            Name = name,
            IsActive = await GetBoolFromYesNoDialogAsync(Resource.GAME_MENU_PROMPT_ACTIVE), 
            GameGuids = SelectedGuids.ToHashSet(),
            SortField = sortField,
            Ascending = await PromptForAscendingAsync(sortField)
        };

        gameGroupManager.CreateGameGroup(group);
    }

    private static readonly List<ChooseDialogItem> SortableFields =
    [
        new(            "None", string.Empty),
        new(           "Added", string.Empty),
        new(      "Categories", string.Empty),
        new(  "CommunityScore", string.Empty),
        new("CompletionStatus", string.Empty),
        new(     "CriticScore", string.Empty),
        new(        "Favorite", string.Empty),
        new(          "Genres", string.Empty),
        new(     "IsInstalled", string.Empty),
        new(    "LastActivity", string.Empty),
        new(        "Modified", string.Empty),
        new(        "Playtime", string.Empty),
        new(       "Platforms", string.Empty),
        new(          "Random", string.Empty),
        new(     "ReleaseDate", string.Empty),
        new(          "Series", string.Empty),
        new(     "SortingName", string.Empty),
        new(          "Source", string.Empty),
        new(            "Tags", string.Empty),
        new(       "UserScore", string.Empty),
        new(         "Version", string.Empty)
    ];

    private async Task<string?> PromptForSortingFieldAsync()
    {
        var result = await playniteAPI.Dialogs.ChooseItemWithSearchAsync(
                string.Empty,
                async s => SortableFields.OrderBy(f => f.Name?.StartsWith(s.SearchTerm ?? string.Empty)).ToList(),
                Resource.MENU_PROMPT_SORT);
        return result?.Name;
    }

    #endregion

    #region GetMainMenuItems
    public ICollection<MenuItemImpl> GetMainMenuItems(GetAppMenuItemsArgs args) => args.ItemId switch
    {
        MAIN_MENU_START   => [new(Resource.MAIN_MENU_START, ManuallyStartScreenSaver, false, UIIcon.FromBitmapFile(IconPath))],
        MAIN_MENU_DYNAMIC => [new(Resource.MAIN_MENU_DYNAMIC, CreateDynamicGameGroupAsync, false, UIIcon.FromBitmapFile(IconPath))],
        MENU_GROUPS       => gameGroupManager.GameGroups.Count is 0? [] : [new(Resource.MENU_GROUPS, GetGroupItems)],
        _ => throw new ArgumentException($"Do not recognize id {args.ItemId}"),
    };

    private IEnumerable<MenuItemImpl> GetGroupItems(GetChildrenArgs args) =>
        from gameGroup in gameGroupManager.GameGroups
        orderby gameGroup.Name
        select new MenuItemImpl(GetDisplayName(gameGroup), _ => GetGameGroupChildrenItems(gameGroup));

    private IEnumerable<MenuItemImpl> GetGameGroupChildrenItems(GameGroup gameGroup)
    {
        //mainMenuItems.Add(new MenuItemImpl
        //{
        //    Action = null,
        //    MenuSection = $"@ScreenSaver|Groups|{gameGroup.Name}",
        //    Description = "Preview",
        //    Icon = IconPath
        //});
        yield return new MenuItemImpl(Resource.MAIN_MENU_TOGGLE, () => gameGroupManager.ToggleGameGroupActiveStatus(gameGroup));
        yield return new MenuItemImpl(Resource.MAIN_MENU_RENAME, () => RenameGameGroupAsync(gameGroup));
        yield return new MenuItemImpl(Resource.MENU_DELETE,      () => DeleteGroupAsync(gameGroup));

        var sortStatus = string.Empty;
        if (gameGroup.SortField != null)
        {
            var sortPrefix = gameGroup.Ascending ? Resource.MAIN_MENU_ASC : Resource.MAIN_MENU_DESC;
            sortStatus = $" ({gameGroup.SortField}:{sortPrefix})";
        }

        yield return new MenuItemImpl(Resource.MAIN_MENU_SORT + sortStatus, () => SetSortFieldAsync(gameGroup));

        yield return new MenuItemImpl(
            Resource.MAIN_MENU_SET_FILTER,
            () => SetGameGroupFilter(gameGroup),
            false,
            UIIcon.FromBitmapFile(IconPath));

        if (gameGroup.Filter != null)
            yield return new MenuItemImpl(
                Resource.MAIN_MENU_LOAD_FILTER,
                () => playniteAPI.MainView.ApplyFiltersAsync(gameGroup.Filter));

        yield return new(Resource.MAIN_MENU_SELECTED_GAMES, _ => GetGroupSelectedGamesItems(gameGroup));
    }

    private IEnumerable<MenuItemImpl> GetGroupSelectedGamesItems(GameGroup gameGroup)
        => gameGroup.GameGuids
          .Select(playniteAPI.Library.Games.Get)
          .Where(g => g != null)
          .OrderBy(g => g!.Name)
          .Select(g => new MenuItemImpl(g!.Name, _ => GetGroupSelectedGameItems(gameGroup, g)));

    private IEnumerable<MenuItemImpl> GetGroupSelectedGameItems(GameGroup gameGroup, Game game)
    {
        yield return new MenuItemImpl(Resource.MENU_VIEW,    () => playniteAPI.MainView.SelectGame(game.Id));
        yield return new MenuItemImpl(Resource.MENU_PREVIEW, () => screenSaverManager.PreviewScreenSaver(game));
        yield return new MenuItemImpl(Resource.MENU_DELETE,  async () => await RemoveGameFromGroupAsync(gameGroup, game));
    }

    private async Task RenameGameGroupAsync(GameGroup gameGroup)
    {
        var name = await PromptForGroupNameAsync(gameGroup.Name);
        if (name is null)
        {
            return;
        }

        gameGroupManager.RenameGameGroup(gameGroup, name);
    }

    private void SetGameGroupFilter(GameGroup gameGroup)
    {
        gameGroup.Filter = playniteAPI.MainView.GetCurrentFilters();
        gameGroupManager.SaveGameGroupChanges();
    }

    private async Task CreateDynamicGameGroupAsync()
    {
        var name = await PromptForGroupNameAsync(string.Empty);
        if (name is null)
        {
            return;
        }

        var sortingField = await PromptForSortingFieldAsync();
        if (sortingField is null)
        {
            return;
        }

        var gameGroup = new GameGroup
        {
            Name = name,
            SortField = sortingField,
            Ascending = await PromptForAscendingAsync(sortingField),
            Filter = playniteAPI.MainView.GetCurrentFilters()
        };
        gameGroupManager.CreateGameGroup(gameGroup);
    }

    private async Task SetSortFieldAsync(GameGroup gameGroup)
    {
        var field = await PromptForSortingFieldAsync();
        if (field is null)
        {
            return;
        }

        gameGroupManager.SetSortField(gameGroup, field, await PromptForAscendingAsync(field));
    }

    private async ValueTask RemoveGameFromGroupAsync(GameGroup gameGroup, Game game)
    { 
        if (gameGroup.GameGuids.Count is 1 && await GetBoolFromYesNoDialogAsync(Resource.MAIN_MENU_GROUP_REMOVE_EMPTY))
        {
            gameGroupManager.DeleteGameGroup(gameGroup);
            return;
        }

        gameGroupManager.RemoveGamesFromGroup(gameGroup, [game.Id]);
    }

    private async Task DeleteGroupAsync(GameGroup gameGroup)
    {
        if (await GetBoolFromYesNoDialogAsync(string.Format(Resource.MAIN_MENU_PROMPT_DELETE, gameGroup.Name)))
        {
            gameGroupManager.DeleteGameGroup(gameGroup);
        }
    }

    private void ManuallyStartScreenSaver() => screenSaverManager.StartPolling(true, false);

    #endregion

    #region Helpers

    #region Prompts

    private async ValueTask<bool> PromptForAscendingAsync(string sortField) 
        => sortField != Resource.GROUP_SORT_RND && await GetBoolFromYesNoDialogAsync(Resource.MENU_PROMPT_ASC);

    private async Task<string?> PromptForGroupNameAsync(string suggestedName)
    {
        var result = await playniteAPI.Dialogs.SelectStringAsync(Resource.MAIN_MENU_GROUP_NAME, App.Name, suggestedName);
        if (!result.Result)
        {
            return null;
        }

        var groupName = result.SelectedString;
        while (string.IsNullOrWhiteSpace(groupName))
        {
            var groupResult = await playniteAPI.Dialogs.SelectStringAsync(
                Resource.MAIN_MENU_GROUP_NAME_SHAME, App.Name, suggestedName);
            if (!groupResult.Result)
            {
                return null;
            }
            
            groupName = groupResult.SelectedString;
        }
        return groupName;
    }

    private async Task<bool> GetBoolFromYesNoDialogAsync(string caption)
    {
        var result = await playniteAPI.Dialogs.ShowMessageAsync(caption, App.Name, MessageBoxButtons.YesNo);
        return result is MessageBoxResult.Yes;
    }

    #endregion

    private static string GetDisplayName(GameGroup group) => group.IsActive ? group.Name + " •" : group.Name;


    private const string GAME_MENU_PREVIEW = App.Name + ".PREVIEW";
    private const string GAME_MENU_CREATE_SELECTED = App.Name + ".CREATE.SELECTED";
    private const string MENU_GROUPS = App.Name + ".GROUPS";
    private const string MAIN_MENU_START = App.Name + ".START";
    private const string MAIN_MENU_DYNAMIC = App.Name + ".CREATE.DYNAMIC";

    public ICollection<MenuItemDescriptor>? GetAppMenuItemDescriptors(GetAppMenuItemDescriptorsArgs args) => 
    [
        new(MAIN_MENU_START, Resource.MAIN_MENU_START),
        new(MAIN_MENU_DYNAMIC, Resource.MAIN_MENU_DYNAMIC),
        new(MENU_GROUPS, Resource.MENU_GROUPS)
    ];

    public ICollection<MenuItemDescriptor> GetGameMenuItemDescriptors(GetGameMenuItemDescriptorsArgs args) =>
    [
        new(GAME_MENU_PREVIEW, Resource.GAME_MENU_PREVIEW),
        new(GAME_MENU_CREATE_SELECTED, Resource.GAME_MENU_CREATE_SELECTED),
        new(MENU_GROUPS, Resource.MENU_GROUPS)
    ];

    private IEnumerable<Game> SelectedGames => playniteAPI.MainView.GetSelectedGames();
    private IEnumerable<string> SelectedGuids => SelectedGames.Select(g => g.Id);

    #endregion

    #endregion
}
