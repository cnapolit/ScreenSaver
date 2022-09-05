using Sounds;

namespace ScreenSaver.Services.State.Poll
{
    internal interface IPollManager : IScreenSaverSettings
    {
        void SetupPolling(ISounds sounds);
        void StartPolling(bool immediately);
        void PausePolling();
        void StopPolling();
    }
}
