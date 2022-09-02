using System;

namespace ScreenSaver.Models.GameContent
{
    internal class GameContent
    {
        public Guid   Id             { get; set; }
        public string GameName       { get; set; }
        public string LogoPath       { get; set; }
        public string MusicPath      { get; set; }
        public string VideoPath      { get; set; }
        public string BackgroundPath { get; set; }
    }
}
