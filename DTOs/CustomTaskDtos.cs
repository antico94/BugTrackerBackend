// DTOs/CustomTaskDtos.cs
using System.ComponentModel.DataAnnotations;
using BugTracker.Models.Enums;

namespace BugTracker.DTOs;

public class CreateCustomTaskDto
{
    [Required]
    public Guid BugId { get; set; }
    
    [Required]
    public Guid StudyId { get; set; }
    
    // Either TrialManagerId OR InteractiveResponseTechnologyId, not both
    public Guid? TrialManagerId { get; set; }
    public Guid? InteractiveResponseTechnologyId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string TaskTitle { get; set; }
    
    [Required]
    public string TaskDescription { get; set; }
    
    [StringLength(50)]
    public string? JiraTaskKey { get; set; }
    
    public string? JiraTaskLink { get; set; }
}

public class UpdateCustomTaskDto
{
    [Required]
    [StringLength(500)]
    public string TaskTitle { get; set; }
    
    [Required]
    public string TaskDescription { get; set; }
    
    [StringLength(50)]
    public string? JiraTaskKey { get; set; }
    
    public string? JiraTaskLink { get; set; }
}

public class CustomTaskResponseDto
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; }
    public string TaskDescription { get; set; }
    public string? JiraTaskKey { get; set; }
    public string? JiraTaskLink { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Foreign Keys
    public Guid BugId { get; set; }
    public Guid? StudyId { get; set; }
    public Guid? TrialManagerId { get; set; }
    public Guid? InteractiveResponseTechnologyId { get; set; }
    
    // Related Data
    public CoreBugBasicDto? CoreBug { get; set; }
    public StudyBasicDto? Study { get; set; }
    public TrialManagerSummaryDto? TrialManager { get; set; }
    public IRTBasicDto? InteractiveResponseTechnology { get; set; }
    
    // Task Steps and Notes
    public List<TaskStepResponseDto> TaskSteps { get; set; } = new List<TaskStepResponseDto>();
    public List<TaskNoteResponseDto> TaskNotes { get; set; } = new List<TaskNoteResponseDto>();
    
    // Computed Properties
    public string ProductName { get; set; }
    public string ProductVersion { get; set; }
    public ProductType ProductType { get; set; }
    public Guid? CurrentStepId { get; set; }
    public int CompletedStepsCount { get; set; }
    public int TotalStepsCount { get; set; }
}

public class CoreBugBasicDto
{
    public Guid BugId { get; set; }
    public string BugTitle { get; set; }
    public string JiraKey { get; set; }
    public string JiraLink { get; set; }
    public BugSeverity Severity { get; set; }
}

public class TaskStepResponseDto
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
    public string? DecisionAnswer { get; set; }
    public string? Notes { get; set; }
    
    // Auto-check specific fields  
    public bool? AutoCheckResult { get; set; }
    
    // Navigation fields for conditional logic
    public Guid? NextStepIfYes { get; set; }
    public Guid? NextStepIfNo { get; set; }
    public Guid? NextStepIfTrue { get; set; }
    public Guid? NextStepIfFalse { get; set; }
}

public class TaskNoteResponseDto
{
    public Guid TaskNoteId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
}

public class CreateTaskNoteDto
{
    [Required]
    public Guid TaskId { get; set; }
    
    [Required]
    public string Content { get; set; }
    
    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; }
}

public class UpdateTaskNoteDto
{
    [Required]
    public string Content { get; set; }
}

public class CompleteTaskStepDto
{
    [Required]
    public Guid TaskId { get; set; }
    
    [Required]
    public Guid TaskStepId { get; set; }
    
    public string? Notes { get; set; }
}

public class MakeDecisionDto
{
    [Required]
    public Guid TaskId { get; set; }
    
    [Required]
    public Guid TaskStepId { get; set; }
    
    [Required]
    [RegularExpression("^(Yes|No)$", ErrorMessage = "Decision must be 'Yes' or 'No'")]
    public string DecisionAnswer { get; set; }
    
    public string? Notes { get; set; }
}