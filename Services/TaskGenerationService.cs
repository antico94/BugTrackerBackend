// Services/TaskGenerationService.cs
using BugTracker.Models;
using BugTracker.Models.Enums;
using BugTracker.DTOs;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;

namespace BugTracker.Services;

public class TaskGenerationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskGenerationService> _logger;
    
    public TaskGenerationService(IServiceProvider serviceProvider, ILogger<TaskGenerationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task<List<CustomTask>> GenerateTasksForAssessedBug(CoreBug assessedBug)
    {
        var tasks = new List<CustomTask>();
        
        if (!assessedBug.IsAssessed || assessedBug.AssessedProductType == null)
            return tasks;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BugTrackerContext>();
        
        // Determine which versions to check against
        var versionsToCheck = GetVersionsToCheck(assessedBug);
        
        // Generate tasks based on product type
        switch (assessedBug.AssessedProductType)
        {
            case ProductType.TM:
                tasks = await GenerateTrialManagerTasks(context, assessedBug, versionsToCheck);
                break;
                
            case ProductType.InteractiveResponseTechnology:
            case ProductType.ExternalModule: // ExternalModule also generates IRT tasks
                tasks = await GenerateIRTTasks(context, assessedBug, versionsToCheck);
                break;
        }
        
        return tasks;
    }
    
    private List<string> GetVersionsToCheck(CoreBug bug)
    {
        // For manually added bugs, use AffectedVersions
        // For XML imported bugs (which would have been assessed), use AssessedImpactedVersions
        if (!string.IsNullOrEmpty(bug.AssessedImpactedVersions))
        {
            return JsonSerializer.Deserialize<List<string>>(bug.AssessedImpactedVersions) ?? new List<string>();
        }
        else if (!string.IsNullOrEmpty(bug.AffectedVersions))
        {
            return JsonSerializer.Deserialize<List<string>>(bug.AffectedVersions) ?? new List<string>();
        }
        
        return new List<string>();
    }
    
    private async Task<List<CustomTask>> GenerateTrialManagerTasks(BugTrackerContext context, CoreBug bug, List<string> versionsToCheck)
    {
        var tasks = new List<CustomTask>();
        
        // Get all Trial Managers
        var trialManagers = await context.TrialManagers
            .Include(tm => tm.Client)
            .Include(tm => tm.Studies)
            .ToListAsync();
        
        foreach (var tm in trialManagers)
        {
            // Create task for each TM
            var task = new CustomTask
            {
                TaskId = Guid.NewGuid(),
                BugId = bug.BugId,
                TrialManagerId = tm.TrialManagerId,
                StudyId = tm.Studies.FirstOrDefault()?.StudyId, // Use first study if available
                TaskTitle = $"{bug.JiraKey} - {tm.Protocol}",
                TaskDescription = $"Assess impact of bug {bug.JiraKey} on Trial Manager {tm.Client?.Name ?? "Unknown"} v{tm.Version}",
                JiraTaskKey = "", // Will be filled if bug is cloned
                JiraTaskLink = "",
                Status = Status.New,
                CreatedAt = DateTime.UtcNow
            };
            
            // Generate steps for this task
            var steps = GenerateTaskSteps(task.TaskId, tm.Version, versionsToCheck, bug.Severity);
            task.TaskSteps = steps;
            
            // Check if task should be auto-completed
            CheckAndAutoCompleteTask(task);
            
            tasks.Add(task);
        }
        
        return tasks;
    }
    
    private async Task<List<CustomTask>> GenerateIRTTasks(BugTrackerContext context, CoreBug bug, List<string> versionsToCheck)
    {
        var tasks = new List<CustomTask>();
        
        // Get all IRTs
        var irts = await context.InteractiveResponseTechnologies
            .Include(irt => irt.Study)
                .ThenInclude(s => s.Client)
            .Include(irt => irt.TrialManager)
            .ToListAsync();
        
        foreach (var irt in irts)
        {
            // Create task for each IRT
            var task = new CustomTask
            {
                TaskId = Guid.NewGuid(),
                BugId = bug.BugId,
                InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                StudyId = irt.StudyId,
                TaskTitle = $"{bug.JiraKey} - {irt.Protocol}",
                TaskDescription = $"Assess impact of bug {bug.JiraKey} on IRT {irt.Study?.Name ?? "Unknown"} v{irt.Version}",
                JiraTaskKey = "", // Will be filled if bug is cloned
                JiraTaskLink = "",
                Status = Status.New,
                CreatedAt = DateTime.UtcNow
            };
            
            // Generate steps for this task
            var steps = GenerateTaskSteps(task.TaskId, irt.Version, versionsToCheck, bug.Severity);
            task.TaskSteps = steps;
            
            // Check if task should be auto-completed
            CheckAndAutoCompleteTask(task);
            
            tasks.Add(task);
        }
        
        return tasks;
    }
    
    private List<TaskStep> GenerateTaskSteps(Guid taskId, string productVersion, List<string> affectedVersions, BugSeverity bugSeverity)
    {
        var steps = new List<TaskStep>();
        
        // Step 1: Is version affected? (AUTO)
        var step1 = new TaskStep
        {
            TaskStepId = Guid.NewGuid(),
            TaskId = taskId,
            Action = "Check Version Impact",
            Description = "Is the version of our product affected by the bug?",
            Order = 1,
            IsDecision = true,
            IsAutoCheck = true,
            IsTerminal = false,
            RequiresNote = true,
            Status = Status.New,
            DecisionAnswer = "",
            Notes = ""
        };
        
        // Auto-check if version is affected
        bool isVersionAffected = affectedVersions.Contains(productVersion);
        step1.DecisionAnswer = isVersionAffected ? "Yes" : "No";
        step1.Status = Status.Done;
        step1.CompletedAt = DateTime.UtcNow;
        step1.AutoCheckResult = isVersionAffected;
        
        if (!isVersionAffected)
        {
            // No path - single terminal step
            step1.Notes = $"This product is version {productVersion} and is not impacted by this core bug which affects versions: {string.Join(", ", affectedVersions)}";
            step1.IsTerminal = true;
            
            // Create terminal step
            var terminalStep = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Don't Clone Bug",
                Description = "Don't clone the bug as this version is not affected",
                Order = 2,
                IsDecision = false,
                IsAutoCheck = true,
                IsTerminal = true,
                RequiresNote = true,
                Status = Status.Done,
                CompletedAt = DateTime.UtcNow,
                Notes = $"Bug not cloned. Product version {productVersion} is not in the affected versions list.",
                DecisionAnswer = ""
            };
            
            step1.NextStepIfNo = terminalStep.TaskStepId;
            steps.Add(step1);
            steps.Add(terminalStep);
        }
        else
        {
            // Yes path - continue with more steps
            step1.Notes = $"This product version {productVersion} is affected by the bug";
            
            // Step 2: Do preconditions apply? (MANUAL)
            var step2 = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Check Preconditions",
                Description = "Do the preconditions apply?",
                Order = 2,
                IsDecision = true,
                IsAutoCheck = false,
                IsTerminal = false,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Step 2 - No path: Close as Function Not Utilized
            var step2No = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Close as Function Not Utilized",
                Description = "Close the bug as Function Not Utilized",
                Order = 3,
                IsDecision = false,
                IsAutoCheck = false,
                IsTerminal = true,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Step 3: Does it reproduce? (MANUAL)
            var step3 = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Test Reproduction",
                Description = "Does it reproduce?",
                Order = 3,
                IsDecision = true,
                IsAutoCheck = false,
                IsTerminal = false,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Step 3 - No path: Close as Invalid
            var step3No = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Close as Invalid",
                Description = "Close the bug as Invalid",
                Order = 4,
                IsDecision = false,
                IsAutoCheck = false,
                IsTerminal = true,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Step 4: Is severity Major/Critical? (AUTO)
            var step4 = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Check Severity",
                Description = "Is the severity Major or Critical?",
                Order = 4,
                IsDecision = true,
                IsAutoCheck = true,
                IsTerminal = false,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Auto-check severity
            bool isMajorOrCritical = bugSeverity == BugSeverity.Major || bugSeverity == BugSeverity.Critical;
            step4.DecisionAnswer = isMajorOrCritical ? "Yes" : "No";
            step4.Status = Status.Done;
            step4.CompletedAt = DateTime.UtcNow;
            step4.AutoCheckResult = isMajorOrCritical;
            step4.Notes = $"Bug severity is {bugSeverity}";
            
            // Step 4 - No path: Close as Won't Fix + ImpactConfirmed
            var step4No = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Close as Won't Fix",
                Description = "Close the bug as Won't Fix and apply ImpactConfirmed label",
                Order = 5,
                IsDecision = false,
                IsAutoCheck = false,
                IsTerminal = true,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Step 4 - Yes path: Leave as New + ImpactConfirmed
            var step4Yes = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Keep as New",
                Description = "Leave the bug as New and apply ImpactConfirmed label",
                Order = 5,
                IsDecision = false,
                IsAutoCheck = false,
                IsTerminal = true,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Step 0: Clone bug (inserted before decision tree)
            var step0 = new TaskStep
            {
                TaskStepId = Guid.NewGuid(),
                TaskId = taskId,
                Action = "Clone Bug in JIRA",
                Description = "Clone the bug in JIRA Epic of the Product",
                Order = 0,
                IsDecision = false,
                IsAutoCheck = false,
                IsTerminal = false,
                RequiresNote = true,
                Status = Status.New,
                DecisionAnswer = "",
                Notes = ""
            };
            
            // Set up navigation
            step1.NextStepIfYes = step2.TaskStepId;
            step2.NextStepIfNo = step2No.TaskStepId;
            step2.NextStepIfYes = step3.TaskStepId;
            step3.NextStepIfNo = step3No.TaskStepId;
            step3.NextStepIfYes = step4.TaskStepId;
            step4.NextStepIfNo = step4No.TaskStepId;
            step4.NextStepIfYes = step4Yes.TaskStepId;
            
            // Add all steps
            steps.Add(step0); // Clone bug step
            steps.Add(step1); // Version check
            steps.Add(step2); // Preconditions
            steps.Add(step2No); // Function Not Utilized
            steps.Add(step3); // Reproduction
            steps.Add(step3No); // Invalid
            steps.Add(step4); // Severity
            steps.Add(step4No); // Won't Fix
            steps.Add(step4Yes); // Keep as New
        }
        
        return steps.OrderBy(s => s.Order).ToList();
    }
    
    private void CheckAndAutoCompleteTask(CustomTask task)
    {
        // Check if all steps are completed
        var allStepsCompleted = task.TaskSteps.All(ts => ts.Status == Status.Done);
        
        // Check if we've reached a terminal step
        var terminalStepReached = task.TaskSteps.Any(ts => ts.IsTerminal && ts.Status == Status.Done);
        
        if (allStepsCompleted || terminalStepReached)
        {
            task.Status = Status.Done;
            task.CompletedAt = DateTime.UtcNow;
            
            // Log completion
            _logger.LogInformation("Task {TaskId} auto-completed. Terminal step reached: {TerminalReached}", 
                task.TaskId, terminalStepReached);
        }
        else
        {
            // Set to InProgress if any steps are completed but not all
            var anyStepsCompleted = task.TaskSteps.Any(ts => ts.Status == Status.Done);
            if (anyStepsCompleted)
            {
                task.Status = Status.InProgress;
            }
        }
    }
}