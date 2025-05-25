// Models/CoreBug.cs
using BugTracker.Models.Enums;

namespace BugTracker.Models;

public class CoreBug
{
    public Guid BugId { get; set; }
    public string BugTitle { get; set; }
    public string JiraKey { get; set; }
    public string JiraLink { get; set; }
    public string BugDescription { get; set; }
    public Status Status { get; set; }
    
    // JIRA Import Fields
    public string FoundInBuild { get; set; }
    public string AffectedVersions { get; set; } // JSON array from JIRA XML
    public BugSeverity Severity { get; set; }
    
    // Assessment Fields (set via UI)
    public ProductType? AssessedProductType { get; set; } // User-selected during assessment
    public string AssessedImpactedVersions { get; set; } // User-selected versions JSON
    public bool IsAssessed { get; set; } = false;
    public DateTime? AssessedAt { get; set; }
    public string AssessedBy { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    // Navigation Properties
    public ICollection<CustomTask> Tasks { get; set; } = new List<CustomTask>();
    public ICollection<WeeklyCoreBugEntry> WeeklyCoreBugEntries { get; set; } = new List<WeeklyCoreBugEntry>();
}