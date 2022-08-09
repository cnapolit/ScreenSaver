using ScreenSaver.Models;
using System.Collections.Generic;
using System.IO;

namespace ScreenSaver
{
    public class ScreenSaverViewModel : ObservableObject
    {
        private string screenSaverImagePath = null;
        public string BackgroundPath
        {
            get => screenSaverImagePath;
            set
            {
                if (!ValidPath(value)) return;
                screenSaverImagePath = value;
                OnPropertyChanged();
            }
        }

        private string logoPath = null;
        public string LogoPath
        {
            get => logoPath;
            set
            {
                if (!ValidPath(value)) return;
                logoPath = value;
                OnPropertyChanged();
            }
        }

        private string videoPath = null;
        public string VideoPath
        {
            get => videoPath;
            set
            {
                if (!ValidPath(value)) return;
                videoPath = value;
                OnPropertyChanged();
            }
        }

        private string musicPath = null;
        public string MusicPath
        {
            get => musicPath;
            set
            {
                if (!ValidPath(value)) return;
                musicPath = value;
                OnPropertyChanged();
            }
        }

        private ScreenSaverSettings settings;
        public ScreenSaverSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        private static bool ValidPath(string value) => !string.IsNullOrWhiteSpace(value) && File.Exists(value);
    }
}
