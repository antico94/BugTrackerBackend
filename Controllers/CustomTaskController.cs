// Controllers/CustomTaskController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.DTOs;
using BugTracker.Models.Enums;

namespace BugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomTaskController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<CustomTaskController> _logger;

        public CustomTaskController(BugTrackerContext context, ILogger<CustomTaskController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/CustomTask
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomTaskResponseDto>>> GetCustomTasks(
            [FromQuery] Status? status = null,
            [FromQuery] ProductType? productType = null,
            [FromQuery] Guid? studyId = null,
            [FromQuery] Guid? bugId = null)
        {
            try
            {
                var query = _context.CustomTasks
                    .Include(t => t.CoreBug)
                    .Include(t => t.Study)
                        .ThenInclude(s => s.Client)
                    .Include(t => t.TrialManager)
                        .ThenInclude(tm => tm.Client)
                    .Include(t => t.InteractiveResponseTechnology)
                    .Include(t => t.TaskSteps)
                    .Include(t => t.TaskNotes)
                    .AsQueryable();

                // Apply filters
                if (status.HasValue)
                    query = query.Where(t => t.Status == status.Value);

                if (studyId.HasValue)
                    query = query.Where(t => t.StudyId == studyId.Value);

                if (bugId.HasValue)
                    query = query.Where(t => t.BugId == bugId.Value);

                if (productType.HasValue)
                {
                    if (productType.Value == ProductType.TM)
                        query = query.Where(t => t.TrialManagerId != null);
                    else if (productType.Value == ProductType.InteractiveResponseTechnology)
                        query = query.Where(t => t.InteractiveResponseTechnologyId != null);
                }

                var tasksData = await query
                    .Select(t => new 
                    {
                        t.TaskId,
                        t.TaskTitle,
                        t.TaskDescription,
                        t.JiraTaskKey,
                        t.JiraTaskLink,
                        t.Status,
                        t.CreatedAt,
                        t.CompletedAt,
                        t.BugId,
                        t.StudyId,
                        t.TrialManagerId,
                        t.InteractiveResponseTechnologyId,
                        CoreBug = t.CoreBug != null ? new CoreBugBasicDto
                        {
                            BugId = t.CoreBug.BugId,
                            BugTitle = t.CoreBug.BugTitle,
                            JiraKey = t.CoreBug.JiraKey,
                            JiraLink = t.CoreBug.JiraLink,
                            Severity = t.CoreBug.Severity
                        } : null,
                        Study = t.Study != null ? new StudyBasicDto
                        {
                            StudyId = t.Study.StudyId,
                            Name = t.Study.Name,
                            Protocol = t.Study.Protocol,
                            Description = t.Study.Description,
                            Client = t.Study.Client != null ? new ClientSummaryDto
                            {
                                ClientId = t.Study.Client.ClientId,
                                Name = t.Study.Client.Name,
                                Description = t.Study.Client.Description
                            } : null
                        } : null,
                        TrialManager = t.TrialManager != null ? new TrialManagerSummaryDto
                        {
                            TrialManagerId = t.TrialManager.TrialManagerId,
                            Version = t.TrialManager.Version,
                            JiraKey = t.TrialManager.JiraKey
                        } : null,
                        InteractiveResponseTechnology = t.InteractiveResponseTechnology != null ? new IRTBasicDto
                        {
                            InteractiveResponseTechnologyId = t.InteractiveResponseTechnology.InteractiveResponseTechnologyId,
                            Version = t.InteractiveResponseTechnology.Version,
                            JiraKey = t.InteractiveResponseTechnology.JiraKey,
                            WebLink = t.InteractiveResponseTechnology.WebLink,
                            Study = null // Avoid circular reference in summary
                        } : null,
                        TaskSteps = t.TaskSteps.OrderBy(ts => ts.Order).Select(ts => new TaskStepResponseDto
                        {
                            TaskStepId = ts.TaskStepId,
                            Action = ts.Action,
                            Description = ts.Description,
                            Order = ts.Order,
                            IsDecision = ts.IsDecision,
                            IsAutoCheck = ts.IsAutoCheck,
                            IsTerminal = ts.IsTerminal,
                            RequiresNote = ts.RequiresNote,
                            Status = ts.Status,
                            CompletedAt = ts.CompletedAt,
                            DecisionAnswer = ts.DecisionAnswer,
                            Notes = ts.Notes,
                            AutoCheckResult = ts.AutoCheckResult,
                            NextStepIfYes = ts.NextStepIfYes,
                            NextStepIfNo = ts.NextStepIfNo,
                            NextStepIfTrue = ts.NextStepIfTrue,
                            NextStepIfFalse = ts.NextStepIfFalse
                        }).ToList(),
                        TaskNotes = t.TaskNotes.OrderByDescending(tn => tn.CreatedAt).Select(tn => new TaskNoteResponseDto
                        {
                            TaskNoteId = tn.TaskNoteId,
                            Content = tn.Content,
                            CreatedAt = tn.CreatedAt,
                            UpdatedAt = tn.UpdatedAt,
                            CreatedBy = tn.CreatedBy
                        }).ToList(),
                        CompletedStepsCount = GetActualCompletedStepsCount(t.TaskSteps),
                        TotalStepsCount = GetActualTotalStepsCount(t.TaskSteps)
                    })
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                // Convert to response DTOs after database query
                var customTasks = tasksData.Select(t => new CustomTaskResponseDto
                {
                    TaskId = t.TaskId,
                    TaskTitle = t.TaskTitle,
                    TaskDescription = t.TaskDescription,
                    JiraTaskKey = t.JiraTaskKey,
                    JiraTaskLink = t.JiraTaskLink,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt,
                    BugId = t.BugId,
                    StudyId = t.StudyId,
                    TrialManagerId = t.TrialManagerId,
                    InteractiveResponseTechnologyId = t.InteractiveResponseTechnologyId,
                    CoreBug = t.CoreBug,
                    Study = t.Study,
                    TrialManager = t.TrialManager,
                    InteractiveResponseTechnology = t.InteractiveResponseTechnology,
                    TaskSteps = t.TaskSteps,
                    TaskNotes = t.TaskNotes,
                    ProductName = t.TrialManager?.TrialManagerId != null ? t.Study?.Client?.Name ?? "Unknown" : t.Study?.Name ?? "Unknown",
                    ProductVersion = t.TrialManager?.Version ?? t.InteractiveResponseTechnology?.Version ?? "Unknown",
                    ProductType = t.TrialManagerId != null ? ProductType.TM : ProductType.InteractiveResponseTechnology,
                    CurrentStepId = GetCurrentStepId(t.TaskSteps),
                    CompletedStepsCount = t.CompletedStepsCount,
                    TotalStepsCount = t.TotalStepsCount
                }).ToList();

                return Ok(customTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom tasks");
                return StatusCode(500, "An error occurred while retrieving custom tasks");
            }
        }

        // GET: api/CustomTask/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomTaskResponseDto>> GetCustomTask(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid task ID");
                }

                var taskData = await _context.CustomTasks
                    .Include(t => t.CoreBug)
                    .Include(t => t.Study)
                        .ThenInclude(s => s.Client)
                    .Include(t => t.TrialManager)
                        .ThenInclude(tm => tm.Client)
                    .Include(t => t.InteractiveResponseTechnology)
                    .Include(t => t.TaskSteps)
                    .Include(t => t.TaskNotes)
                    .Where(t => t.TaskId == id)
                    .Select(t => new 
                    {
                        t.TaskId,
                        t.TaskTitle,
                        t.TaskDescription,
                        t.JiraTaskKey,
                        t.JiraTaskLink,
                        t.Status,
                        t.CreatedAt,
                        t.CompletedAt,
                        t.BugId,
                        t.StudyId,
                        t.TrialManagerId,
                        t.InteractiveResponseTechnologyId,
                        CoreBug = t.CoreBug != null ? new CoreBugBasicDto
                        {
                            BugId = t.CoreBug.BugId,
                            BugTitle = t.CoreBug.BugTitle,
                            JiraKey = t.CoreBug.JiraKey,
                            JiraLink = t.CoreBug.JiraLink,
                            Severity = t.CoreBug.Severity
                        } : null,
                        Study = t.Study != null ? new StudyBasicDto
                        {
                            StudyId = t.Study.StudyId,
                            Name = t.Study.Name,
                            Protocol = t.Study.Protocol,
                            Description = t.Study.Description,
                            Client = t.Study.Client != null ? new ClientSummaryDto
                            {
                                ClientId = t.Study.Client.ClientId,
                                Name = t.Study.Client.Name,
                                Description = t.Study.Client.Description
                            } : null
                        } : null,
                        TrialManager = t.TrialManager != null ? new TrialManagerSummaryDto
                        {
                            TrialManagerId = t.TrialManager.TrialManagerId,
                            Version = t.TrialManager.Version,
                            JiraKey = t.TrialManager.JiraKey
                        } : null,
                        InteractiveResponseTechnology = t.InteractiveResponseTechnology != null ? new IRTBasicDto
                        {
                            InteractiveResponseTechnologyId = t.InteractiveResponseTechnology.InteractiveResponseTechnologyId,
                            Version = t.InteractiveResponseTechnology.Version,
                            JiraKey = t.InteractiveResponseTechnology.JiraKey,
                            WebLink = t.InteractiveResponseTechnology.WebLink
                        } : null,
                        TaskSteps = t.TaskSteps.OrderBy(ts => ts.Order).Select(ts => new TaskStepResponseDto
                        {
                            TaskStepId = ts.TaskStepId,
                            Action = ts.Action,
                            Description = ts.Description,
                            Order = ts.Order,
                            IsDecision = ts.IsDecision,
                            IsAutoCheck = ts.IsAutoCheck,
                            IsTerminal = ts.IsTerminal,
                            RequiresNote = ts.RequiresNote,
                            Status = ts.Status,
                            CompletedAt = ts.CompletedAt,
                            DecisionAnswer = ts.DecisionAnswer,
                            Notes = ts.Notes,
                            AutoCheckResult = ts.AutoCheckResult,
                            NextStepIfYes = ts.NextStepIfYes,
                            NextStepIfNo = ts.NextStepIfNo,
                            NextStepIfTrue = ts.NextStepIfTrue,
                            NextStepIfFalse = ts.NextStepIfFalse
                        }).ToList(),
                        TaskNotes = t.TaskNotes.OrderByDescending(tn => tn.CreatedAt).Select(tn => new TaskNoteResponseDto
                        {
                            TaskNoteId = tn.TaskNoteId,
                            Content = tn.Content,
                            CreatedAt = tn.CreatedAt,
                            UpdatedAt = tn.UpdatedAt,
                            CreatedBy = tn.CreatedBy
                        }).ToList(),
                        CompletedStepsCount = GetActualCompletedStepsCount(t.TaskSteps),
                        TotalStepsCount = GetActualTotalStepsCount(t.TaskSteps)
                    })
                    .FirstOrDefaultAsync();

                if (taskData == null)
                {
                    return NotFound($"Custom task with ID {id} not found");
                }

                // Convert to response DTO after database query
                var customTask = new CustomTaskResponseDto
                {
                    TaskId = taskData.TaskId,
                    TaskTitle = taskData.TaskTitle,
                    TaskDescription = taskData.TaskDescription,
                    JiraTaskKey = taskData.JiraTaskKey,
                    JiraTaskLink = taskData.JiraTaskLink,
                    Status = taskData.Status,
                    CreatedAt = taskData.CreatedAt,
                    CompletedAt = taskData.CompletedAt,
                    BugId = taskData.BugId,
                    StudyId = taskData.StudyId,
                    TrialManagerId = taskData.TrialManagerId,
                    InteractiveResponseTechnologyId = taskData.InteractiveResponseTechnologyId,
                    CoreBug = taskData.CoreBug,
                    Study = taskData.Study,
                    TrialManager = taskData.TrialManager,
                    InteractiveResponseTechnology = taskData.InteractiveResponseTechnology,
                    TaskSteps = taskData.TaskSteps,
                    TaskNotes = taskData.TaskNotes,
                    ProductName = taskData.TrialManager?.TrialManagerId != null ? taskData.Study?.Client?.Name ?? "Unknown" : taskData.Study?.Name ?? "Unknown",
                    ProductVersion = taskData.TrialManager?.Version ?? taskData.InteractiveResponseTechnology?.Version ?? "Unknown",
                    ProductType = taskData.TrialManagerId != null ? ProductType.TM : ProductType.InteractiveResponseTechnology,
                    CurrentStepId = GetCurrentStepId(taskData.TaskSteps),
                    CompletedStepsCount = taskData.CompletedStepsCount,
                    TotalStepsCount = taskData.TotalStepsCount
                };

                return Ok(customTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom task {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the custom task");
            }
        }

        // POST: api/CustomTask/{id}/complete-step
        [HttpPost("{id}/complete-step")]
        public async Task<ActionResult> CompleteTaskStep(Guid id, CompleteTaskStepDto completeStepDto)
        {
            try
            {
                if (id != completeStepDto.TaskId)
                {
                    return BadRequest("Task ID mismatch");
                }

                var task = await _context.CustomTasks
                    .Include(t => t.TaskSteps)
                    .FirstOrDefaultAsync(t => t.TaskId == id);

                if (task == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                var taskStep = task.TaskSteps.FirstOrDefault(ts => ts.TaskStepId == completeStepDto.TaskStepId);
                if (taskStep == null)
                {
                    return NotFound($"Task step with ID {completeStepDto.TaskStepId} not found");
                }

                if (taskStep.Status == Status.Done)
                {
                    return BadRequest("Task step is already completed");
                }

                // Complete the step
                taskStep.Status = Status.Done;
                taskStep.CompletedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(completeStepDto.Notes))
                {
                    taskStep.Notes = completeStepDto.Notes;
                }

                // Check if task should be completed
                if (taskStep.IsTerminal)
                {
                    // Terminal step completed - task is done
                    task.Status = Status.Done;
                    task.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    // Check if all steps in the current path are completed
                    var allStepsInPathCompleted = IsTaskPathCompleted(task.TaskSteps);
                    if (allStepsInPathCompleted)
                    {
                        task.Status = Status.Done;
                        task.CompletedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        task.Status = Status.InProgress;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Task step completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task step {TaskStepId} for task {TaskId}", completeStepDto.TaskStepId, id);
                return StatusCode(500, "An error occurred while completing the task step");
            }
        }

        // POST: api/CustomTask/{id}/make-decision
        [HttpPost("{id}/make-decision")]
        public async Task<ActionResult> MakeDecision(Guid id, MakeDecisionDto makeDecisionDto)
        {
            try
            {
                if (id != makeDecisionDto.TaskId)
                {
                    return BadRequest("Task ID mismatch");
                }

                var task = await _context.CustomTasks
                    .Include(t => t.TaskSteps)
                    .FirstOrDefaultAsync(t => t.TaskId == id);

                if (task == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                var taskStep = task.TaskSteps.FirstOrDefault(ts => ts.TaskStepId == makeDecisionDto.TaskStepId);
                if (taskStep == null)
                {
                    return NotFound($"Task step with ID {makeDecisionDto.TaskStepId} not found");
                }

                if (!taskStep.IsDecision)
                {
                    return BadRequest("This task step is not a decision step");
                }

                if (taskStep.Status == Status.Done)
                {
                    return BadRequest("Decision has already been made for this step");
                }

                // Record the decision
                taskStep.DecisionAnswer = makeDecisionDto.DecisionAnswer;
                taskStep.Status = Status.Done;
                taskStep.CompletedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(makeDecisionDto.Notes))
                {
                    taskStep.Notes = makeDecisionDto.Notes;
                }

                // Determine next step after decision
                Guid? nextStepId = makeDecisionDto.DecisionAnswer == "Yes" ? taskStep.NextStepIfYes : taskStep.NextStepIfNo;
                
                // Check if the next step is terminal and if task should be completed
                if (nextStepId.HasValue)
                {
                    var nextStep = task.TaskSteps.FirstOrDefault(ts => ts.TaskStepId == nextStepId.Value);
                    if (nextStep != null && nextStep.IsTerminal)
                    {
                        // If the next step is terminal and auto-completed, complete the task
                        if (nextStep.Status == Status.Done)
                        {
                            task.Status = Status.Done;
                            task.CompletedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            task.Status = Status.InProgress;
                        }
                    }
                    else
                    {
                        task.Status = Status.InProgress;
                    }
                }
                else
                {
                    // No next step - this decision might be terminal itself
                    task.Status = Status.InProgress;
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Decision recorded successfully", 
                    decision = makeDecisionDto.DecisionAnswer,
                    nextStepId = makeDecisionDto.DecisionAnswer == "Yes" ? taskStep.NextStepIfYes : taskStep.NextStepIfNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making decision for task step {TaskStepId} in task {TaskId}", makeDecisionDto.TaskStepId, id);
                return StatusCode(500, "An error occurred while making the decision");
            }
        }

        // PUT: api/CustomTask/{id}/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateTaskStatus(Guid id, [FromBody] Status newStatus)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid task ID");
                }

                var task = await _context.CustomTasks.FindAsync(id);
                if (task == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                task.Status = newStatus;
                if (newStatus == Status.Done)
                {
                    task.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Task status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for task {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the task status");
            }
        }

        // POST: api/CustomTask/{id}/notes
        [HttpPost("{id}/notes")]
        public async Task<ActionResult<TaskNoteResponseDto>> AddTaskNote(Guid id, CreateTaskNoteDto createNoteDto)
        {
            try
            {
                if (id != createNoteDto.TaskId)
                {
                    return BadRequest("Task ID mismatch");
                }

                var taskExists = await _context.CustomTasks.AnyAsync(t => t.TaskId == id);
                if (!taskExists)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                var taskNote = new TaskNote
                {
                    TaskNoteId = Guid.NewGuid(),
                    TaskId = createNoteDto.TaskId,
                    Content = createNoteDto.Content,
                    CreatedBy = createNoteDto.CreatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TaskNotes.Add(taskNote);
                await _context.SaveChangesAsync();

                var responseDto = new TaskNoteResponseDto
                {
                    TaskNoteId = taskNote.TaskNoteId,
                    Content = taskNote.Content,
                    CreatedAt = taskNote.CreatedAt,
                    UpdatedAt = taskNote.UpdatedAt,
                    CreatedBy = taskNote.CreatedBy
                };

                return CreatedAtAction("GetTaskNote", new { id = taskNote.TaskNoteId }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note to task {TaskId}", id);
                return StatusCode(500, "An error occurred while adding the note");
            }
        }

        private static Guid? GetCurrentStepId(List<TaskStepResponseDto> taskSteps)
        {
            if (!taskSteps.Any())
                return null;

            // Start from the first step (order 0 or 1)
            var firstStep = taskSteps.OrderBy(ts => ts.Order).First();
            
            // Follow the decision tree path
            return FollowDecisionTreePath(firstStep, taskSteps);
        }

        private static Guid? FollowDecisionTreePath(TaskStepResponseDto currentStep, List<TaskStepResponseDto> allSteps)
        {
            // If current step is not completed, this is the current step
            if (currentStep.Status != Status.Done)
                return currentStep.TaskStepId;

            // If current step is terminal and completed, task should be done
            if (currentStep.IsTerminal && currentStep.Status == Status.Done)
                return null; // No current step - task is complete

            // If it's a completed decision step, follow the decision path
            if (currentStep.IsDecision && currentStep.Status == Status.Done)
            {
                Guid? nextStepId = null;
                
                if (currentStep.DecisionAnswer == "Yes" && currentStep.NextStepIfYes.HasValue)
                    nextStepId = currentStep.NextStepIfYes;
                else if (currentStep.DecisionAnswer == "No" && currentStep.NextStepIfNo.HasValue)
                    nextStepId = currentStep.NextStepIfNo;

                if (nextStepId.HasValue)
                {
                    var nextStep = allSteps.FirstOrDefault(s => s.TaskStepId == nextStepId.Value);
                    if (nextStep != null)
                        return FollowDecisionTreePath(nextStep, allSteps);
                }
            }
            
            // If it's a completed non-decision step, find the next step in the sequence
            // This handles linear sequences within the decision tree
            var nextInSequence = allSteps
                .Where(s => s.Order == currentStep.Order + 1)
                .FirstOrDefault();
                
            return nextInSequence?.TaskStepId;
        }

        private static bool IsTaskPathCompleted(ICollection<TaskStep> taskSteps)
        {
            if (!taskSteps.Any())
                return false;

            // Convert to response DTOs for path following logic
            var stepDtos = taskSteps.Select(ts => new TaskStepResponseDto
            {
                TaskStepId = ts.TaskStepId,
                Status = ts.Status,
                Order = ts.Order,
                IsDecision = ts.IsDecision,
                IsTerminal = ts.IsTerminal,
                DecisionAnswer = ts.DecisionAnswer,
                NextStepIfYes = ts.NextStepIfYes,
                NextStepIfNo = ts.NextStepIfNo
            }).ToList();

            // Check if we've reached a terminal step in the current path
            var currentStepId = GetCurrentStepId(stepDtos);
            
            // If no current step, it means we've completed the path
            return currentStepId == null;
        }

        private static int GetActualCompletedStepsCount(ICollection<TaskStep> taskSteps)
        {
            if (!taskSteps.Any())
                return 0;

            // Get the actual path taken and count completed steps in that path
            var pathSteps = GetActualPathSteps(taskSteps);
            return pathSteps.Count(s => s.Status == Status.Done);
        }

        private static int GetActualTotalStepsCount(ICollection<TaskStep> taskSteps)
        {
            if (!taskSteps.Any())
                return 0;

            // Get the actual path that should be taken
            var pathSteps = GetActualPathSteps(taskSteps);
            return pathSteps.Count;
        }

        private static List<TaskStep> GetActualPathSteps(ICollection<TaskStep> allSteps)
        {
            var pathSteps = new List<TaskStep>();
            if (!allSteps.Any())
                return pathSteps;

            // Start from the first step
            var currentStep = allSteps.OrderBy(s => s.Order).First();
            
            while (currentStep != null)
            {
                pathSteps.Add(currentStep);
                
                // If this is a terminal step, we're done
                if (currentStep.IsTerminal)
                    break;
                    
                // If this is a decision step that's been completed, follow the decision path
                if (currentStep.IsDecision && currentStep.Status == Status.Done)
                {
                    Guid? nextStepId = null;
                    if (currentStep.DecisionAnswer == "Yes" && currentStep.NextStepIfYes.HasValue)
                        nextStepId = currentStep.NextStepIfYes;
                    else if (currentStep.DecisionAnswer == "No" && currentStep.NextStepIfNo.HasValue)
                        nextStepId = currentStep.NextStepIfNo;
                        
                    if (nextStepId.HasValue)
                        currentStep = allSteps.FirstOrDefault(s => s.TaskStepId == nextStepId.Value);
                    else
                        break;
                }
                // If this is a decision step that hasn't been completed, include both possible paths
                else if (currentStep.IsDecision && currentStep.Status != Status.Done)
                {
                    // Include both possible next steps in the count
                    if (currentStep.NextStepIfYes.HasValue)
                    {
                        var yesStep = allSteps.FirstOrDefault(s => s.TaskStepId == currentStep.NextStepIfYes.Value);
                        if (yesStep != null)
                            pathSteps.Add(yesStep);
                    }
                    if (currentStep.NextStepIfNo.HasValue)
                    {
                        var noStep = allSteps.FirstOrDefault(s => s.TaskStepId == currentStep.NextStepIfNo.Value);
                        if (noStep != null && (currentStep.NextStepIfYes != currentStep.NextStepIfNo))
                            pathSteps.Add(noStep);
                    }
                    break;
                }
                // For regular completed steps, find the next step in sequence
                else if (currentStep.Status == Status.Done)
                {
                    var nextStep = allSteps
                        .Where(s => s.Order == currentStep.Order + 1)
                        .FirstOrDefault();
                    currentStep = nextStep;
                }
                else
                {
                    // Current step is not completed, stop here
                    break;
                }
            }
            
            return pathSteps;
        }

        private async Task<bool> CustomTaskExists(Guid id)
        {
            return await _context.CustomTasks.AnyAsync(e => e.TaskId == id);
        }
    }
}