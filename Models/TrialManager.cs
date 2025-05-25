// Models/TrialManager.cs

using BugTracker.Models.Enums;
using BugTracker.Models.Interfaces;

namespace BugTracker.Models;

public class TrialManager : IProduct
{
    public Guid TrialManagerId { get; set; }
    public string Version { get; set; }
    public string JiraKey { get; set; }
    public string JiraLink { get; set; }
    public string WebLink { get; set; }
    
    // Foreign Key
    public Guid ClientId { get; set; }
    
    // Navigation Properties
    public Client Client { get; set; }
    public ICollection<Study> Studies { get; set; } = new List<Study>();
    public ICollection<CustomTask> Tasks { get; set; } = new List<CustomTask>();
    
    // IProduct implementation
    public Study Study { get; set; }
    public Guid StudyId { get; set; }
    public ProductType Type => ProductType.TM;
    public Guid ProductId => TrialManagerId;
    public string Protocol { get; set; }
}