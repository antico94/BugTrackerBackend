namespace BugTracker.Models.Workflow;

/// <summary>
/// Core interface for the workflow engine
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Gets the current state of a workflow execution
    /// </summary>
    Task<WorkflowState> GetWorkflowStateAsync(Guid taskId);
    
    /// <summary>
    /// Executes an action on a workflow
    /// </summary>
    Task<WorkflowActionResult> ExecuteActionAsync(Guid taskId, WorkflowActionRequest request);
    
    /// <summary>
    /// Starts a new workflow execution for a task
    /// </summary>
    Task<WorkflowExecution> StartWorkflowAsync(Guid taskId, string workflowDefinitionName, Dictionary<string, object>? initialContext = null);
    
    /// <summary>
    /// Gets the audit trail for a workflow execution
    /// </summary>
    Task<List<WorkflowAuditLog>> GetAuditTrailAsync(Guid taskId);
    
    /// <summary>
    /// Validates if an action can be performed on the current step
    /// </summary>
    Task<WorkflowValidationResult> ValidateActionAsync(Guid taskId, WorkflowActionRequest request);
}

