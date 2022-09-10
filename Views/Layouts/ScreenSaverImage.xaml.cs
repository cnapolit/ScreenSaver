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

        private void OnEnd(object sender, EventArgs e)
        {
            var media = sender as MediaElement;
            media.Position = TimeSpan.Zero;
            media.Play();
        }

        private void Close(object sender, EventArgs e) => ParentWindow?.Close();
    }
}
