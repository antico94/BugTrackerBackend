namespace BugTracker.Models;

public class Study
{
    public TrialManager TrialManager { get; set; }
    public string Client { get; set; }
    public ICollection<InteractiveResponseTechnology> InteractiveResponseTechnologies { get; set; }
}