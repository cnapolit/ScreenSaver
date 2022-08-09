using System;
using System.Runtime.InteropServices;

namespace ScreenSaver.Models.Imports
{

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHook
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
