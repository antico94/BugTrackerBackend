// Models/TrialManager.cs (Updated)

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
    public string Protocol { get; set; }
    
    // Foreign Key
    public Guid ClientId { get; set; }
    
    // Navigation Properties
    public Client Client { get; set; }
    public ICollection<Study> Studies { get; set; } = new List<Study>();
    public ICollection<CustomTask> Tasks { get; set; } = new List<CustomTask>();
    
    // IProduct implementation - These will be ignored in EF configuration
    // They're only used for the interface contract, not database mapping
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Study Study { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] 
    public Guid StudyId { get; set; }
    
    public ProductType Type => ProductType.TM;
    public Guid ProductId => TrialManagerId;
}