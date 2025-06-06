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

    public async Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string name)
    {
        return await _context.WorkflowDefinitions
            .Where(wd => wd.Name == name && wd.IsActive)
            .OrderByDescending(wd => wd.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<WorkflowDefinition>> GetActiveWorkflowDefinitionsAsync()
    {
        return await _context.WorkflowDefinitions
            .Where(wd => wd.IsActive)
            .OrderBy(wd => wd.Name)
            .ThenByDescending(wd => wd.CreatedAt)
            .ToListAsync();
    }

    public async Task<WorkflowDefinition> SaveWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        try
        {
            // Validate the workflow definition
            var schema = definition.GetWorkflowSchema();
            var validationResult = await ValidateWorkflowDefinitionAsync(schema);
            
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.Message));
                throw new ArgumentException($"Invalid workflow definition: {errors}");
            }

            // Check if this is a new definition or an update
            var existingDefinition = await _context.WorkflowDefinitions
                .FirstOrDefaultAsync(wd => wd.WorkflowDefinitionId == definition.WorkflowDefinitionId);

            if (existingDefinition == null)
            {
                // New definition
                definition.WorkflowDefinitionId = Guid.NewGuid();
                definition.CreatedAt = DateTime.UtcNow;
                definition.UpdatedAt = DateTime.UtcNow;
                
                _context.WorkflowDefinitions.Add(definition);
                _logger.LogInformation("Creating new workflow definition: {Name} v{Version}", definition.Name, definition.Version);
            }
            else
            {
                // Update existing definition
                existingDefinition.Description = definition.Description;
                existingDefinition.Version = definition.Version;
                existingDefinition.DefinitionJson = definition.DefinitionJson;
                existingDefinition.IsActive = definition.IsActive;
                existingDefinition.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Updating workflow definition: {Name} v{Version}", definition.Name, definition.Version);
            }

            await _context.SaveChangesAsync();
            
            return existingDefinition ?? definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving workflow definition: {Name}", definition.Name);
            throw;
        }
    }

    public async Task<WorkflowValidationResult> ValidateWorkflowDefinitionAsync(WorkflowSchema schema)
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

            if (!schema.Steps.Any())
            {
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "Steps",
                    ErrorCode = "REQUIRED",
                    Message = "At least one step is required"
                });
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

                // Validate step structure
                if (string.IsNullOrWhiteSpace(step.Name))
                {
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = $"Steps[{step.StepId}].Name",
                        ErrorCode = "REQUIRED",
                        Message = $"Step name is required for step {step.StepId}"
                    });
                }

                // Count terminal steps
                if (step.IsTerminal)
                {
                    terminalStepCount++;
                }

                // Validate step actions
                if (!step.Actions.Any() && !step.Config.AutoExecute)
                {
                    result.Warnings.Add(new WorkflowValidationWarning
                    {
                        Field = $"Steps[{step.StepId}].Actions",
                        WarningCode = "NO_ACTIONS",
                        Message = $"Step {step.StepId} has no actions and is not auto-execute"
                    });
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

            // Check for unreachable steps (steps that have no transitions leading to them except initial step)
            var reachableSteps = new HashSet<string> { schema.InitialStepId };
            foreach (var transition in schema.Transitions)
            {
                reachableSteps.Add(transition.ToStepId);
            }

            foreach (var step in schema.Steps)
            {
                if (!reachableSteps.Contains(step.StepId))
                {
                    result.Warnings.Add(new WorkflowValidationWarning
                    {
                        Field = $"Steps[{step.StepId}]",
                        WarningCode = "UNREACHABLE_STEP",
                        Message = $"Step {step.StepId} may be unreachable"
                    });
                }
            }

            // Check for dead-end steps (non-terminal steps with no outgoing transitions)
            var stepsWithOutgoingTransitions = schema.Transitions.Select(t => t.FromStepId).ToHashSet();
            foreach (var step in schema.Steps.Where(s => !s.IsTerminal))
            {
                if (!stepsWithOutgoingTransitions.Contains(step.StepId))
                {
                    result.Warnings.Add(new WorkflowValidationWarning
                    {
                        Field = $"Steps[{step.StepId}]",
                        WarningCode = "DEAD_END_STEP",
                        Message = $"Non-terminal step {step.StepId} has no outgoing transitions"
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