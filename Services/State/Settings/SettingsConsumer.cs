using ScreenSaver.Models;

namespace ScreenSaver.Services.State.Settings;


public abstract class SettingsConsumer
{
    public required ISettingsRef SettingsRef { private get; init; }
    protected ScreenSaverSettings Settings => SettingsRef.Settings;
}
