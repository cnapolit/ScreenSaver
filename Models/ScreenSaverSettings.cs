using ScreenSaver.Models.Enums;
using System.Collections.Generic;

namespace ScreenSaver.Models
{
    public class ScreenSaverSettings : ObservableObject
    {
        public uint GameTransitionInterval { get; set; } = 20;
        public uint ScreenSaverInterval { get; set; } = 90;
        public uint Volume { get; set; } = 50;
        public uint VideoCornerRadius { get; set; } = 20;
        public uint ClockCornerRadius { get; set; } = 20;
        public uint ClockFontSize { get; set; } = 40;
        public uint ClockSubFontSize { get; set; } = 30;
        public string ClockFont { get; set; } = "Segoe UI Light";
        public bool DisplayClock { get; set; } = true;
        public bool PlayBackup { get; set; } = false;
        public bool BackgroundSkip { get; set; } = true;
        public bool VideoSkip { get; set; } = false;
        public bool MusicSkip { get; set; } = false;
        public bool LogoSkip { get; set; } = false;
        public bool IncludeVideo { get; set; } = true;
        public bool IncludeLogo { get; set; } = true;
        public bool DisableWhilePlaying { get; set; } = true;
        public bool PauseOnDeactivate { get; set; } = true;
        public AudioSource AudioSource { get; set; } = AudioSource.Music;
        public PlayState PlayState { get; set; } = PlayState.FullScreen;
    }
}
