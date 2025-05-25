using BugTracker.Models.Enums;

namespace BugTracker.Models;

public class CustomTask
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; }
    public string TaskDescription { get; set; }
    public string JiraTaskKey { get; set; } // Clone of CoreBug JiraKey for this specific product
    public string JiraTaskLink { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Foreign Keys
    public Guid BugId { get; set; }
    public Guid? StudyId { get; set; } // Nullable since TM tasks might not be study-specific
    public Guid? TrialManagerId { get; set; } // Set if task is for TM
    public Guid? InteractiveResponseTechnologyId { get; set; } // Set if task is for IRT
    
    // Navigation Properties
    public CoreBug CoreBug { get; set; }
    public Study Study { get; set; }
    public TrialManager TrialManager { get; set; }
    public InteractiveResponseTechnology InteractiveResponseTechnology { get; set; }
    public ICollection<TaskStep> TaskSteps { get; set; } = new List<TaskStep>();
    public ICollection<TaskNote> TaskNotes { get; set; } = new List<TaskNote>();
    
    // Computed Properties
    public string ProductName => TrialManager?.Client?.Name ?? InteractiveResponseTechnology?.Study?.Name ?? "Unknown";
    public string ProductVersion => TrialManager?.Version ?? InteractiveResponseTechnology?.Version ?? "Unknown";
    public ProductType ProductType => TrialManager != null ? ProductType.TM : ProductType.InteractiveResponseTechnology;
}