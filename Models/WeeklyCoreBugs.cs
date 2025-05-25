using BugTracker.Models.Enums;

namespace BugTracker.Models;

public class WeeklyCoreBugs
{
    public Guid WeeklyCoreBugsId { get; set; }
    public string Name { get; set; }
    public ICollection<CoreBug> CoreBugs { get; set; }
    public ICollection<CustomTask> CustomTasks { get; set; }
    public Status Status { get; set; }
}