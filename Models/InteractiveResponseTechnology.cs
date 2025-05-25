// Models/InteractiveResponseTechnology.cs
using BugTracker.Models.Enums;
using BugTracker.Models.Interfaces;

namespace BugTracker.Models;

public class InteractiveResponseTechnology : IProduct
{
    public Guid InteractiveResponseTechnologyId { get; set; }
    public string Version { get; set; }
    public string JiraKey { get; set; }
    public string JiraLink { get; set; }
    public string WebLink { get; set; }
    public string Protocol { get; set; }
    
    // Foreign Keys
    public Guid StudyId { get; set; }
    public Guid TrialManagerId { get; set; }
    
    // Navigation Properties
    public Study Study { get; set; }
    public TrialManager TrialManager { get; set; }
    public ICollection<ExternalModule> ExternalModules { get; set; } = new List<ExternalModule>();
    public ICollection<CustomTask> Tasks { get; set; } = new List<CustomTask>();
    
    // IProduct implementation
    public ProductType Type => ProductType.InteractiveResponseTechnology;
    public Guid ProductId => InteractiveResponseTechnologyId;
}