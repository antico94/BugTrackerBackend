using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models.Workflow;
using System.Text.Json;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Core workflow engine implementation
/// </summary>
public class WorkflowEngineService : IWorkflowEngine
{
    private readonly BugTrackerContext _context;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly IWorkflowRuleEngine _ruleEngine;
    private readonly ILogger<WorkflowEngineService> _logger;

    public WorkflowEngineService(
        BugTrackerContext context,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowExecutionService workflowExecutionService,
        IWorkflowRuleEngine ruleEngine,
        ILogger<WorkflowEngineService> logger)
    {
        _context = context;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowExecutionService = workflowExecutionService;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    public async Task<WorkflowState> GetWorkflowStateAsync(Guid taskId)
    {
        var execution = await _workflowExecutionService.GetWorkflowExecutionAsync(taskId);
        if (execution == null)
        {
            throw new InvalidOperationException($"No workflow execution found for task {taskId}");
        }

        var workflowDefinition = await _workflowDefinitionService.GetWorkflowDefinitionAsync(execution.WorkflowDefinition.Name);
        if (workflowDefinition == null)
        {
            throw new InvalidOperationException($"Workflow definition not found: {execution.WorkflowDefinition.Name}");
        }

        var schema = workflowDefinition.GetWorkflowSchema();
        var context = string.IsNullOrEmpty(execution.ContextJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(execution.ContextJson) ?? new Dictionary<string, object>();

        // Build the current workflow state
        var state = new WorkflowState
        {
            TaskId = taskId,
            WorkflowExecutionId = execution.WorkflowExecutionId,
            WorkflowName = schema.Name,
            WorkflowVersion = workflowDefinition.Version,
            Status = execution.Status,
            Context = context,
            GeneratedAt = DateTime.UtcNow
        };

        // Get current step information
        var currentStepDef = schema.Steps.FirstOrDefault(s => s.StepId == execution.CurrentStepId);
        if (currentStepDef != null && execution.Status == WorkflowExecutionStatus.Active)
        {
            state.CurrentStep = new WorkflowCurrentStep
            {
                StepId = currentStepDef.StepId,
                Name = currentStepDef.Name,
                Description = currentStepDef.Description,
                Type = currentStepDef.Type,
                IsTerminal = currentStepDef.IsTerminal,
                RequiresNote = currentStepDef.Config.RequiresNote,
                AutoExecute = currentStepDef.Config.AutoExecute,
                Metadata = currentStepDef.Metadata
            };

            // Get available actions for current step
            state.AvailableActions = await GetAvailableActionsAsync(currentStepDef, context);

            // Get validation rules for current step
            state.ValidationRules = GetValidationRulesState(currentStepDef.Config.ValidationRules);

            // Get possible next steps (for decision previews)
            state.PossibleNextSteps = await GetPossibleNextStepsAsync(schema, currentStepDef, context);

            // Generate UI hints
            state.UIHints = GenerateUIHints(currentStepDef, state.PossibleNextSteps);
        }

        // Get completed steps
        state.CompletedSteps = await GetCompletedStepsAsync(execution.WorkflowExecutionId);

        // Calculate progress
        state.Progress = CalculateProgress(state.CompletedSteps, schema.Steps.Count, execution.Status);

        return state;
    }

    public async Task<WorkflowActionResult> ExecuteActionAsync(Guid taskId, WorkflowActionRequest request)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var execution = await _workflowExecutionService.GetWorkflowExecutionAsync(taskId);
            if (execution == null)
            {
                return new WorkflowActionResult
                {
                    Success = false,
                    Message = $"No workflow execution found for task {taskId}",
                    ErrorCode = "WORKFLOW_NOT_FOUND"
                };
            }

            if (execution.Status != WorkflowExecutionStatus.Active)
            {
                return new WorkflowActionResult
                {
                    Success = false,
                    Message = $"Workflow is not active. Current status: {execution.Status}",
                    ErrorCode = "WORKFLOW_NOT_ACTIVE"
                };
            }

            // Validate the action can be performed
            var validationResult = await ValidateActionAsync(taskId, request);
            if (!validationResult.IsValid)
            {
                return new WorkflowActionResult
                {
                    Success = false,
                    Message = string.Join("; ", validationResult.Errors.Select(e => e.Message)),
                    ErrorCode = "VALIDATION_FAILED"
                };
            }

            var workflowDefinition = await _workflowDefinitionService.GetWorkflowDefinitionAsync(execution.WorkflowDefinition.Name);
            var schema = workflowDefinition!.GetWorkflowSchema();
            var currentStepDef = schema.Steps.First(s => s.StepId == execution.CurrentStepId);
            
            var context = string.IsNullOrEmpty(execution.ContextJson) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(execution.ContextJson) ?? new Dictionary<string, object>();

            // Update context with any additional data from the request
            if (request.AdditionalData != null)
            {
                foreach (var kvp in request.AdditionalData)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }

            // Store the decision or notes in context
            if (!string.IsNullOrEmpty(request.Decision))
            {
                context[$"step_{currentStepDef.StepId}_decision"] = request.Decision;
            }
            
            if (!string.IsNullOrEmpty(request.Notes))
            {
                context[$"step_{currentStepDef.StepId}_notes"] = request.Notes;
            }

            // Determine the next step
            var nextStepId = await DetermineNextStepAsync(schema, currentStepDef, request, context);
            
            // Create audit log entry
            var auditLog = new WorkflowAuditLog
            {
                WorkflowAuditLogId = Guid.NewGuid(),
                WorkflowExecutionId = execution.WorkflowExecutionId,
                StepId = currentStepDef.StepId,
                Action = request.ActionId,
                Result = "Success",
                PreviousStepId = execution.CurrentStepId,
                NextStepId = nextStepId,
                Decision = request.Decision,
                Notes = request.Notes,
                ContextSnapshot = JsonSerializer.Serialize(context),
                Timestamp = startTime,
                PerformedBy = request.PerformedBy,
                DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };

            await _workflowExecutionService.AddAuditLogAsync(auditLog);

            // Update the workflow execution
            bool workflowCompleted = false;
            if (nextStepId == null || currentStepDef.IsTerminal)
            {
                // Workflow is complete
                await _workflowExecutionService.CompleteWorkflowExecutionAsync(execution.WorkflowExecutionId);
                workflowCompleted = true;
            }
            else
            {
                // Move to next step
                await _workflowExecutionService.UpdateWorkflowExecutionStepAsync(execution.WorkflowExecutionId, nextStepId, context);
            }

            // Get the new workflow state
            var newState = await GetWorkflowStateAsync(taskId);

            return new WorkflowActionResult
            {
                Success = true,
                Message = workflowCompleted ? "Workflow completed successfully" : "Action executed successfully",
                PreviousStepId = currentStepDef.StepId,
                NewStepId = nextStepId,
                WorkflowCompleted = workflowCompleted,
                NewState = newState
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow action for task {TaskId}", taskId);
            
            return new WorkflowActionResult
            {
                Success = false,
                Message = "An error occurred while executing the workflow action",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }

    public async Task<WorkflowExecution> StartWorkflowAsync(Guid taskId, string workflowDefinitionName, Dictionary<string, object>? initialContext = null)
    {
        var workflowDefinition = await _workflowDefinitionService.GetWorkflowDefinitionAsync(workflowDefinitionName);
        if (workflowDefinition == null)
        {
            throw new ArgumentException($"Workflow definition not found: {workflowDefinitionName}");
        }

        var schema = workflowDefinition.GetWorkflowSchema();
        if (string.IsNullOrEmpty(schema.InitialStepId))
        {
            throw new InvalidOperationException($"Workflow definition {workflowDefinitionName} has no initial step defined");
        }

        var execution = await _workflowExecutionService.CreateWorkflowExecutionAsync(
            taskId, 
            workflowDefinition.WorkflowDefinitionId, 
            schema.InitialStepId, 
            initialContext);

        _logger.LogInformation("Started workflow {WorkflowName} for task {TaskId}", workflowDefinitionName, taskId);

        return execution;
    }

    public async Task<List<WorkflowAuditLog>> GetAuditTrailAsync(Guid taskId)
    {
        var execution = await _workflowExecutionService.GetWorkflowExecutionAsync(taskId);
        if (execution == null)
        {
            return new List<WorkflowAuditLog>();
        }

        return await _context.WorkflowAuditLogs
            .Where(log => log.WorkflowExecutionId == execution.WorkflowExecutionId)
            .OrderBy(log => log.Timestamp)
            .ToListAsync();
    }

    public async Task<WorkflowValidationResult> ValidateActionAsync(Guid taskId, WorkflowActionRequest request)
    {
        var result = new WorkflowValidationResult();

        try
        {
            var execution = await _workflowExecutionService.GetWorkflowExecutionAsync(taskId);
            if (execution == null)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "TaskId",
                    ErrorCode = "WORKFLOW_NOT_FOUND",
                    Message = $"No workflow execution found for task {taskId}"
                });
                return result;
            }

            var workflowDefinition = await _workflowDefinitionService.GetWorkflowDefinitionAsync(execution.WorkflowDefinition.Name);
            var schema = workflowDefinition!.GetWorkflowSchema();
            var currentStepDef = schema.Steps.FirstOrDefault(s => s.StepId == execution.CurrentStepId);

            if (currentStepDef == null)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "CurrentStep",
                    ErrorCode = "STEP_NOT_FOUND",
                    Message = $"Current step {execution.CurrentStepId} not found in workflow definition"
                });
                return result;
            }

            // Validate the action exists for this step
            var actionDef = currentStepDef.Actions.FirstOrDefault(a => a.ActionId == request.ActionId);
            if (actionDef == null)
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "ActionId",
                    ErrorCode = "ACTION_NOT_ALLOWED",
                    Message = $"Action {request.ActionId} is not allowed for step {currentStepDef.Name}"
                });
                return result;
            }

            // Validate required fields based on step configuration
            if (currentStepDef.Config.RequiresNote && string.IsNullOrWhiteSpace(request.Notes))
            {
                result.IsValid = false;
                result.Errors.Add(new WorkflowValidationError
                {
                    Field = "Notes",
                    ErrorCode = "NOTES_REQUIRED",
                    Message = "Notes are required for this step"
                });
            }

            // Validate decision steps
            if (currentStepDef.Type == WorkflowStepType.Decision && actionDef.Type == WorkflowActionType.Decide)
            {
                if (string.IsNullOrWhiteSpace(request.Decision))
                {
                    result.IsValid = false;
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = "Decision",
                        ErrorCode = "DECISION_REQUIRED",
                        Message = "Decision is required for decision steps"
                    });
                }
                else if (request.Decision != "Yes" && request.Decision != "No")
                {
                    result.IsValid = false;
                    result.Errors.Add(new WorkflowValidationError
                    {
                        Field = "Decision",
                        ErrorCode = "INVALID_DECISION",
                        Message = "Decision must be 'Yes' or 'No'"
                    });
                }
            }

            // Validate custom validation rules
            if (currentStepDef.Config.ValidationRules.Any())
            {
                var input = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(request.Notes)) input["notes"] = request.Notes;
                if (!string.IsNullOrEmpty(request.Decision)) input["decision"] = request.Decision;
                if (request.AdditionalData != null)
                {
                    foreach (var kvp in request.AdditionalData)
                    {
                        input[kvp.Key] = kvp.Value;
                    }
                }

                var customValidationResult = await _ruleEngine.ValidateInputAsync(currentStepDef.Config.ValidationRules, input);
                if (!customValidationResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(customValidationResult.Errors);
                    result.Warnings.AddRange(customValidationResult.Warnings);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow action for task {TaskId}", taskId);
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

    private async Task<List<WorkflowAvailableAction>> GetAvailableActionsAsync(WorkflowStepDefinition stepDef, Dictionary<string, object> context)
    {
        var actions = new List<WorkflowAvailableAction>();

        foreach (var actionDef in stepDef.Actions.Where(a => a.IsEnabled))
        {
            var action = new WorkflowAvailableAction
            {
                ActionId = actionDef.ActionId,
                Name = actionDef.Name,
                Label = actionDef.Label,
                Type = actionDef.Type,
                IsEnabled = true,
                Description = actionDef.Description,
                Metadata = actionDef.Metadata
            };

            // Set UI properties based on action type
            switch (actionDef.Type)
            {
                case WorkflowActionType.Complete:
                    action.ButtonVariant = stepDef.IsTerminal ? "workflow-terminal" : "workflow-action";
                    action.GlowColor = stepDef.IsTerminal ? "orange" : "emerald";
                    break;
                case WorkflowActionType.Decide:
                    action.ButtonVariant = "workflow-decision";
                    action.GlowColor = actionDef.Name.Contains("Yes") ? "green" : "red";
                    break;
                default:
                    action.ButtonVariant = "default";
                    action.GlowColor = "blue";
                    break;
            }

            actions.Add(action);
        }

        return actions;
    }

    private List<WorkflowValidationState> GetValidationRulesState(List<WorkflowValidationRule> rules)
    {
        return rules.Select(rule => new WorkflowValidationState
        {
            RuleId = rule.RuleId,
            Field = rule.Field,
            Type = rule.Type,
            IsRequired = rule.Type == WorkflowValidationType.Required,
            Value = rule.Value,
            ErrorMessage = rule.ErrorMessage,
            IsValid = true // Will be validated when action is attempted
        }).ToList();
    }

    private async Task<List<WorkflowNextStep>> GetPossibleNextStepsAsync(WorkflowSchema schema, WorkflowStepDefinition currentStep, Dictionary<string, object> context)
    {
        var nextSteps = new List<WorkflowNextStep>();

        if (currentStep.IsTerminal)
            return nextSteps;

        var transitions = schema.Transitions.Where(t => t.FromStepId == currentStep.StepId).ToList();

        foreach (var transition in transitions)
        {
            var nextStepDef = schema.Steps.FirstOrDefault(s => s.StepId == transition.ToStepId);
            if (nextStepDef == null) continue;

            var condition = transition.TriggerAction == "decide_yes" ? "Yes" :
                           transition.TriggerAction == "decide_no" ? "No" :
                           transition.TriggerAction;

            nextSteps.Add(new WorkflowNextStep
            {
                StepId = nextStepDef.StepId,
                Name = nextStepDef.Name,
                Description = nextStepDef.Description,
                Type = nextStepDef.Type,
                IsTerminal = nextStepDef.IsTerminal,
                Condition = condition,
                PreviewText = nextStepDef.IsTerminal ? "This will complete the workflow" : $"Next: {nextStepDef.Name}"
            });
        }

        return nextSteps;
    }

    private WorkflowUIHints GenerateUIHints(WorkflowStepDefinition currentStep, List<WorkflowNextStep> possibleNextSteps)
    {
        var stepType = currentStep.Type switch
        {
            WorkflowStepType.Decision => "decision",
            WorkflowStepType.Terminal => "terminal",
            WorkflowStepType.AutoCheck => "autocheck",
            _ => "action"
        };

        var themeColor = stepType switch
        {
            "decision" => "cyan",
            "terminal" => "orange",
            "autocheck" => "purple",
            _ => "blue"
        };

        return new WorkflowUIHints
        {
            CurrentStepType = stepType,
            ThemeColor = themeColor,
            ShowProgressBar = true,
            ShowStepHistory = true,
            ShowUpcomingSteps = !currentStep.IsTerminal,
            NextStepPreview = currentStep.IsTerminal 
                ? "This will complete the workflow" 
                : possibleNextSteps.FirstOrDefault()?.PreviewText ?? "Processing next step...",
            CustomHints = currentStep.Metadata
        };
    }

    private async Task<List<WorkflowCompletedStep>> GetCompletedStepsAsync(Guid workflowExecutionId)
    {
        var auditLogs = await _context.WorkflowAuditLogs
            .Where(log => log.WorkflowExecutionId == workflowExecutionId && log.Result == "Success")
            .OrderBy(log => log.Timestamp)
            .ToListAsync();

        return auditLogs.Select(log => new WorkflowCompletedStep
        {
            StepId = log.StepId,
            Name = log.Action, // We might want to enhance this with step names from definition
            Description = log.Action,
            Type = WorkflowStepType.Action, // We might want to store this in audit log
            CompletedAt = log.Timestamp,
            Decision = log.Decision,
            Notes = log.Notes,
            CompletedBy = log.PerformedBy,
            DurationMs = log.DurationMs
        }).ToList();
    }

    private WorkflowProgress CalculateProgress(List<WorkflowCompletedStep> completedSteps, int totalSteps, WorkflowExecutionStatus status)
    {
        var completed = completedSteps.Count;
        var percentComplete = totalSteps > 0 ? (double)completed / totalSteps * 100 : 0;

        var statusText = status switch
        {
            WorkflowExecutionStatus.Completed => "Complete",
            WorkflowExecutionStatus.Failed => "Failed",
            WorkflowExecutionStatus.Cancelled => "Cancelled",
            WorkflowExecutionStatus.Suspended => "Suspended",
            _ => $"Step {completed + 1} of {totalSteps}"
        };

        return new WorkflowProgress
        {
            CompletedSteps = completed,
            TotalSteps = totalSteps,
            PercentComplete = Math.Round(percentComplete, 1),
            StatusText = statusText,
            IsInProgress = status == WorkflowExecutionStatus.Active
        };
    }

    private async Task<string?> DetermineNextStepAsync(WorkflowSchema schema, WorkflowStepDefinition currentStep, WorkflowActionRequest request, Dictionary<string, object> context)
    {
        if (currentStep.IsTerminal)
            return null;

        // Find applicable transitions
        var transitions = schema.Transitions.Where(t => t.FromStepId == currentStep.StepId).ToList();

        foreach (var transition in transitions)
        {
            // Check if trigger action matches
            bool triggerMatches = transition.TriggerAction switch
            {
                "complete" => request.ActionId == "complete",
                "decide_yes" => request.ActionId == "decide" && request.Decision == "Yes",
                "decide_no" => request.ActionId == "decide" && request.Decision == "No",
                _ => transition.TriggerAction == request.ActionId
            };

            if (!triggerMatches) continue;

            // Evaluate transition conditions
            bool conditionsMet = true;
            if (transition.Conditions.Any())
            {
                conditionsMet = await _ruleEngine.EvaluateConditionsAsync(transition.Conditions, context);
            }

            if (conditionsMet)
            {
                return transition.ToStepId;
            }
        }

        // No valid transition found
        _logger.LogWarning("No valid transition found for step {StepId} with action {ActionId}", currentStep.StepId, request.ActionId);
        return null;
    }
}