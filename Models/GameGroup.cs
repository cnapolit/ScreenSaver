using System;
using System.Collections.Generic;
namespace ScreenSaver.Models
{
    internal class GameGroup
    {
        public string Name { get; set; }
        public ISet<Guid> GameGuids { get; set; }
        public bool IsActive { get; set; }

        //public DateTime StartTime { get; set; }
        //public DateTime EndTime { get; set; }
        //public object Filter { get; set; }
        //public object SortField { get; set; }
        //public ISet<Guid> GuidBlackList { get; set; }
    }
}
