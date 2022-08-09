using Playnite.SDK.Models;

namespace ScreenSaver.Services.State.ScreenSaver
{
    internal interface IScreenSaverManager : IScreenSaverSettings
    {
        void SetupPolling();
        void StartPolling(bool manual);
        void PausePolling(bool ignoreCheck);
        void StopPolling();
        void PreviewScreenSaver(Game game);
    }
}
