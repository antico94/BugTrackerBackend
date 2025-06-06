using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models.Workflow;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Service for managing workflow definitions
/// </summary>
public class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly BugTrackerContext _context;
    private readonly ILogger<WorkflowDefinitionService> _logger;

    public WorkflowDefinitionService(BugTrackerContext context, ILogger<WorkflowDefinitionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkflowDefinition?> LoadWorkflowDefinitionAsync(string workflowName)
    {
        return await _context.WorkflowDefinitions
            .Where(wd => wd.Name == workflowName && wd.IsActive)
            .OrderByDescending(wd => wd.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
    {
        return await _context.WorkflowDefinitions
            .Where(wd => wd.IsActive)
            .OrderBy(wd => wd.Name)
            .ThenByDescending(wd => wd.CreatedAt)
            .ToListAsync();
    }

    public async Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        definition.WorkflowDefinitionId = Guid.NewGuid();
        definition.CreatedAt = DateTime.UtcNow;
        definition.UpdatedAt = DateTime.UtcNow;
        
        _context.WorkflowDefinitions.Add(definition);
        await _context.SaveChangesAsync();
        
        return definition;
    }

    public async Task<WorkflowDefinition> UpdateWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        var existingDefinition = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(wd => wd.WorkflowDefinitionId == definition.WorkflowDefinitionId);

        if (existingDefinition == null)
        {
            throw new ArgumentException($"Workflow definition with ID {definition.WorkflowDefinitionId} not found");
        }

        existingDefinition.Description = definition.Description;
        existingDefinition.Version = definition.Version;
        existingDefinition.DefinitionJson = definition.DefinitionJson;
        existingDefinition.IsActive = definition.IsActive;
        existingDefinition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingDefinition;
    }

    public async Task<bool> DeleteWorkflowDefinitionAsync(Guid definitionId)
    {
        var definition = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(wd => wd.WorkflowDefinitionId == definitionId);

        if (definition == null)
        {
            return false;
        }

        _context.WorkflowDefinitions.Remove(definition);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ValidateWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        try
        {
            var schema = definition.GetWorkflowSchema();
            var validationResult = await ValidateWorkflowSchemaAsync(schema);
            return validationResult.IsValid;
        }
        catch
        {
            return false;
        }
    }

    private async Task<WorkflowValidationResult> ValidateWorkflowSchemaAsync(WorkflowSchema schema)
    {
        return await Task.FromResult(ValidateWorkflowSchema(schema));
    }

    private WorkflowValidationResult ValidateWorkflowSchema(WorkflowSchema schema)
    {
        var result = new WorkflowValidationResult();

        try
        {
            // Validate basic structure
            if (string.IsNullOrWhiteSpace(schema.WorkflowId))
            {
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "WorkflowId",
                    ErrorCode = "REQUIRED",
                    Message = "Workflow ID is required"
                });
            }

            if (string.IsNullOrWhiteSpace(schema.Name))
            {
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "Name",
                    ErrorCode = "REQUIRED",
                    Message = "Workflow name is required"
                });
            }

            if (string.IsNullOrWhiteSpace(schema.InitialStepId))
            {
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "InitialStepId",
                    ErrorCode = "REQUIRED",
                    Message = "Initial step ID is required"
                });
            }

            if (schema.Steps == null || !schema.Steps.Any())
            {
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "Steps",
                    ErrorCode = "REQUIRED",
                    Message = "At least one step is required"
                });
                result.IsValid = false;
                return result;
            }

            // Validate steps
            var stepIds = new HashSet<string>();
            var terminalStepCount = 0;

            foreach (var step in schema.Steps)
            {
                // Check for duplicate step IDs
                if (!stepIds.Add(step.StepId))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = "Steps",
                        ErrorCode = "DUPLICATE_STEP_ID",
                        Message = $"Duplicate step ID: {step.StepId}"
                    });
                }

                // Validate step properties
                if (string.IsNullOrWhiteSpace(step.StepId))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = "Steps.StepId",
                        ErrorCode = "REQUIRED",
                        Message = "Step ID is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(step.Name))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = $"Steps[{step.StepId}].Name",
                        ErrorCode = "REQUIRED",
                        Message = $"Step name is required for step {step.StepId}"
                    });
                }

                if (step.IsTerminal)
                {
                    terminalStepCount++;
                }

                // Validate action IDs are unique within step
                var actionIds = new HashSet<string>();
                foreach (var action in step.Actions)
                {
                    if (!actionIds.Add(action.ActionId))
                    {
                        result.Errors.Add(new WorkflowValidationError
                        {
                            Field = $"Steps[{step.StepId}].Actions",
                            ErrorCode = "DUPLICATE_ACTION_ID",
                            Message = $"Duplicate action ID: {action.ActionId} in step {step.StepId}"
                        });
                    }
                }
            }

            // Validate initial step exists
            if (!string.IsNullOrWhiteSpace(schema.InitialStepId) && !stepIds.Contains(schema.InitialStepId))
            {
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "InitialStepId",
                    ErrorCode = "STEP_NOT_FOUND",
                    Message = $"Initial step {schema.InitialStepId} not found in workflow steps"
                });
            }

            // Validate at least one terminal step exists
            if (terminalStepCount == 0)
            {
                result.Warnings.Add(new WorkflowValidationWarning
                {
                    Field = "Steps",
                    WarningCode = "NO_TERMINAL_STEPS",
                    Message = "No terminal steps defined - workflow may not complete properly"
                });
            }

            // Validate transitions
            var transitionIds = new HashSet<string>();
            foreach (var transition in schema.Transitions)
            {
                // Check for duplicate transition IDs
                if (!transitionIds.Add(transition.TransitionId))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = "Transitions",
                        ErrorCode = "DUPLICATE_TRANSITION_ID",
                        Message = $"Duplicate transition ID: {transition.TransitionId}"
                    });
                }

                // Validate from step exists
                if (!stepIds.Contains(transition.FromStepId))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = $"Transitions[{transition.TransitionId}].FromStepId",
                        ErrorCode = "STEP_NOT_FOUND",
                        Message = $"From step {transition.FromStepId} not found in transition {transition.TransitionId}"
                    });
                }

                // Validate to step exists
                if (!stepIds.Contains(transition.ToStepId))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = $"Transitions[{transition.TransitionId}].ToStepId",
                        ErrorCode = "STEP_NOT_FOUND",
                        Message = $"To step {transition.ToStepId} not found in transition {transition.TransitionId}"
                    });
                }

                // Validate trigger action
                if (string.IsNullOrWhiteSpace(transition.TriggerAction))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = $"Transitions[{transition.TransitionId}].TriggerAction",
                        ErrorCode = "REQUIRED",
                        Message = $"Trigger action is required for transition {transition.TransitionId}"
                    });
                }
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow definition");
            result.IsValid = false;
            result.Errors.Add(new WorkflowValidationError
            {
                Field = "System",
                ErrorCode = "VALIDATION_ERROR",
                Message = "An error occurred during validation"
            });
        }

        return result;
    }
}