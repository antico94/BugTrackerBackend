using Microsoft.AspNetCore.Mvc;
using BugTracker.Models.Workflow;
using BugTracker.Services.Workflow;
using System.ComponentModel.DataAnnotations;

namespace BugTracker.Controllers;

// Response DTOs for WorkflowController
public class WorkflowStepState
{
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public bool IsTerminal { get; set; }
    public int Order { get; set; }
    public WorkflowStepConfig Config { get; set; } = new();
}

public class WorkflowStepConfig
{
    public bool RequiresNote { get; set; }
    public bool AutoExecute { get; set; }
    public List<ValidationRule> ValidationRules { get; set; } = new();
}

public class ValidationRule
{
    public string RuleId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class WorkflowActionState
{
    public string ActionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public WorkflowActionType Type { get; set; }
    public bool IsEnabled { get; set; }
    public string Description { get; set; } = string.Empty;
}

public enum WorkflowActionType
{
    Complete,
    Decide,
    Skip
}

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowEngine workflowEngine,
        IWorkflowExecutionService workflowExecutionService,
        ILogger<WorkflowController> logger)
    {
        _workflowEngine = workflowEngine;
        _workflowExecutionService = workflowExecutionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current workflow state for a task - single source of truth for frontend
    /// </summary>
    [HttpGet("{taskId}/state")]
    public async Task<ActionResult<WorkflowStateResponse>> GetWorkflowState(
        [FromRoute] Guid taskId)
    {
        try
        {
            _logger.LogInformation("Getting workflow state for task {TaskId}", taskId);

            var workflowState = await _workflowEngine.GetWorkflowStateAsync(taskId);
            
            if (workflowState == null)
            {
                _logger.LogWarning("No workflow state found for task {TaskId}", taskId);
                return NotFound(new { message = "Workflow state not found for this task" });
            }

            var response = new WorkflowStateResponse
            {
                TaskId = taskId,
                WorkflowName = workflowState.WorkflowName,
                Status = workflowState.Status,
                CurrentStep = workflowState.CurrentStep,
                AvailableActions = workflowState.AvailableActions,
                CompletedSteps = workflowState.CompletedSteps,
                Context = workflowState.Context,
                LastUpdated = workflowState.LastUpdated,
                ErrorMessage = workflowState.ErrorMessage,
                ExecutionMetadata = new WorkflowExecutionMetadata
                {
                    StartedAt = workflowState.StartedAt,
                    PerformedBy = workflowState.PerformedBy,
                    TotalSteps = workflowState.TotalSteps,
                    CompletedStepsCount = workflowState.CompletedSteps?.Count ?? 0,
                    ProgressPercentage = CalculateProgressPercentage(workflowState)
                }
            };

            _logger.LogDebug("Returning workflow state for task {TaskId}: {Status}, Step: {StepName}", 
                taskId, workflowState.Status, workflowState.CurrentStep?.Name ?? "None");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow state for task {TaskId}", taskId);
            return StatusCode(500, new { message = "Internal server error retrieving workflow state" });
        }
    }

    /// <summary>
    /// Executes a workflow action - unified action execution endpoint
    /// </summary>
    [HttpPost("{taskId}/execute-action")]
    public async Task<ActionResult<WorkflowActionResponse>> ExecuteAction(
        [FromRoute] Guid taskId,
        [FromBody] ExecuteActionRequest request)
    {
        try
        {
            _logger.LogInformation("Executing action {ActionId} for task {TaskId} by {PerformedBy}", 
                request.ActionId, taskId, request.PerformedBy);

            // Validate request
            if (string.IsNullOrEmpty(request.ActionId))
            {
                return BadRequest(new { message = "ActionId is required" });
            }

            if (string.IsNullOrEmpty(request.PerformedBy))
            {
                return BadRequest(new { message = "PerformedBy is required" });
            }

            // Create workflow action request
            var actionRequest = new WorkflowActionRequest
            {
                ActionId = request.ActionId,
                PerformedBy = request.PerformedBy,
                Notes = request.Notes,
                AdditionalData = request.AdditionalData ?? new Dictionary<string, object>()
            };

            // Execute the action
            var result = await _workflowEngine.ExecuteActionAsync(taskId, actionRequest);

            var response = new WorkflowActionResponse
            {
                Success = result.Success,
                Message = result.Message,
                NewState = result.NewState,
                ValidationErrors = result.ValidationErrors ?? new List<string>(),
                ExecutedAt = DateTime.UtcNow,
                ActionId = request.ActionId,
                PreviousStepId = result.PreviousStepId,
                NextStepId = result.NewState?.CurrentStep?.StepId
            };

            if (!result.Success)
            {
                _logger.LogWarning("Action execution failed for task {TaskId}: {Message}", taskId, result.Message);
                return BadRequest(response);
            }

            _logger.LogInformation("Successfully executed action {ActionId} for task {TaskId}. New step: {StepName}", 
                request.ActionId, taskId, result.NewState?.CurrentStep?.Name ?? "Terminal");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action {ActionId} for task {TaskId}", 
                request.ActionId, taskId);
            return StatusCode(500, new { message = "Internal server error executing action" });
        }
    }

    /// <summary>
    /// Gets the complete audit trail for a workflow execution
    /// </summary>
    [HttpGet("{taskId}/audit")]
    public async Task<ActionResult<WorkflowAuditResponse>> GetWorkflowAudit(
        [FromRoute] Guid taskId)
    {
        try
        {
            _logger.LogInformation("Getting workflow audit trail for task {TaskId}", taskId);

            var execution = await _workflowExecutionService.GetWorkflowExecutionAsync(taskId);
            if (execution == null)
            {
                _logger.LogWarning("No workflow execution found for task {TaskId}", taskId);
                return NotFound(new { message = "Workflow execution not found for this task" });
            }

            var auditTrail = await _workflowExecutionService.GetAuditTrailAsync(execution.WorkflowExecutionId);

            var response = new WorkflowAuditResponse
            {
                TaskId = taskId,
                WorkflowExecutionId = execution.WorkflowExecutionId,
                WorkflowName = execution.WorkflowDefinition?.Name ?? "Unknown",
                Status = execution.Status,
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt,
                AuditTrail = auditTrail.Select(entry => new WorkflowAuditEntry
                {
                    AuditId = entry.WorkflowAuditId,
                    StepId = entry.StepId,
                    StepName = entry.StepName,
                    ActionTaken = entry.ActionTaken,
                    PerformedBy = entry.PerformedBy,
                    PerformedAt = entry.PerformedAt,
                    Notes = entry.Notes,
                    PreviousStepId = entry.PreviousStepId,
                    NextStepId = entry.NextStepId,
                    ExecutionContext = entry.ExecutionContext != null 
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(entry.ExecutionContext)
                        : new Dictionary<string, object>()
                }).ToList(),
                TotalSteps = auditTrail.Count,
                Summary = new WorkflowAuditSummary
                {
                    DecisionPoints = auditTrail.Count(a => a.ActionTaken.StartsWith("decide_")),
                    AutomatedSteps = auditTrail.Count(a => a.ActionTaken == "auto_evaluate"),
                    ManualSteps = auditTrail.Count(a => a.ActionTaken == "complete"),
                    TotalDuration = execution.CompletedAt.HasValue 
                        ? execution.CompletedAt.Value - execution.StartedAt
                        : DateTime.UtcNow - execution.StartedAt
                }
            };

            _logger.LogDebug("Returning audit trail for task {TaskId} with {EntryCount} entries", 
                taskId, auditTrail.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow audit trail for task {TaskId}", taskId);
            return StatusCode(500, new { message = "Internal server error retrieving audit trail" });
        }
    }

    /// <summary>
    /// Gets all available workflow definitions for management
    /// </summary>
    [HttpGet("definitions")]
    public async Task<ActionResult<List<WorkflowDefinitionSummary>>> GetWorkflowDefinitions()
    {
        try
        {
            _logger.LogInformation("Getting all workflow definitions");

            var definitions = await _workflowExecutionService.GetWorkflowDefinitionsAsync();
            
            var summaries = definitions.Select(def => new WorkflowDefinitionSummary
            {
                WorkflowDefinitionId = def.WorkflowDefinitionId,
                Name = def.Name,
                Description = def.Description,
                Version = def.Version,
                IsActive = def.IsActive,
                CreatedAt = def.CreatedAt,
                CreatedBy = def.CreatedBy
            }).ToList();

            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow definitions");
            return StatusCode(500, new { message = "Internal server error retrieving workflow definitions" });
        }
    }

    /// <summary>
    /// Gets workflow execution statistics for monitoring
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<WorkflowStatistics>> GetWorkflowStatistics()
    {
        try
        {
            _logger.LogInformation("Getting workflow execution statistics");

            var stats = await _workflowExecutionService.GetWorkflowStatisticsAsync();
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow statistics");
            return StatusCode(500, new { message = "Internal server error retrieving workflow statistics" });
        }
    }

    private double CalculateProgressPercentage(WorkflowState workflowState)
    {
        if (workflowState.TotalSteps <= 0)
            return 0;

        var completedCount = workflowState.CompletedSteps?.Count ?? 0;
        return Math.Round((double)completedCount / workflowState.TotalSteps * 100, 2);
    }
}

/// <summary>
/// Request model for executing workflow actions
/// </summary>
public class ExecuteActionRequest
{
    [Required]
    public string ActionId { get; set; } = string.Empty;

    [Required]
    public string PerformedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Response model for workflow state queries
/// </summary>
public class WorkflowStateResponse
{
    public Guid TaskId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public WorkflowExecutionStatus Status { get; set; }
    public WorkflowStepState? CurrentStep { get; set; }
    public List<WorkflowActionState> AvailableActions { get; set; } = new();
    public List<WorkflowStepState> CompletedSteps { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public string? ErrorMessage { get; set; }
    public WorkflowExecutionMetadata ExecutionMetadata { get; set; } = new();
}

/// <summary>
/// Response model for workflow action execution
/// </summary>
public class WorkflowActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public WorkflowState? NewState { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public DateTime ExecutedAt { get; set; }
    public string ActionId { get; set; } = string.Empty;
    public string? PreviousStepId { get; set; }
    public string? NextStepId { get; set; }
}

/// <summary>
/// Response model for workflow audit queries
/// </summary>
public class WorkflowAuditResponse
{
    public Guid TaskId { get; set; }
    public Guid WorkflowExecutionId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public WorkflowExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<WorkflowAuditEntry> AuditTrail { get; set; } = new();
    public int TotalSteps { get; set; }
    public WorkflowAuditSummary Summary { get; set; } = new();
}

/// <summary>
/// Workflow execution metadata for progress tracking
/// </summary>
public class WorkflowExecutionMetadata
{
    public DateTime StartedAt { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public int TotalSteps { get; set; }
    public int CompletedStepsCount { get; set; }
    public double ProgressPercentage { get; set; }
}

/// <summary>
/// Individual audit trail entry
/// </summary>
public class WorkflowAuditEntry
{
    public Guid AuditId { get; set; }
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
    public string? PreviousStepId { get; set; }
    public string? NextStepId { get; set; }
    public Dictionary<string, object> ExecutionContext { get; set; } = new();
}

/// <summary>
/// Summary statistics for workflow audit
/// </summary>
public class WorkflowAuditSummary
{
    public int DecisionPoints { get; set; }
    public int AutomatedSteps { get; set; }
    public int ManualSteps { get; set; }
    public TimeSpan TotalDuration { get; set; }
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

/// <summary>
/// Workflow execution statistics for monitoring
/// </summary>
public class WorkflowStatistics
{
    public int TotalExecutions { get; set; }
    public int ActiveExecutions { get; set; }
    public int CompletedExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double AverageCompletionTimeMinutes { get; set; }
    public Dictionary<string, int> StepCompletionCounts { get; set; } = new();
    public Dictionary<string, int> WorkflowUsageCounts { get; set; } = new();
}