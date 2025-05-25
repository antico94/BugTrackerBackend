// Models/WeeklyCoreBugs.cs
using BugTracker.Models.Enums;

namespace BugTracker.Models;

public class WeeklyCoreBugs
{
    public Guid WeeklyCoreBugsId { get; set; }
    public string Name { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation Properties
    public ICollection<WeeklyCoreBugEntry> WeeklyCoreBugEntries { get; set; } = new List<WeeklyCoreBugEntry>();
}