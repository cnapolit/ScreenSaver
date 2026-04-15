using Playnite;

namespace ScreenSaver.Models;

internal class GameGroup
{
    public required string Name { get; set; }
    public bool IsActive { get; set; }
    public bool Ascending { get; set; }
    public string? SortField { get; set; }
    public FilteringConfiguration? Filter { get; set; }
    public ISet<string> GameGuids { get; set; } = new HashSet<string>();

    //public DateTime StartTime { get; set; }
    //public DateTime EndTime { get; set; }
    //public ISet<Guid> GuidBlackList { get; set; }
}
