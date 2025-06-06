// Services/WorkflowEngineService.cs
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.DTOs;
using BugTracker.Models;
using BugTracker.Models.Enums;

namespace BugTracker.Services;

public interface IWorkflowEngineService
{
    Task<WorkflowStateDto> GetWorkflowStateAsync(Guid taskId);
    Task<WorkflowHistoryDto> GetWorkflowHistoryAsync(Guid taskId);
}

public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly BugTrackerContext _context;

    public WorkflowEngineService(BugTrackerContext context)
    {
        _context = context;
    }

    public async Task<WorkflowStateDto> GetWorkflowStateAsync(Guid taskId)
    {
        var task = await GetTaskWithStepsAsync(taskId);
        if (task == null)
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        var currentStep = CalculateCurrentStep(task);
        var availableActions = CalculateAvailableActions(currentStep, task);
        var progressInfo = CalculateProgressInfo(task);
        var validationRules = CalculateValidationRules(currentStep);
        var completedSteps = GetCompletedStepsSummary(task);
        var upcomingSteps = GetUpcomingStepsSummary(task, currentStep);
        var uiHints = CalculateUIHints(currentStep, task);

        return new WorkflowStateDto
        {
            TaskId = taskId,
            TaskStatus = task.Status,
            CurrentStep = currentStep?.ToTaskStepDto(),
            AvailableActions = availableActions,
            ProgressInfo = progressInfo,
            ValidationRules = validationRules,
            CompletedSteps = completedSteps,
            UpcomingSteps = upcomingSteps,
            IsTaskComplete = task.Status == Status.Done,
            UIHints = uiHints
        };
    }

    public async Task<WorkflowHistoryDto> GetWorkflowHistoryAsync(Guid taskId)
    {
        var task = await GetTaskWithStepsAsync(taskId);
        if (task == null)
        {
            throw new ArgumentException($"Task with ID {taskId} not found", nameof(taskId));
        }

        var events = GenerateWorkflowEvents(task);
        var totalDuration = task.CompletedAt?.Subtract(task.CreatedAt) ?? DateTime.UtcNow.Subtract(task.CreatedAt);

        return new WorkflowHistoryDto
        {
            Events = events,
            TotalDuration = totalDuration,
            StartedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt
        };
    }

    private async Task<CustomTask?> GetTaskWithStepsAsync(Guid taskId)
    {
        return await _context.CustomTasks
            .Include(t => t.TaskSteps.OrderBy(ts => ts.Order))
            .Include(t => t.TaskNotes)
            .Include(t => t.CoreBug)
            .Include(t => t.Study)
            .Include(t => t.TrialManager)
            .Include(t => t.InteractiveResponseTechnology)
            .FirstOrDefaultAsync(t => t.CustomTaskId == taskId);
    }

    private TaskStep? CalculateCurrentStep(CustomTask task)
    {
        var taskSteps = task.TaskSteps.OrderBy(ts => ts.Order).ToList();
        
        if (!taskSteps.Any())
        {
            return null;
        }

        // Start from the first step and follow the decision tree path
        var firstStep = taskSteps.First();
        return FollowDecisionTreePath(firstStep, taskSteps);
    }

    private TaskStep? FollowDecisionTreePath(TaskStep currentStep, List<TaskStep> allSteps)
    {
        // If current step is not completed, this is the current step
        if (currentStep.Status != Status.Done)
        {
            return currentStep;
        }

        // If current step is terminal and completed, workflow is done
        if (currentStep.IsTerminal && currentStep.Status == Status.Done)
        {
            return null;
        }

        TaskStep? nextStep = null;

        // Handle decision steps
        if (currentStep.IsDecision && currentStep.Status == Status.Done)
        {
            if (currentStep.DecisionAnswer == "Yes" && currentStep.NextStepIfYes.HasValue)
            {
                nextStep = allSteps.FirstOrDefault(s => s.TaskStepId == currentStep.NextStepIfYes.Value);
            }
            else if (currentStep.DecisionAnswer == "No" && currentStep.NextStepIfNo.HasValue)
            {
                nextStep = allSteps.FirstOrDefault(s => s.TaskStepId == currentStep.NextStepIfNo.Value);
            }
        }
        // Handle auto-check steps
        else if (currentStep.IsAutoCheck && currentStep.Status == Status.Done)
        {
            if (currentStep.AutoCheckResult == true && currentStep.NextStepIfTrue.HasValue)
            {
                nextStep = allSteps.FirstOrDefault(s => s.TaskStepId == currentStep.NextStepIfTrue.Value);
            }
            else if (currentStep.AutoCheckResult == false && currentStep.NextStepIfFalse.HasValue)
            {
                nextStep = allSteps.FirstOrDefault(s => s.TaskStepId == currentStep.NextStepIfFalse.Value);
            }
        }
        // Handle regular steps - follow order
        else if (currentStep.Status == Status.Done)
        {
            nextStep = allSteps.FirstOrDefault(s => s.Order > currentStep.Order);
        }

        // If we found a next step, recursively follow the path
        if (nextStep != null)
        {
            return FollowDecisionTreePath(nextStep, allSteps);
        }

        // No valid next step found - workflow might be complete or broken
        return null;
    }

    private List<WorkflowActionDto> CalculateAvailableActions(TaskStep? currentStep, CustomTask task)
    {
        var actions = new List<WorkflowActionDto>();

        // If task is complete or no current step, no actions available
        if (task.Status == Status.Done || currentStep == null)
        {
            return actions;
        }

        // If step is already completed, no actions available
        if (currentStep.Status == Status.Done)
        {
            return actions;
        }

        // Auto-check steps have no user actions
        if (currentStep.IsAutoCheck)
        {
            return actions;
        }

        // Decision steps get Yes/No actions
        if (currentStep.IsDecision)
        {
            actions.Add(new WorkflowActionDto
            {
                ActionType = "decide_yes",
                Label = "Yes",
                ButtonVariant = "workflow-decision",
                IsEnabled = true,
                Description = "Choose 'Yes' for this decision"
            });

            actions.Add(new WorkflowActionDto
            {
                ActionType = "decide_no",
                Label = "No",
                ButtonVariant = "workflow-decision", 
                IsEnabled = true,
                Description = "Choose 'No' for this decision"
            });
        }
        // Regular steps get complete action
        else
        {
            actions.Add(new WorkflowActionDto
            {
                ActionType = "complete",
                Label = currentStep.IsTerminal ? "Complete Task" : "Complete Step",
                ButtonVariant = "workflow-action",
                IsEnabled = true,
                Description = $"Mark '{currentStep.Action}' as completed"
            });
        }

        return actions;
    }

    private WorkflowProgressDto CalculateProgressInfo(CustomTask task)
    {
        var allSteps = task.TaskSteps.ToList();
        var completedSteps = allSteps.Count(s => s.Status == Status.Done);
        var totalSteps = allSteps.Count;

        var percentComplete = totalSteps > 0 ? (double)completedSteps / totalSteps * 100 : 0;
        var statusText = task.Status == Status.Done 
            ? "Complete" 
            : $"Step {completedSteps + 1} of {totalSteps}";

        return new WorkflowProgressDto
        {
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps,
            PercentComplete = Math.Round(percentComplete, 1),
            StatusText = statusText,
            IsInProgress = task.Status == Status.InProgress
        };
    }

    private WorkflowValidationDto CalculateValidationRules(TaskStep? currentStep)
    {
        var validation = new WorkflowValidationDto();

        if (currentStep == null)
        {
            return validation;
        }

        // Check if step requires notes
        validation.RequiresNote = currentStep.RequiresNote;

        if (validation.RequiresNote)
        {
            validation.MinNoteLength = 10; // Configurable
            validation.MaxNoteLength = 2000; // Configurable
            validation.NotePrompt = GetNotePromptForStep(currentStep);
            validation.Requirements.Add($"Please provide at least {validation.MinNoteLength} characters explaining your action");
        }

        // Add step-specific validation messages
        if (currentStep.IsDecision)
        {
            validation.ValidationMessages.Add("This step requires a Yes or No decision");
        }

        if (currentStep.IsTerminal)
        {
            validation.ValidationMessages.Add("This is the final step - completing it will finish the task");
        }

        return validation;
    }

    private string GetNotePromptForStep(TaskStep step)
    {
        return step.Action.ToLower() switch
        {
            var action when action.Contains("clone") && action.Contains("jira") 
                => "Provide the JIRA ticket key and any relevant details about the cloning process",
            var action when action.Contains("test") && action.Contains("reproduction") 
                => "Describe the reproduction steps attempted and the results",
            var action when action.Contains("severity") 
                => "Explain the severity assessment and reasoning",
            var action when action.Contains("version") && action.Contains("impact")
                => "Detail which versions are affected and the impact assessment",
            _ => $"Please provide details about completing: {step.Action}"
        };
    }

    private List<StepSummaryDto> GetCompletedStepsSummary(CustomTask task)
    {
        return task.TaskSteps
            .Where(s => s.Status == Status.Done)
            .OrderBy(s => s.Order)
            .Select(s => new StepSummaryDto
            {
                TaskStepId = s.TaskStepId,
                Action = s.Action,
                Description = s.Description,
                Order = s.Order,
                IsDecision = s.IsDecision,
                IsTerminal = s.IsTerminal,
                Status = s.Status,
                CompletedAt = s.CompletedAt,
                DecisionAnswer = s.DecisionAnswer,
                Notes = s.Notes,
                StatusIcon = "✓",
                StatusColor = "green"
            })
            .ToList();
    }

    private List<StepSummaryDto> GetUpcomingStepsSummary(CustomTask task, TaskStep? currentStep)
    {
        if (currentStep == null || task.Status == Status.Done)
        {
            return new List<StepSummaryDto>();
        }

        // Get next few steps in order (simplified - could be enhanced with decision tree logic)
        return task.TaskSteps
            .Where(s => s.Order > currentStep.Order && s.Status != Status.Done)
            .OrderBy(s => s.Order)
            .Take(3) // Show next 3 steps
            .Select(s => new StepSummaryDto
            {
                TaskStepId = s.TaskStepId,
                Action = s.Action,
                Description = s.Description,
                Order = s.Order,
                IsDecision = s.IsDecision,
                IsTerminal = s.IsTerminal,
                Status = s.Status,
                StatusIcon = "○",
                StatusColor = "gray"
            })
            .ToList();
    }

    private WorkflowUIHintsDto CalculateUIHints(TaskStep? currentStep, CustomTask task)
    {
        if (currentStep == null || task.Status == Status.Done)
        {
            return new WorkflowUIHintsDto
            {
                CurrentStepType = "complete",
                ThemeColor = "green",
                ShowProgressBar = true,
                ShowStepHistory = true,
                ShowUpcomingSteps = false,
                NextStepPreview = "Task completed"
            };
        }

        var stepType = currentStep.IsDecision ? "decision" : 
                      currentStep.IsTerminal ? "terminal" : "action";

        var themeColor = stepType switch
        {
            "decision" => "cyan",
            "terminal" => "orange", 
            _ => "blue"
        };

        return new WorkflowUIHintsDto
        {
            CurrentStepType = stepType,
            ThemeColor = themeColor,
            ShowProgressBar = true,
            ShowStepHistory = true,
            ShowUpcomingSteps = !currentStep.IsTerminal,
            NextStepPreview = GetNextStepPreview(currentStep, task)
        };
    }

    private string GetNextStepPreview(TaskStep currentStep, CustomTask task)
    {
        if (currentStep.IsTerminal)
        {
            return "This will complete the task";
        }

        if (currentStep.IsDecision)
        {
            return "Your decision will determine the next step";
        }

        var nextStep = task.TaskSteps
            .Where(s => s.Order > currentStep.Order)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        return nextStep != null ? $"Next: {nextStep.Action}" : "This will complete the task";
    }

    private List<WorkflowEventDto> GenerateWorkflowEvents(CustomTask task)
    {
        var events = new List<WorkflowEventDto>();

        // Task creation event
        events.Add(new WorkflowEventDto
        {
            EventId = Guid.NewGuid(),
            EventType = "task_created",
            Timestamp = task.CreatedAt,
            Description = "Task created",
            UserName = "System"
        });

        // Step completion events
        foreach (var step in task.TaskSteps.Where(s => s.Status == Status.Done).OrderBy(s => s.CompletedAt))
        {
            events.Add(new WorkflowEventDto
            {
                EventId = Guid.NewGuid(),
                EventType = step.IsDecision ? "decision_made" : "step_completed",
                Timestamp = step.CompletedAt ?? task.CreatedAt,
                Description = step.Action,
                StepName = step.Action,
                Decision = step.DecisionAnswer,
                Notes = step.Notes,
                UserName = "User" // Could be enhanced with actual user tracking
            });
        }

        // Task completion event
        if (task.Status == Status.Done && task.CompletedAt.HasValue)
        {
            events.Add(new WorkflowEventDto
            {
                EventId = Guid.NewGuid(),
                EventType = "task_completed",
                Timestamp = task.CompletedAt.Value,
                Description = "Task completed",
                UserName = "System"
            });
        }

        return events.OrderBy(e => e.Timestamp).ToList();
    }
}

// Extension method to convert TaskStep to TaskStepDto
public static class TaskStepExtensions
{
    public static TaskStepDto ToTaskStepDto(this TaskStep step)
    {
        return new TaskStepDto
        {
            TaskStepId = step.TaskStepId,
            Action = step.Action,
            Description = step.Description,
            Order = step.Order,
            IsDecision = step.IsDecision,
            IsAutoCheck = step.IsAutoCheck,
            IsTerminal = step.IsTerminal,
            RequiresNote = step.RequiresNote,
            Status = step.Status,
            CompletedAt = step.CompletedAt,
            DecisionAnswer = step.DecisionAnswer,
            Notes = step.Notes,
            AutoCheckResult = step.AutoCheckResult
        };
    }
}