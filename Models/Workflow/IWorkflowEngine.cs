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

/// <summary>
/// Interface for evaluating workflow conditions and rules
/// </summary>
public interface IWorkflowRuleEngine
{
    /// <summary>
    /// Evaluates a list of conditions against the current context
    /// </summary>
    Task<bool> EvaluateConditionsAsync(List<WorkflowCondition> conditions, Dictionary<string, object> context);
    
    /// <summary>
    /// Evaluates a single condition against the current context
    /// </summary>
    Task<bool> EvaluateConditionAsync(WorkflowCondition condition, Dictionary<string, object> context);
    
    /// <summary>
    /// Validates input against workflow validation rules
    /// </summary>
    Task<WorkflowValidationResult> ValidateInputAsync(List<WorkflowValidationRule> rules, Dictionary<string, object> input);
}

/// <summary>
/// Interface for managing workflow definitions
/// </summary>
public interface IWorkflowDefinitionService
{
    /// <summary>
    /// Gets a workflow definition by name
    /// </summary>
    Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string name);
    
    /// <summary>
    /// Gets all active workflow definitions
    /// </summary>
    Task<List<WorkflowDefinition>> GetActiveWorkflowDefinitionsAsync();
    
    /// <summary>
    /// Creates or updates a workflow definition
    /// </summary>
    Task<WorkflowDefinition> SaveWorkflowDefinitionAsync(WorkflowDefinition definition);
    
    /// <summary>
    /// Validates a workflow definition
    /// </summary>
    Task<WorkflowValidationResult> ValidateWorkflowDefinitionAsync(WorkflowSchema schema);
}

/// <summary>
/// Interface for managing workflow executions
/// </summary>
public interface IWorkflowExecutionService
{
    /// <summary>
    /// Gets a workflow execution by task ID
    /// </summary>
    Task<WorkflowExecution?> GetWorkflowExecutionAsync(Guid taskId);
    
    /// <summary>
    /// Creates a new workflow execution
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
    /// Adds an audit log entry
    /// </summary>
    Task AddAuditLogAsync(WorkflowAuditLog auditLog);
}