using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenSaver
{
    public class ScreenSaverSettings : ObservableObject
    {
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
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}