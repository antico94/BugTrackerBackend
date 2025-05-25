// Models/WeeklyCoreBugEntry.cs (Junction table)
namespace BugTracker.Models;

public class WeeklyCoreBugEntry
{
    public Guid WeeklyCoreBugEntryId { get; set; }
    
    // Foreign Keys
    public Guid WeeklyCoreBugsId { get; set; }
    public Guid BugId { get; set; }
    
    // Navigation Properties
    public WeeklyCoreBugs WeeklyCoreBugs { get; set; }
    public CoreBug CoreBug { get; set; }
}