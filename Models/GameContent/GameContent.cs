using Playnite.SDK.Models;

namespace ScreenSaver.Models.GameContent
{
    internal class GameContent
    {
        public Game   Source         { get; set; }
        public string LogoPath       { get; set; }
        public string MusicPath      { get; set; }
        public string VideoPath      { get; set; }
        public string BackgroundPath { get; set; }
    }
}
