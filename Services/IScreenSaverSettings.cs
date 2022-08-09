using ScreenSaver.Models;

namespace ScreenSaver.Services
{
    internal interface IScreenSaverSettings
    {
        void UpdateSettings(ScreenSaverSettings settings);
    }
}
