// Models/TaskStep.cs
using BugTracker.Models.Enums;

namespace BugTracker.Models;

public class TaskStep
{
    public Guid TaskStepId { get; set; }
    public string Action { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public bool IsDecision { get; set; }
    public bool IsAutoCheck { get; set; }
    public bool IsTerminal { get; set; }
    public bool RequiresNote { get; set; }
    public Status Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Decision-specific fields
    public string DecisionAnswer { get; set; } // "Yes" or "No"
    public string Notes { get; set; }
    
    // Auto-check specific fields  
    public bool? AutoCheckResult { get; set; }
    
    // Navigation fields for conditional logic
    public Guid? NextStepIfYes { get; set; }
    public Guid? NextStepIfNo { get; set; }
    public Guid? NextStepIfTrue { get; set; }
    public Guid? NextStepIfFalse { get; set; }
    
    // Foreign Key
    public Guid TaskId { get; set; }
    
    // Navigation Properties
    public CustomTask Task { get; set; }
}