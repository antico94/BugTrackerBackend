// Services/WorkflowActionService.cs
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.DTOs;
using BugTracker.Models;
using BugTracker.Models.Enums;

namespace BugTracker.Services;

public interface IWorkflowActionService
{
    Task<WorkflowActionResultDto> ProcessActionAsync(WorkflowActionRequestDto request);
}

public class WorkflowActionService : IWorkflowActionService
{
    private readonly BugTrackerContext _context;
    private readonly IWorkflowEngineService _workflowEngine;

    public WorkflowActionService(BugTrackerContext context, IWorkflowEngineService workflowEngine)
    {
        _context = context;
        _workflowEngine = workflowEngine;
    }

    public async Task<WorkflowActionResultDto> ProcessActionAsync(WorkflowActionRequestDto request)
    {
        var result = new WorkflowActionResultDto();

        try
        {
            // Validate the request
            var validationResult = await ValidateActionRequestAsync(request);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.ErrorMessages.AddRange(validationResult.ErrorMessages);
                return result;
            }

            // Process the action based on type
            var processResult = request.ActionType switch
            {
                "complete" => await ProcessStepCompletionAsync(request),
                "decide_yes" => await ProcessDecisionAsync(request, "Yes"),
                "decide_no" => await ProcessDecisionAsync(request, "No"),
                "add_note" => await ProcessNoteAdditionAsync(request),
                _ => throw new InvalidOperationException($"Unknown action type: {request.ActionType}")
            };

            if (!processResult.Success)
            {
                result.Success = false;
                result.ErrorMessages.AddRange(processResult.ErrorMessages);
                return result;
            }

            // Save changes to database
            await _context.SaveChangesAsync();

            // Get updated workflow state
            result.NewWorkflowState = await _workflowEngine.GetWorkflowStateAsync(request.TaskId);
            result.Success = true;
            result.InfoMessages.AddRange(processResult.InfoMessages);

            // Suggest next action if applicable
            result.NextAction = DetermineNextAction(result.NewWorkflowState);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessages.Add($"An error occurred while processing the action: {ex.Message}");
            return result;
        }
    }

    private async Task<ActionValidationResult> ValidateActionRequestAsync(WorkflowActionRequestDto request)
    {
        var result = new ActionValidationResult();

        // Check if task exists
        var task = await _context.CustomTasks
            .Include(t => t.TaskSteps)
            .FirstOrDefaultAsync(t => t.CustomTaskId == request.TaskId);

        if (task == null)
        {
            result.ErrorMessages.Add($"Task with ID {request.TaskId} not found");
            return result;
        }

        // Check if task is already completed
        if (task.Status == Status.Done)
        {
            result.ErrorMessages.Add("Cannot perform actions on a completed task");
            return result;
        }

        // Get current workflow state to validate against
        var workflowState = await _workflowEngine.GetWorkflowStateAsync(request.TaskId);

        // Check if the requested action is available
        var availableAction = workflowState.AvailableActions
            .FirstOrDefault(a => a.ActionType == request.ActionType);

        if (availableAction == null)
        {
            result.ErrorMessages.Add($"Action '{request.ActionType}' is not available for the current step");
            return result;
        }

        if (!availableAction.IsEnabled)
        {
            result.ErrorMessages.Add($"Action '{request.ActionType}' is disabled: {availableAction.DisabledReason}");
            return result;
        }

        // Validate note requirements
        if (workflowState.ValidationRules.RequiresNote)
        {
            if (string.IsNullOrWhiteSpace(request.Note))
            {
                result.ErrorMessages.Add("A note is required for this step");
                return result;
            }

            if (request.Note.Length < workflowState.ValidationRules.MinNoteLength)
            {
                result.ErrorMessages.Add($"Note must be at least {workflowState.ValidationRules.MinNoteLength} characters long");
                return result;
            }

            if (request.Note.Length > workflowState.ValidationRules.MaxNoteLength)
            {
                result.ErrorMessages.Add($"Note cannot exceed {workflowState.ValidationRules.MaxNoteLength} characters");
                return result;
            }
        }

        result.IsValid = true;
        return result;
    }

    private async Task<ActionProcessResult> ProcessStepCompletionAsync(WorkflowActionRequestDto request)
    {
        var result = new ActionProcessResult();

        var task = await _context.CustomTasks
            .Include(t => t.TaskSteps)
            .FirstOrDefaultAsync(t => t.CustomTaskId == request.TaskId);

        if (task == null)
        {
            result.ErrorMessages.Add("Task not found");
            return result;
        }

        // Get current step
        var workflowState = await _workflowEngine.GetWorkflowStateAsync(request.TaskId);
        if (workflowState.CurrentStep == null)
        {
            result.ErrorMessages.Add("No current step found to complete");
            return result;
        }

        var currentStep = task.TaskSteps.FirstOrDefault(s => s.TaskStepId == workflowState.CurrentStep.TaskStepId);
        if (currentStep == null)
        {
            result.ErrorMessages.Add("Current step not found in database");
            return result;
        }

        // Validate that this is not a decision step
        if (currentStep.IsDecision)
        {
            result.ErrorMessages.Add("Cannot complete a decision step directly - use decide_yes or decide_no actions");
            return result;
        }

        // Complete the step
        currentStep.Status = Status.Done;
        currentStep.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            currentStep.Notes = request.Note;
        }

        // Update task status and completion
        await UpdateTaskStatusAfterStepCompletion(task);

        result.Success = true;
        result.InfoMessages.Add($"Step '{currentStep.Action}' completed successfully");

        if (currentStep.IsTerminal || task.Status == Status.Done)
        {
            result.InfoMessages.Add("Task has been completed");
        }

        return result;
    }

    private async Task<ActionProcessResult> ProcessDecisionAsync(WorkflowActionRequestDto request, string decision)
    {
        var result = new ActionProcessResult();

        var task = await _context.CustomTasks
            .Include(t => t.TaskSteps)
            .FirstOrDefaultAsync(t => t.CustomTaskId == request.TaskId);

        if (task == null)
        {
            result.ErrorMessages.Add("Task not found");
            return result;
        }

        // Get current step
        var workflowState = await _workflowEngine.GetWorkflowStateAsync(request.TaskId);
        if (workflowState.CurrentStep == null)
        {
            result.ErrorMessages.Add("No current step found to make decision on");
            return result;
        }

        var currentStep = task.TaskSteps.FirstOrDefault(s => s.TaskStepId == workflowState.CurrentStep.TaskStepId);
        if (currentStep == null)
        {
            result.ErrorMessages.Add("Current step not found in database");
            return result;
        }

        // Validate that this is a decision step
        if (!currentStep.IsDecision)
        {
            result.ErrorMessages.Add("Cannot make a decision on a non-decision step");
            return result;
        }

        // Validate decision paths exist
        var nextStepId = decision == "Yes" ? currentStep.NextStepIfYes : currentStep.NextStepIfNo;
        if (nextStepId.HasValue)
        {
            var nextStepExists = task.TaskSteps.Any(s => s.TaskStepId == nextStepId.Value);
            if (!nextStepExists)
            {
                result.WarningMessages.Add($"Next step for '{decision}' decision not found - workflow may end here");
            }
        }

        // Make the decision
        currentStep.Status = Status.Done;
        currentStep.CompletedAt = DateTime.UtcNow;
        currentStep.DecisionAnswer = decision;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            currentStep.Notes = request.Note;
        }

        // Update task status
        await UpdateTaskStatusAfterStepCompletion(task);

        result.Success = true;
        result.InfoMessages.Add($"Decision '{decision}' recorded for step '{currentStep.Action}'");

        return result;
    }

    private async Task<ActionProcessResult> ProcessNoteAdditionAsync(WorkflowActionRequestDto request)
    {
        var result = new ActionProcessResult();

        if (string.IsNullOrWhiteSpace(request.Note))
        {
            result.ErrorMessages.Add("Note content is required");
            return result;
        }

        var task = await _context.CustomTasks
            .FirstOrDefaultAsync(t => t.CustomTaskId == request.TaskId);

        if (task == null)
        {
            result.ErrorMessages.Add("Task not found");
            return result;
        }

        // Create new task note
        var taskNote = new TaskNote
        {
            TaskNoteId = Guid.NewGuid(),
            TaskId = request.TaskId,
            Content = request.Note,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "User" // Could be enhanced with actual user context
        };

        _context.TaskNotes.Add(taskNote);

        result.Success = true;
        result.InfoMessages.Add("Note added successfully");

        return result;
    }

    private async Task UpdateTaskStatusAfterStepCompletion(CustomTask task)
    {
        // Check if this was a terminal step
        var justCompletedStep = task.TaskSteps
            .Where(s => s.Status == Status.Done)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefault();

        if (justCompletedStep?.IsTerminal == true)
        {
            task.Status = Status.Done;
            task.CompletedAt = DateTime.UtcNow;
            return;
        }

        // Check if all steps in the current path are completed
        var workflowState = await _workflowEngine.GetWorkflowStateAsync(task.CustomTaskId);
        if (workflowState.CurrentStep == null)
        {
            // No current step means workflow is complete
            task.Status = Status.Done;
            task.CompletedAt = DateTime.UtcNow;
            return;
        }

        // Task is in progress if we have more steps to do
        if (task.Status == Status.New)
        {
            task.Status = Status.InProgress;
        }
    }

    private string? DetermineNextAction(WorkflowStateDto workflowState)
    {
        if (workflowState.IsTaskComplete)
        {
            return null;
        }

        if (workflowState.AvailableActions.Any())
        {
            var nextAction = workflowState.AvailableActions.First();
            return $"Consider: {nextAction.Label}";
        }

        return null;
    }
}

// Helper classes
internal class ActionValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();
}

internal class ActionProcessResult
{
    public bool Success { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();
    public List<string> InfoMessages { get; set; } = new List<string>();
    public List<string> WarningMessages { get; set; } = new List<string>();
}