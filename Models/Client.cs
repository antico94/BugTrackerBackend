// Models/Client.cs

using System.ComponentModel.DataAnnotations;

namespace BugTracker.Models;

public class Client
{
    public Guid ClientId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Navigation Properties
    public TrialManager TrialManager { get; set; }
    public ICollection<Study> Studies { get; set; } = new List<Study>();
}