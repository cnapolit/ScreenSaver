using Playnite;
using ScreenSaver.Common.Constants;
using ScreenSaver.Models;
using System.IO;
using System.Text.Json;

namespace ScreenSaver.Services.State.Settings;

public class SettingsService(IPlayniteApi api) : ISettingsService
{

    private class SettingsRef : ISettingsRef
    {
        public required ScreenSaverSettings Settings { get; set; }
    }

    private SettingsRef? _settingsRef;

    public async Task<ISettingsRef> GetSettingsReferenceAsync() => _settingsRef ??= await GetSettingsAsync();
    private async Task<SettingsRef> GetSettingsAsync()
    {
        var settingsFilePath = GetSettingsFilePath();
        ScreenSaverSettings? settings = null;
        if (File.Exists(settingsFilePath))
        {
            using var fileStream = File.OpenRead(settingsFilePath);
            fileStream.Position = 0;
            settings = await JsonSerializer.DeserializeAsync<ScreenSaverSettings>(fileStream);
        }

        return new() { Settings = settings ?? new() };
    }

    public void UpdateSettingsAsync(ScreenSaverSettings settings)
    {
        if (_settingsRef is null)
        {
            throw new ArgumentException("GetSettingsReferenceAsync must be called before UpdateSettingsAsync");
        }
        _settingsRef.Settings = settings;
    }

    public async Task SaveSettingsAsync()
    {
        if (_settingsRef is null)
        {
            throw new ArgumentException("GetSettingsReferenceAsync must be called before SaveSettingsAsync");
        }

        using var fileStream = File.OpenWrite(GetSettingsFilePath());
        await JsonSerializer.SerializeAsync(fileStream, _settingsRef.Settings);
    }
    private string GetSettingsFilePath() => Path.Combine(api.UserDataDir, App.SettingsFileName);
}