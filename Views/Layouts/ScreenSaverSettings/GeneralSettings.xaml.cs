using ScreenSaver.Common.Constants;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Forms;

namespace ScreenSaver.Views.Layouts.ScreenSaverSettings
{
    /// <summary>
    /// Interaction logic for GeneralSettings.xaml
    /// </summary>
    public partial class GeneralSettings : System.Windows.Controls.UserControl
    {
        public GeneralSettings()
        {
            InitializeComponent();
            Screen.AllScreens.ForEach(s => MonitorCombo.Items.Add(new ComboBoxItem
            { 
                Content = s.DeviceName + (s.Primary ? " " + Resource.SETTINGS_MONITOR_PRIMARY_INDICATOR : string.Empty)
            }));
        }
    }
}
