using Playnite;
using static ScreenSaver.Services.UI.Windows.WindowsManager;

namespace ScreenSaver.Services.UI.Windows;

internal interface IWindowsManager
{
    Task StartScreenSaverAsync();
    void StopScreenSaver();
    Task UpdateScreenSaverAsync();
    void UpdateScreenSaverTime();
    void PreviewScreenSaver(Game game, Action onCloseCallBack);
    event OnStopCallback? OnStop;
}
