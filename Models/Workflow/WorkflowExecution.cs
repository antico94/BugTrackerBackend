using System.ComponentModel.DataAnnotations;
using BugTracker.Models.Enums;

namespace BugTracker.Models.Workflow;

/// <summary>
/// Represents the execution state of a workflow instance
/// </summary>
public class WorkflowExecution
{
    public Guid WorkflowExecutionId { get; set; }
    
    /// <summary>
    /// The task this workflow execution is associated with
    /// </summary>
    public Guid TaskId { get; set; }
    
    /// <summary>
    /// Reference to the workflow definition being executed
    /// </summary>
    public Guid WorkflowDefinitionId { get; set; }
    
    /// <summary>
    /// Current step in the workflow
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CurrentStepId { get; set; } = string.Empty;
    
    /// <summary>
    /// Overall status of the workflow execution
    /// </summary>
    public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Active;
    
    /// <summary>
    /// JSON representation of the current workflow context/variables
    /// </summary>
    public string ContextJson { get; set; } = "{}";
    
    /// <summary>
    /// When this workflow execution started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this workflow execution completed (if completed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Who started this workflow execution
    /// </summary>
    [MaxLength(100)]
    public string StartedBy { get; set; } = "System";
    
    /// <summary>
    /// Last time this workflow was updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Error message if the workflow execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    // Navigation Properties
    public CustomTask Task { get; set; } = null!;
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public ICollection<WorkflowAuditLog> AuditLogs { get; set; } = new List<WorkflowAuditLog>();
}

/// <summary>
/// Represents an audit log entry for workflow execution
/// </summary>
public class WorkflowAuditLog
{
    public Guid WorkflowAuditLogId { get; set; }
    
    /// <summary>
    /// The workflow execution this log entry belongs to
    /// </summary>
    public Guid WorkflowExecutionId { get; set; }
    
    /// <summary>
    /// The step this log entry is related to
    /// </summary>
    [MaxLength(100)]
    public string StepId { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name of the step
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string StepName { get; set; } = string.Empty;
    
    /// <summary>
    /// The action that was performed
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Result of the action (Success, Failed, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Result { get; set; } = string.Empty;
    
    /// <summary>
    /// Previous step before this action
    /// </summary>
    [MaxLength(100)]
    public string? PreviousStepId { get; set; }
    
    /// <summary>
    /// Next step after this action
    /// </summary>
    [MaxLength(100)]
    public string? NextStepId { get; set; }
    
    /// <summary>
    /// Decision made (for decision steps)
    /// </summary>
    [MaxLength(50)]
    public string? Decision { get; set; }
    
    /// <summary>
    /// Notes provided by user
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// JSON representation of conditions evaluated
    /// </summary>
    public string? ConditionsEvaluated { get; set; }
    
    /// <summary>
    /// JSON representation of the workflow context at this point
    /// </summary>
    public string? ContextSnapshot { get; set; }
    
    /// <summary>
    /// When this action occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Who performed this action
    /// </summary>
    [MaxLength(100)]
    public string PerformedBy { get; set; } = "System";
    
    /// <summary>
    /// Duration of the action in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }
    
    // Navigation Properties
    public WorkflowExecution WorkflowExecution { get; set; } = null!;
}

/// <summary>
/// Status of a workflow execution
/// </summary>
public enum WorkflowExecutionStatus
{
    /// <summary>
    /// Workflow is currently active and can be progressed
    /// </summary>
    Active,
    
    /// <summary>
    /// Workflow has completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Workflow has been suspended and needs manual intervention
    /// </summary>
    Suspended,
    
    /// <summary>
    /// Workflow has failed due to an error
    /// </summary>
    Failed,
    
    /// <summary>
    /// Workflow has been cancelled
    /// </summary>
    Cancelled
}