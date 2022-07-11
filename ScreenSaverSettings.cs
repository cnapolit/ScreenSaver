using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace ScreenSaver
{
    public enum AudioSource
    {
        None,
        Video,
        Music
    }

    public enum PlayState
    {
        Never,
        Desktop,
        FullScreen,
        Always
    }

    public class ScreenSaverSettings : ObservableObject
    {
        public uint Volume { get; set; } = 50;
        public AudioSource AudioSource { get; set; } = AudioSource.Music;
        public PlayState PlayState { get; set; } = PlayState.FullScreen;
        public uint GameTransitionInterval { get; set; } = 10;
        public uint ScreenSaverInterval { get; set; } = 10;
        public uint PollInterval { get; set; } = 3;
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

    public class ScreenSaverSettingsViewModel : ObservableObject, ISettings
    {
        private readonly ScreenSaverPlugin plugin;
        private ScreenSaverSettings editingClone { get; set; }

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

        public ScreenSaverSettingsViewModel(ScreenSaverPlugin plugin)
        {
            this.plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<ScreenSaverSettings>();

            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new ScreenSaverSettings();
            }
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            if (settings.PlayState != editingClone.PlayState) switch (settings.PlayState)
            {
                case PlayState.Never:
                    plugin.StopPolling();
                    break;
                default:
                    plugin.StartPolling();
                    break;
            }
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}