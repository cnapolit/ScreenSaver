using Playnite.SDK.Models;
using System;

namespace ScreenSaver.Services.UI.Windows
{
    internal interface IWindowsManager : IScreenSaverSettings
    {
        void StartScreenSaver();
        void StopScreenSaver();
        void UpdateScreenSaver();
        void UpdateScreenSaverTime();
        void PreviewScreenSaver(Game game, Action onCloseCallBack);
    }
}
