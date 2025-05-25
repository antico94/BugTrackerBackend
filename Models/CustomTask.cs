namespace BugTracker.Models;

public class CustomTask
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; }
    public string TaskDescription { get; set; }
    public Guid BugId { get; set; }
    public CoreBug CoreBug { get; set; }
    public Study Study { get; set; }
    public Guid StudyId { get; set; }
    public Product Product { get; set; }
    public Guid ProductId { get; set; }
    public string JiraTaskLink { get; set; }
    public string JiraProductLink { get; set; }
}