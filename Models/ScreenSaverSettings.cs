using ScreenSaver.Models.Enums;
using System.Collections.Generic;


namespace ScreenSaver.Models
{
    public class ScreenSaverSettings : ObservableObject
    {
        private uint gameTransitionInterval = 20000;
        public uint GameTransitionInterval { get => gameTransitionInterval; set => gameTransitionInterval = value * 1000; }
        private uint screenSaverInterval = 90000;
        public uint ScreenSaverInterval { get => screenSaverInterval; set => screenSaverInterval = value * 1000; }
        public uint Volume { get; set; } = 50;
        public AudioSource AudioSource { get; set; } = AudioSource.Music;
        public PlayState PlayState { get; set; } = PlayState.FullScreen;
        public bool PlayBackup { get; set; } = false;
        public bool BackgroundSkip { get; set; } = true;
        public bool VideoSkip { get; set; } = false;
        public bool MusicSkip { get; set; } = false;
        public bool LogoSkip { get; set; } = false;
        public bool IncludeVideo { get; set; } = true;
        public bool IncludeLogo { get; set; } = true;
        public bool DisableWhilePlaying { get; set; } = true;
        public bool PauseOnDeactivate { get; set; } = true;
    }
}
