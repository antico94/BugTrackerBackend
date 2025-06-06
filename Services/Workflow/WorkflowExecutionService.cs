using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models.Workflow;
using System.Text.Json;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Service for managing workflow executions
/// </summary>
public class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly BugTrackerContext _context;
    private readonly ILogger<WorkflowExecutionService> _logger;

    public WorkflowExecutionService(BugTrackerContext context, ILogger<WorkflowExecutionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkflowExecution?> GetWorkflowExecutionAsync(Guid taskId)
    {
        return await _context.WorkflowExecutions
            .Include(we => we.WorkflowDefinition)
            .Include(we => we.AuditLogs)
            .FirstOrDefaultAsync(we => we.TaskId == taskId);
    }

    public async Task<WorkflowExecution> CreateWorkflowExecutionAsync(Guid taskId, Guid workflowDefinitionId, string initialStepId, Dictionary<string, object>? context = null)
    {
        try
        {
            // Check if a workflow execution already exists for this task
            var existingExecution = await GetWorkflowExecutionAsync(taskId);
            if (existingExecution != null)
            {
                throw new InvalidOperationException($"Workflow execution already exists for task {taskId}");
            }

            var contextJson = context != null ? JsonSerializer.Serialize(context) : "{}";

            var execution = new WorkflowExecution
            {
                WorkflowExecutionId = Guid.NewGuid(),
                TaskId = taskId,
                WorkflowDefinitionId = workflowDefinitionId,
                CurrentStepId = initialStepId,
                Status = WorkflowExecutionStatus.Active,
                ContextJson = contextJson,
                StartedAt = DateTime.UtcNow,
                StartedBy = "System", // TODO: Get from current user context
                LastUpdated = DateTime.UtcNow
            };

            _context.WorkflowExecutions.Add(execution);

            // Create initial audit log entry
            var initialAuditLog = new WorkflowAuditLog
            {
                WorkflowAuditLogId = Guid.NewGuid(),
                WorkflowExecutionId = execution.WorkflowExecutionId,
                StepId = initialStepId,
                StepName = "Workflow Started",
                Action = "workflow_started",
                Result = "Success",
                PreviousStepId = null,
                NextStepId = initialStepId,
                ContextSnapshot = contextJson,
                Timestamp = DateTime.UtcNow,
                PerformedBy = "System",
                DurationMs = 0
            };

            _context.WorkflowAuditLogs.Add(initialAuditLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created workflow execution {ExecutionId} for task {TaskId} starting at step {StepId}", 
                execution.WorkflowExecutionId, taskId, initialStepId);

            return execution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow execution for task {TaskId}", taskId);
            throw;
        }
    }

    public async Task UpdateWorkflowExecutionStepAsync(Guid workflowExecutionId, string newStepId, Dictionary<string, object>? updatedContext = null)
    {
        try
        {
            var execution = await _context.WorkflowExecutions
                .FirstOrDefaultAsync(we => we.WorkflowExecutionId == workflowExecutionId);

            if (execution == null)
            {
                throw new ArgumentException($"Workflow execution {workflowExecutionId} not found");
            }

            var previousStepId = execution.CurrentStepId;
            execution.CurrentStepId = newStepId;
            execution.LastUpdated = DateTime.UtcNow;

            if (updatedContext != null)
            {
                execution.ContextJson = JsonSerializer.Serialize(updatedContext);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated workflow execution {ExecutionId} from step {PreviousStep} to step {NewStep}", 
                workflowExecutionId, previousStepId, newStepId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow execution {ExecutionId} to step {StepId}", 
                workflowExecutionId, newStepId);
            throw;
        }
    }

    public async Task CompleteWorkflowExecutionAsync(Guid workflowExecutionId)
    {
        try
        {
            var execution = await _context.WorkflowExecutions
                .FirstOrDefaultAsync(we => we.WorkflowExecutionId == workflowExecutionId);

            if (execution == null)
            {
                throw new ArgumentException($"Workflow execution {workflowExecutionId} not found");
            }

            execution.Status = WorkflowExecutionStatus.Completed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.LastUpdated = DateTime.UtcNow;

            // Create completion audit log entry
            var completionAuditLog = new WorkflowAuditLog
            {
                WorkflowAuditLogId = Guid.NewGuid(),
                WorkflowExecutionId = workflowExecutionId,
                StepId = execution.CurrentStepId,
                StepName = "Workflow Completed",
                Action = "workflow_completed",
                Result = "Success",
                PreviousStepId = execution.CurrentStepId,
                NextStepId = null,
                ContextSnapshot = execution.ContextJson,
                Timestamp = DateTime.UtcNow,
                PerformedBy = "System",
                DurationMs = 0
            };

            _context.WorkflowAuditLogs.Add(completionAuditLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed workflow execution {ExecutionId} at step {StepId}", 
                workflowExecutionId, execution.CurrentStepId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing workflow execution {ExecutionId}", workflowExecutionId);
            throw;
        }
    }

    public async Task AddAuditLogAsync(WorkflowAuditLog auditLog)
    {
        try
        {
            _context.WorkflowAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Added audit log entry for workflow execution {ExecutionId}: {Action} on step {StepId}", 
                auditLog.WorkflowExecutionId, auditLog.Action, auditLog.StepId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding audit log for workflow execution {ExecutionId}", 
                auditLog.WorkflowExecutionId);
            throw;
        }
    }

    public async Task SuspendWorkflowExecutionAsync(Guid workflowExecutionId, string reason)
    {
        try
        {
            var execution = await _context.WorkflowExecutions
                .FirstOrDefaultAsync(we => we.WorkflowExecutionId == workflowExecutionId);

            if (execution == null)
            {
                throw new ArgumentException($"Workflow execution {workflowExecutionId} not found");
            }

            execution.Status = WorkflowExecutionStatus.Suspended;
            execution.LastUpdated = DateTime.UtcNow;

            // Create suspension audit log entry
            var suspensionAuditLog = new WorkflowAuditLog
            {
                WorkflowAuditLogId = Guid.NewGuid(),
                WorkflowExecutionId = workflowExecutionId,
                StepId = execution.CurrentStepId,
                StepName = "Workflow Suspended",
                Action = "workflow_suspended",
                Result = "Suspended",
                Notes = reason,
                ContextSnapshot = execution.ContextJson,
                Timestamp = DateTime.UtcNow,
                PerformedBy = "System",
                DurationMs = 0
            };

            _context.WorkflowAuditLogs.Add(suspensionAuditLog);

            await _context.SaveChangesAsync();

            _logger.LogWarning("Suspended workflow execution {ExecutionId} at step {StepId}: {Reason}", 
                workflowExecutionId, execution.CurrentStepId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending workflow execution {ExecutionId}", workflowExecutionId);
            throw;
        }
    }

    public async Task ResumeWorkflowExecutionAsync(Guid workflowExecutionId)
    {
        try
        {
            var execution = await _context.WorkflowExecutions
                .FirstOrDefaultAsync(we => we.WorkflowExecutionId == workflowExecutionId);

            if (execution == null)
            {
                throw new ArgumentException($"Workflow execution {workflowExecutionId} not found");
            }

            if (execution.Status != WorkflowExecutionStatus.Suspended)
            {
                throw new InvalidOperationException($"Workflow execution {workflowExecutionId} is not suspended (current status: {execution.Status})");
            }

            execution.Status = WorkflowExecutionStatus.Active;
            execution.LastUpdated = DateTime.UtcNow;

            // Create resumption audit log entry
            var resumptionAuditLog = new WorkflowAuditLog
            {
                WorkflowAuditLogId = Guid.NewGuid(),
                WorkflowExecutionId = workflowExecutionId,
                StepId = execution.CurrentStepId,
                StepName = "Workflow Resumed",
                Action = "workflow_resumed",
                Result = "Active",
                ContextSnapshot = execution.ContextJson,
                Timestamp = DateTime.UtcNow,
                PerformedBy = "System",
                DurationMs = 0
            };

            _context.WorkflowAuditLogs.Add(resumptionAuditLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Resumed workflow execution {ExecutionId} at step {StepId}", 
                workflowExecutionId, execution.CurrentStepId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming workflow execution {ExecutionId}", workflowExecutionId);
            throw;
        }
    }

    public async Task FailWorkflowExecutionAsync(Guid workflowExecutionId, string errorMessage, Exception? exception = null)
    {
        try
        {
            var execution = await _context.WorkflowExecutions
                .FirstOrDefaultAsync(we => we.WorkflowExecutionId == workflowExecutionId);

            if (execution == null)
            {
                throw new ArgumentException($"Workflow execution {workflowExecutionId} not found");
            }

            execution.Status = WorkflowExecutionStatus.Failed;
            execution.ErrorMessage = errorMessage;
            execution.LastUpdated = DateTime.UtcNow;

            // Create failure audit log entry
            var failureAuditLog = new WorkflowAuditLog
            {
                WorkflowAuditLogId = Guid.NewGuid(),
                WorkflowExecutionId = workflowExecutionId,
                StepId = execution.CurrentStepId,
                StepName = "Workflow Failed",
                Action = "workflow_failed",
                Result = "Failed",
                Notes = errorMessage,
                ContextSnapshot = execution.ContextJson,
                Timestamp = DateTime.UtcNow,
                PerformedBy = "System",
                DurationMs = 0
            };

            _context.WorkflowAuditLogs.Add(failureAuditLog);

            await _context.SaveChangesAsync();

            _logger.LogError(exception, "Failed workflow execution {ExecutionId} at step {StepId}: {Error}", 
                workflowExecutionId, execution.CurrentStepId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error failing workflow execution {ExecutionId}", workflowExecutionId);
            throw;
        }
    }

    public async Task<List<WorkflowAuditLog>> GetAuditTrailAsync(Guid workflowExecutionId)
    {
        try
        {
            var auditLogs = await _context.WorkflowAuditLogs
                .Where(al => al.WorkflowExecutionId == workflowExecutionId)
                .OrderBy(al => al.Timestamp)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} audit log entries for workflow execution {ExecutionId}", 
                auditLogs.Count, workflowExecutionId);

            return auditLogs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for workflow execution {ExecutionId}", workflowExecutionId);
            throw;
        }
    }

    public async Task<List<WorkflowDefinition>> GetWorkflowDefinitionsAsync()
    {
        try
        {
            var definitions = await _context.WorkflowDefinitions
                .Where(wd => wd.IsActive)
                .OrderBy(wd => wd.Name)
                .ThenByDescending(wd => wd.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} active workflow definitions", definitions.Count);

            return definitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow definitions");
            throw;
        }
    }

    public async Task<object> GetWorkflowStatisticsAsync()
    {
        try
        {
            var stats = new
            {
                TotalExecutions = await _context.WorkflowExecutions.CountAsync(),
                ActiveExecutions = await _context.WorkflowExecutions
                    .CountAsync(we => we.Status == WorkflowExecutionStatus.Active),
                CompletedExecutions = await _context.WorkflowExecutions
                    .CountAsync(we => we.Status == WorkflowExecutionStatus.Completed),
                FailedExecutions = await _context.WorkflowExecutions
                    .CountAsync(we => we.Status == WorkflowExecutionStatus.Failed),
                AverageCompletionTimeMinutes = await _context.WorkflowExecutions
                    .Where(we => we.Status == WorkflowExecutionStatus.Completed && we.CompletedAt.HasValue)
                    .Select(we => EF.Functions.DateDiffMinute(we.StartedAt, we.CompletedAt!.Value))
                    .DefaultIfEmpty(0)
                    .AverageAsync(),
                StepCompletionCounts = await _context.WorkflowAuditLogs
                    .Where(al => al.Action == "complete" || al.Action == "decide_yes" || al.Action == "decide_no")
                    .GroupBy(al => al.StepName)
                    .Select(g => new { StepName = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.StepName, x => x.Count),
                WorkflowUsageCounts = await _context.WorkflowExecutions
                    .Include(we => we.WorkflowDefinition)
                    .GroupBy(we => we.WorkflowDefinition!.Name)
                    .Select(g => new { WorkflowName = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.WorkflowName, x => x.Count)
            };

            _logger.LogDebug("Retrieved workflow statistics: {TotalExecutions} total, {ActiveExecutions} active, {CompletedExecutions} completed", 
                stats.TotalExecutions, stats.ActiveExecutions, stats.CompletedExecutions);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow statistics");
            throw;
        }
    }
}