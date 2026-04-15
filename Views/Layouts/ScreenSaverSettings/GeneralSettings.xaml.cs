using Playnite;
using System.Windows.Controls;
using WpfScreenHelper;

namespace ScreenSaver.Views.Layouts.ScreenSaverSettings;

/// <summary>
/// Interaction logic for GeneralSettings.xaml
/// </summary>
public partial class GeneralSettings : UserControl
{
    public GeneralSettings()
    {
        InitializeComponent();
        Screen.AllScreens.ForEach(s => MonitorCombo.Items.Add(new ComboBoxItem
        { 
            Content = s.DeviceName
        }));
    }
}
