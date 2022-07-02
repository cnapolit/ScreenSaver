using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ScreenSaver
{
    public class ScreenSaverPlugin : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ScreenSaverSettingsViewModel settings { get; set; }

        private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string IconPath = Path.Combine(PluginFolder, "icon.png");

        private static readonly List<GameMenuItem> _gameMenuItems = 
            new List<GameMenuItem> { new GameMenuItem { Action = ShowScreenSaver, Description = "Open Window", Icon = IconPath } };

        private readonly IPlayniteAPI _playniteAPI;

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseWindow(IntPtr hWnd); 
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyWindow(IntPtr hwnd);

        public ScreenSaverPlugin(IPlayniteAPI api) : base(api)
        {
            settings = new ScreenSaverSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            _playniteAPI = api;
        }

        private static void ShowScreenSaver(GameMenuItemActionArgs args)
        {
            var saver = new ScreenSaverWindow();
            saver.StartMedia();
            saver.Show();

            var processes = Process.GetProcesses();
            logger.Info($"Processes: {string.Join(", ", processes.Select(x => x.ProcessName))}");
            var playnite = processes.Where(x => x.ProcessName.StartsWith("Playnite.")).FirstOrDefault();
            if (playnite != null)
            {
                var r = CloseWindow(playnite.Handle);
                logger.Info($"attempted to close Playnite: {playnite.ProcessName} {r}");
            }
            else
            {
                logger.Info("Unable to find playnite");
            }
        }

        #region Playnite Interface

        public override Guid Id { get; } = Guid.Parse("198510bc-f254-46d5-8ac7-d048e9cd1688");

        public override ISettings GetSettings(bool firstRunSettings) => new ScreenSaverSettingsViewModel(this);

        public override UserControl GetSettingsView(bool firstRunSettings) => new ScreenSaverSettingsView();

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            //_playniteAPI.Database.;
        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {

        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {

        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {

        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {

        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {

        }

        public override void OnPropertyChanged()
        {

        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args) => _gameMenuItems;

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args) => new List<MainMenuItem>();

        #endregion

        private void ResetTimer()
        {

        }

        private void EnableTimer()
        {

        }

        private void DisableTimer()
        {

        }
    }
}