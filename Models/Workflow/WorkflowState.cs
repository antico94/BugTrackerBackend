using System.Text.Json;

namespace BugTracker.Models.Workflow;

/// <summary>
/// Represents the complete current state of a workflow execution
/// This is what gets returned to the frontend as the single source of truth
/// </summary>
public class WorkflowState
{
    public Guid TaskId { get; set; }
    public Guid WorkflowExecutionId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkflowVersion { get; set; } = string.Empty;
    public WorkflowExecutionStatus Status { get; set; }
    
    /// <summary>
    /// The current step the workflow is on
    /// </summary>
    public WorkflowCurrentStep? CurrentStep { get; set; }
    
    /// <summary>
    /// Actions available in the current step
    /// </summary>
    public List<WorkflowAvailableAction> AvailableActions { get; set; } = new();
    
    /// <summary>
    /// Validation rules for the current step
    /// </summary>
    public List<WorkflowValidationState> ValidationRules { get; set; } = new();
    
    /// <summary>
    /// Progress information
    /// </summary>
    public WorkflowProgress Progress { get; set; } = new();
    
    /// <summary>
    /// Completed steps in the current execution path
    /// </summary>
    public List<WorkflowCompletedStep> CompletedSteps { get; set; } = new();
    
    /// <summary>
    /// Possible next steps (for decision previews)
    /// </summary>
    public List<WorkflowNextStep> PossibleNextSteps { get; set; } = new();
    
    /// <summary>
    /// UI hints for rendering
    /// </summary>
    public WorkflowUIHints UIHints { get; set; } = new();
    
    /// <summary>
    /// Context variables for this workflow execution
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
    
    /// <summary>
    /// When this state was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this state was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the workflow execution started
    /// </summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>
    /// Who performed the workflow actions
    /// </summary>
    public string PerformedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Total number of steps in the workflow
    /// </summary>
    public int TotalSteps { get; set; }
    
    /// <summary>
    /// Error message if the workflow is in error state
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the current step in the workflow
/// </summary>
public class WorkflowCurrentStep
{
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public bool IsTerminal { get; set; }
    public bool RequiresNote { get; set; }
    public bool AutoExecute { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents an available action in the current step
/// </summary>
public class WorkflowAvailableAction
{
    public string ActionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public WorkflowActionType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public string ButtonVariant { get; set; } = "default";
    public string? GlowColor { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents validation state for the current step
/// </summary>
public class WorkflowValidationState
{
    public string RuleId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public WorkflowValidationType Type { get; set; }
    public bool IsRequired { get; set; }
    public object? Value { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
}

/// <summary>
/// Represents workflow progress information
/// </summary>
public class WorkflowProgress
{
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public double PercentComplete { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public bool IsInProgress { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// Represents a completed step in the workflow
/// </summary>
public class WorkflowCompletedStep
{
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? Decision { get; set; }
    public string? Notes { get; set; }
    public string CompletedBy { get; set; } = string.Empty;
    public long? DurationMs { get; set; }
}

/// <summary>
/// Represents a possible next step (for decision previews)
/// </summary>
public class WorkflowNextStep
{
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public bool IsTerminal { get; set; }
    public string Condition { get; set; } = string.Empty; // "Yes", "No", etc.
    public string? PreviewText { get; set; }
}

/// <summary>
/// UI hints for rendering the workflow state
/// </summary>
public class WorkflowUIHints
{
    public string CurrentStepType { get; set; } = string.Empty;
    public string ThemeColor { get; set; } = "blue";
    public bool ShowProgressBar { get; set; } = true;
    public bool ShowStepHistory { get; set; } = true;
    public bool ShowUpcomingSteps { get; set; } = true;
    public string? NextStepPreview { get; set; }
    public Dictionary<string, object> CustomHints { get; set; } = new();
}