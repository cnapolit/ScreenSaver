using ScreenSaver.Common.Constants;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Forms;
using Xceed.Wpf.Toolkit;

namespace ScreenSaver.Views.Layouts
{
    public class UIntDUp : UIntegerUpDown { }
    public partial class ScreenSaverSettingsView : System.Windows.Controls.UserControl
    {
        public ScreenSaverSettingsView()
        {
            InitializeComponent();
            Screen.AllScreens.ForEach(s => MonitorCombo.Items.Add(new ComboBoxItem
            { Content=s.DeviceName + (s.Primary ? Resource.SETTINGS_MONITOR_PRIMARY_INDICATOR : string.Empty) }));
        }
    }
}