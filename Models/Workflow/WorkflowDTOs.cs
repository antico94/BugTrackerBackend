using System.ComponentModel.DataAnnotations;

namespace BugTracker.Models.Workflow;

/// <summary>
/// Request for executing an action on a workflow
/// </summary>
public class WorkflowActionRequest
{
    [Required]
    public string ActionId { get; set; } = string.Empty;
    
    public string? Decision { get; set; }
    
    public string? Notes { get; set; }
    
    public Dictionary<string, object>? AdditionalData { get; set; }
    
    [Required]
    public string PerformedBy { get; set; } = "User";
}

/// <summary>
/// Result of executing an action on a workflow
/// </summary>
public class WorkflowActionResult
{
    public bool Success { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    public string? ErrorCode { get; set; }
    
    public string? PreviousStepId { get; set; }
    
    public string? NewStepId { get; set; }
    
    public bool WorkflowCompleted { get; set; }
    
    public WorkflowState? NewState { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Result of validating workflow rules or conditions
/// </summary>
public class WorkflowValidationResult
{
    public bool IsValid { get; set; } = true;
    
    public List<WorkflowValidationError> Errors { get; set; } = new();
    
    public List<WorkflowValidationWarning> Warnings { get; set; } = new();
    
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Represents a validation error
/// </summary>
public class WorkflowValidationError
{
    public string Field { get; set; } = string.Empty;
    
    public string ErrorCode { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public object? Value { get; set; }
}

/// <summary>
/// Represents a validation warning
/// </summary>
public class WorkflowValidationWarning
{
    public string Field { get; set; } = string.Empty;
    
    public string WarningCode { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public object? Value { get; set; }
}

/// <summary>
/// Request for creating a new workflow definition
/// </summary>
public class CreateWorkflowDefinitionRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public WorkflowSchema Schema { get; set; } = new();
    
    [MaxLength(100)]
    public string CreatedBy { get; set; } = "User";
}

/// <summary>
/// Response for workflow definition operations
/// </summary>
public class WorkflowDefinitionResponse
{
    public Guid WorkflowDefinitionId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Version { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
    
    public WorkflowSchema? Schema { get; set; }
}

/// <summary>
/// Response for workflow audit trail
/// </summary>
public class WorkflowAuditResponse
{
    public Guid TaskId { get; set; }
    
    public string WorkflowName { get; set; } = string.Empty;
    
    public WorkflowExecutionStatus Status { get; set; }
    
    public DateTime StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public TimeSpan? TotalDuration { get; set; }
    
    public List<WorkflowAuditLogEntry> AuditTrail { get; set; } = new();
}

/// <summary>
/// Audit log entry for the response
/// </summary>
public class WorkflowAuditLogEntry
{
    public Guid AuditLogId { get; set; }
    
    public string StepId { get; set; } = string.Empty;
    
    public string StepName { get; set; } = string.Empty;
    
    public string Action { get; set; } = string.Empty;
    
    public string Result { get; set; } = string.Empty;
    
    public string? PreviousStepId { get; set; }
    
    public string? NextStepId { get; set; }
    
    public string? Decision { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string PerformedBy { get; set; } = string.Empty;
    
    public long? DurationMs { get; set; }
    
    public List<WorkflowConditionEvaluationResult>? ConditionsEvaluated { get; set; }
}

/// <summary>
/// Result of evaluating a workflow condition
/// </summary>
public class WorkflowConditionEvaluationResult
{
    public string ConditionId { get; set; } = string.Empty;
    
    public string Field { get; set; } = string.Empty;
    
    public WorkflowConditionOperator Operator { get; set; }
    
    public object? ExpectedValue { get; set; }
    
    public object? ActualValue { get; set; }
    
    public bool Result { get; set; }
    
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for workflow statistics
/// </summary>
public class WorkflowStatistics
{
    public int TotalWorkflows { get; set; }
    public int ActiveWorkflows { get; set; }
    public int CompletedWorkflows { get; set; }
    public int FailedWorkflows { get; set; }
    public double AverageCompletionTime { get; set; }
    public List<WorkflowDefinitionSummary> DefinitionSummaries { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Summary of workflow definition for management views
/// </summary>
public class WorkflowDefinitionSummary
{
    public Guid WorkflowDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}