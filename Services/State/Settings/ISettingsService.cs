using ScreenSaver.Models;

namespace ScreenSaver.Services.State.Settings;

public interface ISettingsService
{
    Task<ISettingsRef> GetSettingsReferenceAsync();
    Task SaveSettingsAsync();
    void UpdateSettingsAsync(ScreenSaverSettings settings);
}