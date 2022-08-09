namespace ScreenSaver.Services.State.Poll
{
    internal interface IPollManager : IScreenSaverSettings
    {
        void SetupPolling();
        void StartPolling(bool immediately);
        void PausePolling();
        void StopPolling();
    }
}
