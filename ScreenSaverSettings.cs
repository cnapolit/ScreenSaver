﻿using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenSaver
{
    public enum AudioSource
    {
        None,
        Video,
        Music
    }

    public class ScreenSaverSettings : ObservableObject
    {
        public uint Volume { get; set; } = 50;
        public AudioSource AudioSource { get; set; } = AudioSource.Music;
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
        public bool DisablePoll { get; set; } = false;
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