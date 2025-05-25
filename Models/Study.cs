// Models/Study.cs  
namespace BugTracker.Models;

public class Study
{
    public Guid StudyId { get; set; }
    public string Name { get; set; }
    public string Protocol { get; set; }
    public string Description { get; set; }
    
    // Foreign Keys
    public Guid ClientId { get; set; }
    public Guid TrialManagerId { get; set; }
    
    // Navigation Properties
    public Client Client { get; set; }
    public TrialManager TrialManager { get; set; }
    public ICollection<InteractiveResponseTechnology> InteractiveResponseTechnologies { get; set; } = new List<InteractiveResponseTechnology>();
    public ICollection<CustomTask> Tasks { get; set; } = new List<CustomTask>();
}