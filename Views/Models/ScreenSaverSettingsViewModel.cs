using Playnite.SDK;
using Playnite.SDK.Data;
using ScreenSaver.Models;
using System.Collections.Generic;

namespace ScreenSaver.Views.Models
{
    public class ScreenSaverSettingsViewModel : ObservableObject, ISettings
    {
        private readonly ScreenSaverPlugin plugin;
        private ScreenSaverSettings EditingClone { get; set; }

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

            if (savedSettings is null)
            {
                Settings = new ScreenSaverSettings();
            }
            else
            {
                Settings = savedSettings;
            }
        }

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        public void EndEdit()
        {
            plugin. SavePluginSettings (Settings);
            plugin.     UpdateSettings (Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}