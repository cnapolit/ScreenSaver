using CommunityToolkit.Mvvm.ComponentModel;
using Playnite;
using ScreenSaver.Models;
using ScreenSaver.Services.State.ScreenSaver;
using ScreenSaver.Services.State.Settings;
using ScreenSaver.Views.Layouts.ScreenSaverSettings;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ScreenSaver.Views.Models;

[INotifyPropertyChanged]
internal partial class ScreenSaverSettingsHandler(
    ISettingsService settingsService, IScreenSaverManager screenSaverManager) : PluginSettingsHandler
{
    private ScreenSaverSettings? EditingClone { get; set; }
    private ISettingsRef? _settingsRef;
    public ScreenSaverSettings? Settings => _settingsRef?.Settings;

    public override FrameworkElement GetEditView(GetSettingsViewArgs args) => new ScreenSaverSettingsView { DataContext = this };

    public override async Task BeginEditAsync(BeginEditArgs args)
    {
        _settingsRef = await settingsService.GetSettingsReferenceAsync();
        using MemoryStream stream = new();
        await JsonSerializer.SerializeAsync(stream, Settings);
        stream.Position = 0;
        EditingClone = await JsonSerializer.DeserializeAsync<ScreenSaverSettings>(stream);
    }

    public override async Task CancelEditAsync(CancelEditArgs args) => settingsService.UpdateSettingsAsync(EditingClone!);

    public override async Task EndEditAsync(EndEditArgs args)
    {
        await settingsService.SaveSettingsAsync();
        screenSaverManager.UpdatePollState();
    }

    public override async Task<ICollection<string>> VerifySettingsAsync(VerifySettingsArgs args)
    {
        await Task.CompletedTask;
        return [];
    }
}
