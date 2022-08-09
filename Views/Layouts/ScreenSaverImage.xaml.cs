using System;
using System.Windows;
using System.Windows.Controls;

namespace ScreenSaver.Views.Layouts
{
    /// <summary>
    /// Interaction logic for ScreenSaver.xaml
    /// </summary>
    public partial class ScreenSaverImage : UserControl
    {
        public Window ParentWindow { get; set; }
        public ScreenSaverImage() => InitializeComponent();
        private void Close(object sender, EventArgs e) => ParentWindow?.Close();
    }
}
