using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.Models.Enums;
using BugTracker.Models.Workflow;
using System.Text.Json;

namespace BugTracker.Services.Workflow;

/// <summary>
/// New workflow-based task generation service that replaces the hardcoded TaskGenerationService
/// </summary>
public class WorkflowTaskGenerationService
{
    private readonly BugTrackerContext _context;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly WorkflowSeederService _workflowSeeder;
    private readonly ILogger<WorkflowTaskGenerationService> _logger;

    public WorkflowTaskGenerationService(
        BugTrackerContext context,
        IWorkflowEngine workflowEngine,
        WorkflowSeederService workflowSeeder,
        ILogger<WorkflowTaskGenerationService> logger)
    {
        _context = context;
        _workflowEngine = workflowEngine;
        _workflowSeeder = workflowSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Generates workflow-based tasks for an assessed bug
    /// </summary>
    public async Task<List<CustomTask>> GenerateTasksForAssessedBugAsync(CoreBug assessedBug)
    {
        var tasks = new List<CustomTask>();
        
        if (!assessedBug.IsAssessed || assessedBug.AssessedProductType == null)
        {
            _logger.LogWarning("Bug {BugId} is not assessed or missing product type", assessedBug.BugId);
            return tasks;
        }

        try
        {
            // Ensure workflow definitions are seeded
            if (await _workflowSeeder.NeedsSeeding())
            {
                await _workflowSeeder.SeedWorkflowDefinitionsAsync();
            }

            var bugAssessmentWorkflow = await _workflowSeeder.GetBugAssessmentWorkflowAsync();
            var versionsToCheck = GetVersionsToCheck(assessedBug);

            // Generate tasks based on product type
            switch (assessedBug.AssessedProductType)
            {
                case ProductType.TM:
                    tasks = await GenerateTrialManagerTasksAsync(assessedBug, versionsToCheck, bugAssessmentWorkflow);
                    break;
                    
                case ProductType.InteractiveResponseTechnology:
                case ProductType.ExternalModule:
                    tasks = await GenerateIRTTasksAsync(assessedBug, versionsToCheck, bugAssessmentWorkflow);
                    break;
            }

            _logger.LogInformation("Generated {TaskCount} workflow-based tasks for bug {BugId}", tasks.Count, assessedBug.BugId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating workflow-based tasks for bug {BugId}", assessedBug.BugId);
            throw;
        }

        return tasks;
    }

    private async Task<List<CustomTask>> GenerateTrialManagerTasksAsync(CoreBug bug, List<string> versionsToCheck, WorkflowDefinition workflowDefinition)
    {
        var tasks = new List<CustomTask>();
        
        var trialManagers = await _context.TrialManagers
            .Include(tm => tm.Client)
            .Include(tm => tm.Studies)
            .ToListAsync();

        foreach (var tm in trialManagers)
        {
            var task = new CustomTask
            {
                TaskId = Guid.NewGuid(),
                BugId = bug.BugId,
                TrialManagerId = tm.TrialManagerId,
                StudyId = tm.Studies.FirstOrDefault()?.StudyId,
                TaskTitle = $"{bug.JiraKey} - {tm.Protocol}",
                TaskDescription = $"Assess impact of bug {bug.JiraKey} on Trial Manager {tm.Client?.Name ?? "Unknown"} v{tm.Version}",
                JiraTaskKey = "",
                JiraTaskLink = "",
                Status = Status.New,
                CreatedAt = DateTime.UtcNow
            };

            // Create workflow context with all necessary data
            var workflowContext = CreateWorkflowContext(bug, tm.Version, versionsToCheck);

            // Start the workflow for this task
            try
            {
                var workflowExecution = await _workflowEngine.StartWorkflowAsync(
                    task.TaskId, 
                    workflowDefinition.Name, 
                    workflowContext);

                // Auto-execute any auto-check steps
                await ProcessAutoCheckSteps(task.TaskId, workflowContext);

                _logger.LogInformation("Started workflow execution {ExecutionId} for TM task {TaskId}", 
                    workflowExecution.WorkflowExecutionId, task.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting workflow for TM task {TaskId}", task.TaskId);
                // Continue with other tasks even if one fails
            }

            tasks.Add(task);
        }

        return tasks;
    }

    private async Task<List<CustomTask>> GenerateIRTTasksAsync(CoreBug bug, List<string> versionsToCheck, WorkflowDefinition workflowDefinition)
    {
        var tasks = new List<CustomTask>();
        
        var irts = await _context.InteractiveResponseTechnologies
            .Include(irt => irt.Study)
                .ThenInclude(s => s.Client)
            .Include(irt => irt.TrialManager)
            .ToListAsync();

        foreach (var irt in irts)
        {
            var task = new CustomTask
            {
                TaskId = Guid.NewGuid(),
                BugId = bug.BugId,
                InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                StudyId = irt.StudyId,
                TaskTitle = $"{bug.JiraKey} - {irt.Protocol}",
                TaskDescription = $"Assess impact of bug {bug.JiraKey} on IRT {irt.Study?.Name ?? "Unknown"} v{irt.Version}",
                JiraTaskKey = "",
                JiraTaskLink = "",
                Status = Status.New,
                CreatedAt = DateTime.UtcNow
            };

            // Create workflow context with all necessary data
            var workflowContext = CreateWorkflowContext(bug, irt.Version, versionsToCheck);

            // Start the workflow for this task
            try
            {
                var workflowExecution = await _workflowEngine.StartWorkflowAsync(
                    task.TaskId, 
                    workflowDefinition.Name, 
                    workflowContext);

                // Auto-execute any auto-check steps
                await ProcessAutoCheckSteps(task.TaskId, workflowContext);

                _logger.LogInformation("Started workflow execution {ExecutionId} for IRT task {TaskId}", 
                    workflowExecution.WorkflowExecutionId, task.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting workflow for IRT task {TaskId}", task.TaskId);
                // Continue with other tasks even if one fails
            }

            tasks.Add(task);
        }

        return tasks;
    }

    private Dictionary<string, object> CreateWorkflowContext(CoreBug bug, string productVersion, List<string> affectedVersions)
    {
        var versionAffected = affectedVersions.Contains(productVersion);
        var severityIsMajorOrCritical = bug.Severity == BugSeverity.Major || bug.Severity == BugSeverity.Critical;

        return new Dictionary<string, object>
        {
            // Bug information
            ["bugId"] = bug.BugId,
            ["bugJiraKey"] = bug.JiraKey,
            ["bugTitle"] = bug.BugTitle,
            ["bugSeverity"] = bug.Severity.ToString(),
            ["bugDescription"] = bug.BugDescription ?? "",
            
            // Version information
            ["productVersion"] = productVersion,
            ["affectedVersions"] = affectedVersions,
            ["versionAffected"] = versionAffected,
            
            // Severity evaluation
            ["severityIsMajorOrCritical"] = severityIsMajorOrCritical,
            
            // Auto-generated notes for auto-check steps
            ["versionCheckNotes"] = versionAffected 
                ? $"This product version {productVersion} is affected by the bug"
                : $"This product is version {productVersion} and is not impacted by this core bug which affects versions: {string.Join(", ", affectedVersions)}",
            
            ["severityCheckNotes"] = $"Bug severity is {bug.Severity}",
            
            // Timestamps
            ["workflowStarted"] = DateTime.UtcNow,
            ["bugCreated"] = bug.CreatedAt
        };
    }

    private async Task ProcessAutoCheckSteps(Guid taskId, Dictionary<string, object> context)
    {
        try
        {
            var workflowState = await _workflowEngine.GetWorkflowStateAsync(taskId);
            
            // Process auto-check steps
            while (workflowState.CurrentStep != null && 
                   workflowState.CurrentStep.Type == WorkflowStepType.AutoCheck && 
                   workflowState.Status == WorkflowExecutionStatus.Active)
            {
                var autoAction = workflowState.AvailableActions.FirstOrDefault();
                if (autoAction == null) break;

                // Execute the auto-check action
                var actionRequest = new WorkflowActionRequest
                {
                    ActionId = autoAction.ActionId,
                    PerformedBy = "System",
                    AdditionalData = context
                };

                var result = await _workflowEngine.ExecuteActionAsync(taskId, actionRequest);
                
                if (!result.Success)
                {
                    _logger.LogWarning("Auto-check step failed for task {TaskId}: {Message}", taskId, result.Message);
                    break;
                }

                workflowState = result.NewState ?? await _workflowEngine.GetWorkflowStateAsync(taskId);
                
                _logger.LogDebug("Auto-executed step for task {TaskId}: {StepName}", taskId, workflowState.CurrentStep?.Name ?? "Unknown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auto-check steps for task {TaskId}", taskId);
        }
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
}