using BugTracker.Models.Enums;

namespace BugTracker.Models;

public class CoreBug
{
    public Guid BugId { get; set; }
    public string BugTitle { get; set; }
    public string JiraKey { get; set; }
    public string BugDescription { get; set; }
    public Status Status { get; set; }
}