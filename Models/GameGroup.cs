using Playnite.SDK.Models;
using System;
using System.Collections.Generic;

namespace ScreenSaver.Models
{
    internal class GameGroup
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool Ascending { get; set; }
        public string SortField { get; set; }
        public FilterPreset Filter { get; set; }
        public ISet<Guid> GameGuids { get; set; } = new HashSet<Guid>();

        //public DateTime StartTime { get; set; }
        //public DateTime EndTime { get; set; }
        //public ISet<Guid> GuidBlackList { get; set; }
    }
}
