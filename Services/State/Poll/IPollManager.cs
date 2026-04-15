namespace ScreenSaver.Services.State.Poll;

internal interface IPollManager
{
    void SetupPolling();
    void StartPolling(bool immediately);
    void PausePolling();
    void StopPolling();
    void OnButtonPress();
}
