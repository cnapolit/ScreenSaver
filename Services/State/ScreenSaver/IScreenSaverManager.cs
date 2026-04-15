using Playnite;

namespace ScreenSaver.Services.State.ScreenSaver;

internal interface IScreenSaverManager
{
    void SetupPolling();
    void StartPolling(bool manual, bool gameStarted);
    void PausePolling(bool ignoreCheck, bool gameStopped);
    void StopPolling();
    void PreviewScreenSaver(Game game);
    void UpdatePollState();
}
