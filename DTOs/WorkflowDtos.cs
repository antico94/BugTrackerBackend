// DTOs/WorkflowDtos.cs
using System.ComponentModel.DataAnnotations;
using BugTracker.Models.Enums;

namespace BugTracker.DTOs;

public class WorkflowStateDto
{
    public Guid TaskId { get; set; }
    public Status TaskStatus { get; set; }
    public TaskStepDto? CurrentStep { get; set; }
    public List<WorkflowActionDto> AvailableActions { get; set; } = new List<WorkflowActionDto>();
    public WorkflowProgressDto ProgressInfo { get; set; }
    public WorkflowValidationDto ValidationRules { get; set; }
    public List<StepSummaryDto> CompletedSteps { get; set; } = new List<StepSummaryDto>();
    public List<StepSummaryDto> UpcomingSteps { get; set; } = new List<StepSummaryDto>();
    public bool IsTaskComplete { get; set; }
    public WorkflowUIHintsDto UIHints { get; set; }
}

public class TaskStepDto
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
    public string? DecisionAnswer { get; set; }
    public string? Notes { get; set; }
    public bool? AutoCheckResult { get; set; }
}

public class WorkflowActionDto
{
    public string ActionType { get; set; } // "complete", "decide_yes", "decide_no", "add_note"
    public string Label { get; set; }       // "Complete Step", "Yes", "No", etc.
    public string ButtonVariant { get; set; } // "workflow-decision", "workflow-action"
    public bool IsEnabled { get; set; }
    public string? DisabledReason { get; set; }
    public string? Description { get; set; } // Additional context for the action
}

public class WorkflowProgressDto
{
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public double PercentComplete { get; set; }
    public string StatusText { get; set; } // "Step 3 of 7", "Complete", etc.
    public bool IsInProgress { get; set; }
}

public class WorkflowValidationDto
{
    public bool RequiresNote { get; set; }
    public int MinNoteLength { get; set; }
    public int MaxNoteLength { get; set; }
    public string? NotePrompt { get; set; }
    public List<string> ValidationMessages { get; set; } = new List<string>();
    public List<string> Requirements { get; set; } = new List<string>();
}

public class StepSummaryDto
{
    public Guid TaskStepId { get; set; }
    public string Action { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public bool IsDecision { get; set; }
    public bool IsTerminal { get; set; }
    public Status Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? DecisionAnswer { get; set; }
    public string? Notes { get; set; }
    public string StatusIcon { get; set; } // "✓", "→", "○", etc.
    public string StatusColor { get; set; } // "green", "blue", "gray", etc.
}

public class WorkflowUIHintsDto
{
    public string CurrentStepType { get; set; } // "decision", "action", "terminal", "complete"
    public string ThemeColor { get; set; } // Primary color for UI elements
    public bool ShowProgressBar { get; set; }
    public bool ShowStepHistory { get; set; }
    public bool ShowUpcomingSteps { get; set; }
    public string NextStepPreview { get; set; } // Preview of what happens next
}

// Action Request DTOs
public class WorkflowActionRequestDto
{
    [Required]
    public Guid TaskId { get; set; }
    
    [Required]
    public string ActionType { get; set; } // "complete", "decide_yes", "decide_no", "add_note"
    
    public string? Note { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

public class WorkflowActionResultDto
{
    public bool Success { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();
    public List<string> WarningMessages { get; set; } = new List<string>();
    public List<string> InfoMessages { get; set; } = new List<string>();
    public WorkflowStateDto NewWorkflowState { get; set; }
    public string? NextAction { get; set; } // Suggested next action
}

// Extended DTOs for complex workflows
public class WorkflowHistoryDto
{
    public List<WorkflowEventDto> Events { get; set; } = new List<WorkflowEventDto>();
    public TimeSpan TotalDuration { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class WorkflowEventDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } // "step_completed", "decision_made", "note_added"
    public DateTime Timestamp { get; set; }
    public string Description { get; set; }
    public string? StepName { get; set; }
    public string? Decision { get; set; }
    public string? Notes { get; set; }
    public string? UserName { get; set; }
}