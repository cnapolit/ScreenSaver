using Playnite;
using ScreenSaver.Common.Constants;
using ScreenSaver.Services;
using ScreenSaver.Services.State.Poll;
using ScreenSaver.Services.State.ScreenSaver;
using ScreenSaver.Services.State.Settings;
using ScreenSaver.Services.UI.Menus;
using ScreenSaver.Services.UI.Windows;
using ScreenSaver.Views.Models;
namespace ScreenSaver;

public class ScreenSaverPlugin : Plugin
{
    #region Infrastructure

    public const string Id = "cnapolit.ScreenSaver";

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    private IMenuManager?               _menuManager;
    private IGameGroupManager?     _gameGroupManager;
    private IScreenSaverManager? _screenSaverManager;
    private IPollManager?               _pollManager;
    private ISettingsService?       _settingsService;
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

    #endregion

    #region Playnite Interface

    public override async Task OnGameStartingAsync(OnGameStartingEventArgs _)
    {
        await Task.CompletedTask;
        _screenSaverManager?.PausePolling(false, true);
    }

    public override async Task OnGameStoppedAsync(OnGameStoppedEventArgs _)
    {
        await Task.CompletedTask;
        _screenSaverManager?.StartPolling(false, true);
    }

    public override async Task OnApplicationStartupAsync(OnApplicationStartupArgs _)
    {
        await Task.CompletedTask;
        _screenSaverManager?.SetupPolling();
    }

    public override async Task OnApplicationShutdownAsync(OnApplicationShutdownArgs args)
    {
        await Task.CompletedTask;
        _screenSaverManager?.StopPolling();
    }

    public override async Task OnGameCollectionChange(DataCollectionChangeArgs<Game> args)
    {
        await Task.CompletedTask;
        var removedItems = args.RemovedItems ?? [];
        _gameGroupManager?.RemoveGamesFromGroups(removedItems.Select(g => g.Id));
    }

    public override async Task OnGamepadConnectedAsync(OnGamepadConnectedArgs args)
    {
        await Task.CompletedTask;
        _pollManager?.OnButtonPress();
    }

    public override async Task OnGamepadButtonStateChangedAsync(OnGamepadButtonStateChangedArgs args)
    {
        await Task.CompletedTask;
        _pollManager?.OnButtonPress();
    }

    public override ICollection<MenuItemImpl>? GetGameMenuItems(GetGameMenuItemsArgs args)
        => _menuManager?.GetGameMenuItems(args) ?? throw new InvalidOperationException("InitializeAsync not invoked");

    public override ICollection<MenuItemImpl>? GetAppMenuItems(GetAppMenuItemsArgs args)
        => _menuManager?.GetMainMenuItems(args);

    public override ICollection<MenuItemDescriptor>? GetAppMenuItemDescriptors(GetAppMenuItemDescriptorsArgs args)
        => _menuManager?.GetAppMenuItemDescriptors(args);

    public override ICollection<MenuItemDescriptor> GetGameMenuItemDescriptors(GetGameMenuItemDescriptorsArgs args)
        => _menuManager?.GetGameMenuItemDescriptors(args) ?? throw new InvalidOperationException("InitializeAsync not invoked");

    public override async Task InitializeAsync(InitializeArgs args)
    {
        await Task.CompletedTask;
        Resource.Api = args.Api;
        Loc.Api = args.Api;
        _settingsService = new SettingsService(args.Api);
        var settingsRef = await _settingsService.GetSettingsReferenceAsync();
        var soundsLoaded = args.Api.Addons.Plugins.Any(p => p.Id == App.Sounds);
        var gameContentFactory = new GameContentFactory(args.Api) { SettingsRef = settingsRef };
        _gameGroupManager = new GameGroupManager(args.Api.AppInfo.ExtensionsDataDirectory);
        var windowsManager = new WindowsManager(args.Api, _gameGroupManager, gameContentFactory) { SettingsRef = settingsRef };
        _pollManager = new PollManager(windowsManager, soundsLoaded) { SettingsRef = settingsRef };
        _screenSaverManager = new ScreenSaverManager(args.Api, windowsManager, _pollManager, soundsLoaded) { SettingsRef = settingsRef };
        _menuManager = new MenuManager(args.Api, _gameGroupManager, _screenSaverManager);
    }

    public override async Task<PluginSettingsHandler?> GetSettingsHandlerAsync(GetSettingsHandlerArgs args)
    {
        await Task.CompletedTask;
        if (_settingsService is null || _screenSaverManager is null)
        {
            throw new InvalidOperationException("InitializeAsync not invoked");
        }
        return new ScreenSaverSettingsHandler(_settingsService, _screenSaverManager);
    }

    #endregion
}