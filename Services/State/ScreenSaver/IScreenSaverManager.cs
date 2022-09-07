using Playnite.SDK.Models;

namespace ScreenSaver.Services.State.ScreenSaver
{
    internal interface IScreenSaverManager : IScreenSaverSettings
    {
        void SetupPolling();
        void StartPolling(bool manual, bool gameStarted);
        void PausePolling(bool ignoreCheck, bool gameStopped);
        void StopPolling();
        void PreviewScreenSaver(Game game);
    }
}
