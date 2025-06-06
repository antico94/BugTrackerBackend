using BugTracker.Models.Workflow;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Service interface for managing workflow definitions
/// </summary>
public interface IWorkflowDefinitionService
{
    /// <summary>
    /// Loads a workflow definition by name
    /// </summary>
    Task<WorkflowDefinition?> LoadWorkflowDefinitionAsync(string workflowName);
    
    /// <summary>
    /// Gets all available workflow definitions
    /// </summary>
    Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync();
    
    /// <summary>
    /// Creates a new workflow definition
    /// </summary>
    Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinition definition);
    
    /// <summary>
    /// Updates an existing workflow definition
    /// </summary>
    Task<WorkflowDefinition> UpdateWorkflowDefinitionAsync(WorkflowDefinition definition);
    
    /// <summary>
    /// Deletes a workflow definition
    /// </summary>
    Task<bool> DeleteWorkflowDefinitionAsync(Guid definitionId);
    
    /// <summary>
    /// Validates a workflow definition
    /// </summary>
    Task<bool> ValidateWorkflowDefinitionAsync(WorkflowDefinition definition);
    
    /// <summary>
    /// Saves or updates a workflow definition
    /// </summary>
    Task<WorkflowDefinition> SaveWorkflowDefinitionAsync(WorkflowDefinition definition);
    
    /// <summary>
    /// Gets all active workflow definitions
    /// </summary>
    Task<List<WorkflowDefinition>> GetActiveWorkflowDefinitionsAsync();
}