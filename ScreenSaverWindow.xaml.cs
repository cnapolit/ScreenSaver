using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
namespace ScreenSaver
{
    /// <summary>
    /// Interaction logic for ScreenSaverWindow.xaml
    /// </summary>
    public partial class ScreenSaverWindow
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public ScreenSaverWindow()
        {
            InitializeComponent();
        }

        public void StartMedia()
        {
            Background.Play();
            video.Play();
            music.Play();
        }

        private void Background_OnMediaEnded(object sender, EventArgs e)
        {
            Background.Position = TimeSpan.FromMilliseconds(1);
            Background.Play();
        }

        private void Video_OnMediaEnded(object sender, EventArgs e)
        {
            video.Position = TimeSpan.FromMilliseconds(1);
            video.Play();
        }

        private void Music_OnMediaEnded(object sender, EventArgs e)
        {
            music.Position = TimeSpan.FromMilliseconds(1);
            music.Play();
        }

        private void Close(object sender, EventArgs e)
        {
            Background.Stop();
            Background.Close();
            Background.Source = null;
            Background = null;

            video.Stop();
            video.Close();
            video.Source = null;
            video = null;

            music.Stop();
            music.Close();
            music.Source = null;
            music = null;

            var playnite = Process.GetProcesses().Where(x => x.ProcessName.StartsWith("Playnite.")).Select(y => y.Handle).FirstOrDefault();
            if (playnite != null)
            {
                ShowWindow(playnite, 5);
            }

            GetWindow(this).Close();
        }
    }
}
