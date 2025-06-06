using BugTracker.Models.Workflow;
using BugTracker.Controllers;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Interface for workflow execution service
/// </summary>
public interface IWorkflowExecutionService
{
    /// <summary>
    /// Gets a workflow execution by task ID
    /// </summary>
    Task<WorkflowExecution?> GetWorkflowExecutionAsync(Guid taskId);

    /// <summary>
    /// Creates a new workflow execution for a task
    /// </summary>
    Task<WorkflowExecution> CreateWorkflowExecutionAsync(Guid taskId, Guid workflowDefinitionId, string initialStepId, Dictionary<string, object>? context = null);

    /// <summary>
    /// Updates the current step of a workflow execution
    /// </summary>
    Task UpdateWorkflowExecutionStepAsync(Guid workflowExecutionId, string newStepId, Dictionary<string, object>? updatedContext = null);

    /// <summary>
    /// Completes a workflow execution
    /// </summary>
    Task CompleteWorkflowExecutionAsync(Guid workflowExecutionId);

    /// <summary>
    /// Adds an audit log entry for a workflow execution
    /// </summary>
    Task AddAuditLogAsync(WorkflowAuditLog auditLog);

    /// <summary>
    /// Suspends a workflow execution
    /// </summary>
    Task SuspendWorkflowExecutionAsync(Guid workflowExecutionId, string reason);

    /// <summary>
    /// Resumes a suspended workflow execution
    /// </summary>
    Task ResumeWorkflowExecutionAsync(Guid workflowExecutionId);

    /// <summary>
    /// Fails a workflow execution with an error message
    /// </summary>
    Task FailWorkflowExecutionAsync(Guid workflowExecutionId, string errorMessage, Exception? exception = null);

    /// <summary>
    /// Gets the complete audit trail for a workflow execution
    /// </summary>
    Task<List<WorkflowAuditLog>> GetAuditTrailAsync(Guid workflowExecutionId);

    /// <summary>
    /// Gets all active workflow definitions
    /// </summary>
    Task<List<WorkflowDefinition>> GetWorkflowDefinitionsAsync();

    /// <summary>
    /// Gets workflow execution statistics
    /// </summary>
    Task<WorkflowStatistics> GetWorkflowStatisticsAsync();
}