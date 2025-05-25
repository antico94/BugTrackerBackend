namespace BugTracker.Models;

public class TrialManager
{
    public Guid TrialManagerId { get; set; }
    public string Client { get; set; }
    public ICollection<InteractiveResponseTechnology> InteractiveResponseTechnologies { get; set; }
    public string Version { get; set; }
    public Study Study { get; set; }
    public Guid StudyId { get; set; }
}