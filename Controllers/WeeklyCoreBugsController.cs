// Controllers/WeeklyCoreBugsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.DTOs;
using BugTracker.Models.Enums;
using BugTracker.Services;

namespace BugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeeklyCoreBugsController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<WeeklyCoreBugsController> _logger;
        private readonly ExcelReportService _excelReportService;

        public WeeklyCoreBugsController(BugTrackerContext context, ILogger<WeeklyCoreBugsController> logger, ExcelReportService excelReportService)
        {
            _context = context;
            _logger = logger;
            _excelReportService = excelReportService;
        }

        // GET: api/WeeklyCoreBugs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeeklyCoreBugsResponseDto>>> GetWeeklyCoreBugs(
            [FromQuery] Status? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.WeeklyCoreBugs
                    .Include(wcb => wcb.WeeklyCoreBugEntries)
                        .ThenInclude(wce => wce.CoreBug)
                            .ThenInclude(cb => cb.Tasks)
                    .AsQueryable();

                // Apply filters
                if (status.HasValue)
                    query = query.Where(wcb => wcb.Status == status.Value);

                if (fromDate.HasValue)
                    query = query.Where(wcb => wcb.WeekStartDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(wcb => wcb.WeekEndDate <= toDate.Value);

                var weeklyCoreBugsData = await query
                    .Select(wcb => new 
                    {
                        wcb.WeeklyCoreBugsId,
                        wcb.Name,
                        wcb.WeekStartDate,
                        wcb.WeekEndDate,
                        wcb.Status,
                        wcb.CreatedAt,
                        wcb.CompletedAt,
                        WeeklyCoreBugEntries = wcb.WeeklyCoreBugEntries.Select(wce => new WeeklyCoreBugEntryDto
                        {
                            WeeklyCoreBugEntryId = wce.WeeklyCoreBugEntryId,
                            WeeklyCoreBugsId = wce.WeeklyCoreBugsId,
                            BugId = wce.BugId,
                            CoreBug = wce.CoreBug != null ? new CoreBugSummaryDto
                            {
                                BugId = wce.CoreBug.BugId,
                                BugTitle = wce.CoreBug.BugTitle,
                                JiraKey = wce.CoreBug.JiraKey,
                                JiraLink = wce.CoreBug.JiraLink,
                                Status = wce.CoreBug.Status,
                                Severity = wce.CoreBug.Severity,
                                IsAssessed = wce.CoreBug.IsAssessed,
                                AssessedProductType = wce.CoreBug.AssessedProductType,
                                CreatedAt = wce.CoreBug.CreatedAt,
                                TaskCount = wce.CoreBug.Tasks.Count,
                                CompletedTaskCount = wce.CoreBug.Tasks.Count(t => t.Status == Status.Done),
                                Tasks = wce.CoreBug.Tasks.Select(t => new TaskSummaryDto
                                {
                                    TaskId = t.TaskId,
                                    TaskTitle = t.TaskTitle,
                                    Status = t.Status.ToString(),
                                    CreatedAt = t.CreatedAt,
                                    CompletedAt = t.CompletedAt
                                }).ToList()
                            } : null
                        }).ToList(),
                        TotalBugsCount = wcb.WeeklyCoreBugEntries.Count,
                        AssessedBugsCount = wcb.WeeklyCoreBugEntries.Count(wce => wce.CoreBug.IsAssessed),
                        TotalTasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(),
                        CompletedTasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(t => t.Status == Status.Done),
                        InProgressTasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(t => t.Status == Status.InProgress)
                    })
                    .OrderByDescending(wcb => wcb.WeekStartDate)
                    .ToListAsync();

                // Convert to response DTOs after database query
                var weeklyCoreBugs = weeklyCoreBugsData.Select(wcb => new WeeklyCoreBugsResponseDto
                {
                    WeeklyCoreBugsId = wcb.WeeklyCoreBugsId,
                    Name = wcb.Name,
                    WeekStartDate = wcb.WeekStartDate,
                    WeekEndDate = wcb.WeekEndDate,
                    Status = wcb.Status,
                    CreatedAt = wcb.CreatedAt,
                    CompletedAt = wcb.CompletedAt,
                    WeeklyCoreBugEntries = wcb.WeeklyCoreBugEntries,
                    TotalBugsCount = wcb.TotalBugsCount,
                    AssessedBugsCount = wcb.AssessedBugsCount,
                    UnassessedBugsCount = wcb.TotalBugsCount - wcb.AssessedBugsCount,
                    TotalTasksCount = wcb.TotalTasksCount,
                    CompletedTasksCount = wcb.CompletedTasksCount,
                    InProgressTasksCount = wcb.InProgressTasksCount,
                    CompletionPercentage = wcb.TotalTasksCount > 0 ? Math.Round((double)wcb.CompletedTasksCount / wcb.TotalTasksCount * 100, 1) : 0
                }).ToList();

                return Ok(weeklyCoreBugs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weekly core bugs");
                return StatusCode(500, "An error occurred while retrieving weekly core bugs");
            }
        }

        // GET: api/WeeklyCoreBugs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WeeklyCoreBugsResponseDto>> GetWeeklyCoreBugs(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid weekly core bugs ID");
                }

                var weeklyCoreBugsData = await _context.WeeklyCoreBugs
                    .Include(wcb => wcb.WeeklyCoreBugEntries)
                        .ThenInclude(wce => wce.CoreBug)
                            .ThenInclude(cb => cb.Tasks)
                    .Where(wcb => wcb.WeeklyCoreBugsId == id)
                    .Select(wcb => new 
                    {
                        wcb.WeeklyCoreBugsId,
                        wcb.Name,
                        wcb.WeekStartDate,
                        wcb.WeekEndDate,
                        wcb.Status,
                        wcb.CreatedAt,
                        wcb.CompletedAt,
                        WeeklyCoreBugEntries = wcb.WeeklyCoreBugEntries.Select(wce => new WeeklyCoreBugEntryDto
                        {
                            WeeklyCoreBugEntryId = wce.WeeklyCoreBugEntryId,
                            WeeklyCoreBugsId = wce.WeeklyCoreBugsId,
                            BugId = wce.BugId,
                            CoreBug = wce.CoreBug != null ? new CoreBugSummaryDto
                            {
                                BugId = wce.CoreBug.BugId,
                                BugTitle = wce.CoreBug.BugTitle,
                                JiraKey = wce.CoreBug.JiraKey,
                                JiraLink = wce.CoreBug.JiraLink,
                                Status = wce.CoreBug.Status,
                                Severity = wce.CoreBug.Severity,
                                IsAssessed = wce.CoreBug.IsAssessed,
                                AssessedProductType = wce.CoreBug.AssessedProductType,
                                CreatedAt = wce.CoreBug.CreatedAt,
                                TaskCount = wce.CoreBug.Tasks.Count,
                                CompletedTaskCount = wce.CoreBug.Tasks.Count(t => t.Status == Status.Done),
                                Tasks = wce.CoreBug.Tasks.Select(t => new TaskSummaryDto
                                {
                                    TaskId = t.TaskId,
                                    TaskTitle = t.TaskTitle,
                                    Status = t.Status.ToString(),
                                    CreatedAt = t.CreatedAt,
                                    CompletedAt = t.CompletedAt
                                }).ToList()
                            } : null
                        }).ToList(),
                        TotalBugsCount = wcb.WeeklyCoreBugEntries.Count,
                        AssessedBugsCount = wcb.WeeklyCoreBugEntries.Count(wce => wce.CoreBug.IsAssessed),
                        TotalTasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(),
                        CompletedTasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(t => t.Status == Status.Done),
                        InProgressTasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(t => t.Status == Status.InProgress)
                    })
                    .FirstOrDefaultAsync();

                if (weeklyCoreBugsData == null)
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }

                // Convert to response DTO after database query
                var weeklyCoreBugs = new WeeklyCoreBugsResponseDto
                {
                    WeeklyCoreBugsId = weeklyCoreBugsData.WeeklyCoreBugsId,
                    Name = weeklyCoreBugsData.Name,
                    WeekStartDate = weeklyCoreBugsData.WeekStartDate,
                    WeekEndDate = weeklyCoreBugsData.WeekEndDate,
                    Status = weeklyCoreBugsData.Status,
                    CreatedAt = weeklyCoreBugsData.CreatedAt,
                    CompletedAt = weeklyCoreBugsData.CompletedAt,
                    WeeklyCoreBugEntries = weeklyCoreBugsData.WeeklyCoreBugEntries,
                    TotalBugsCount = weeklyCoreBugsData.TotalBugsCount,
                    AssessedBugsCount = weeklyCoreBugsData.AssessedBugsCount,
                    UnassessedBugsCount = weeklyCoreBugsData.TotalBugsCount - weeklyCoreBugsData.AssessedBugsCount,
                    TotalTasksCount = weeklyCoreBugsData.TotalTasksCount,
                    CompletedTasksCount = weeklyCoreBugsData.CompletedTasksCount,
                    InProgressTasksCount = weeklyCoreBugsData.InProgressTasksCount,
                    CompletionPercentage = weeklyCoreBugsData.TotalTasksCount > 0 ? Math.Round((double)weeklyCoreBugsData.CompletedTasksCount / weeklyCoreBugsData.TotalTasksCount * 100, 1) : 0
                };

                return Ok(weeklyCoreBugs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while retrieving the weekly core bugs");
            }
        }

        // POST: api/WeeklyCoreBugs
        [HttpPost]
        public async Task<ActionResult<WeeklyCoreBugsResponseDto>> PostWeeklyCoreBugs(CreateWeeklyCoreBugsDto createWeeklyCoreBugsDto)
        {
            try
            {
                // Validate date range
                if (createWeeklyCoreBugsDto.WeekEndDate <= createWeeklyCoreBugsDto.WeekStartDate)
                {
                    return BadRequest("Week end date must be after week start date");
                }

                // Check for overlapping weeks
                var overlappingWeek = await _context.WeeklyCoreBugs
                    .AnyAsync(wcb => (createWeeklyCoreBugsDto.WeekStartDate >= wcb.WeekStartDate && createWeeklyCoreBugsDto.WeekStartDate <= wcb.WeekEndDate) ||
                                    (createWeeklyCoreBugsDto.WeekEndDate >= wcb.WeekStartDate && createWeeklyCoreBugsDto.WeekEndDate <= wcb.WeekEndDate));

                if (overlappingWeek)
                {
                    return Conflict("A weekly core bugs entry already exists for this date range");
                }

                var weeklyCoreBugs = new WeeklyCoreBugs
                {
                    WeeklyCoreBugsId = Guid.NewGuid(),
                    Name = createWeeklyCoreBugsDto.Name,
                    WeekStartDate = createWeeklyCoreBugsDto.WeekStartDate,
                    WeekEndDate = createWeeklyCoreBugsDto.WeekEndDate,
                    Status = Status.New,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WeeklyCoreBugs.Add(weeklyCoreBugs);

                // Add bug entries if provided
                if (createWeeklyCoreBugsDto.BugIds?.Any() == true)
                {
                    var validBugIds = await _context.CoreBugs
                        .Where(cb => createWeeklyCoreBugsDto.BugIds.Contains(cb.BugId))
                        .Select(cb => cb.BugId)
                        .ToListAsync();

                    foreach (var bugId in validBugIds)
                    {
                        var entry = new WeeklyCoreBugEntry
                        {
                            WeeklyCoreBugEntryId = Guid.NewGuid(),
                            WeeklyCoreBugsId = weeklyCoreBugs.WeeklyCoreBugsId,
                            BugId = bugId
                        };
                        _context.WeeklyCoreBugEntries.Add(entry);
                    }
                }

                await _context.SaveChangesAsync();

                var responseDto = new WeeklyCoreBugsResponseDto
                {
                    WeeklyCoreBugsId = weeklyCoreBugs.WeeklyCoreBugsId,
                    Name = weeklyCoreBugs.Name,
                    WeekStartDate = weeklyCoreBugs.WeekStartDate,
                    WeekEndDate = weeklyCoreBugs.WeekEndDate,
                    Status = weeklyCoreBugs.Status,
                    CreatedAt = weeklyCoreBugs.CreatedAt,
                    CompletedAt = weeklyCoreBugs.CompletedAt,
                    WeeklyCoreBugEntries = new List<WeeklyCoreBugEntryDto>(),
                    TotalBugsCount = createWeeklyCoreBugsDto.BugIds?.Count ?? 0,
                    AssessedBugsCount = 0,
                    UnassessedBugsCount = createWeeklyCoreBugsDto.BugIds?.Count ?? 0,
                    TotalTasksCount = 0,
                    CompletedTasksCount = 0,
                    InProgressTasksCount = 0,
                    CompletionPercentage = 0
                };

                return CreatedAtAction("GetWeeklyCoreBugs", new { id = weeklyCoreBugs.WeeklyCoreBugsId }, responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating weekly core bugs");
                return StatusCode(500, "An error occurred while creating the weekly core bugs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating weekly core bugs");
                return StatusCode(500, "An error occurred while creating the weekly core bugs");
            }
        }

        // PUT: api/WeeklyCoreBugs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWeeklyCoreBugs(Guid id, UpdateWeeklyCoreBugsDto updateWeeklyCoreBugsDto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid weekly core bugs ID");
                }

                var weeklyCoreBugs = await _context.WeeklyCoreBugs.FindAsync(id);
                if (weeklyCoreBugs == null)
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }

                // Validate date range
                if (updateWeeklyCoreBugsDto.WeekEndDate <= updateWeeklyCoreBugsDto.WeekStartDate)
                {
                    return BadRequest("Week end date must be after week start date");
                }

                weeklyCoreBugs.Name = updateWeeklyCoreBugsDto.Name;
                weeklyCoreBugs.WeekStartDate = updateWeeklyCoreBugsDto.WeekStartDate;
                weeklyCoreBugs.WeekEndDate = updateWeeklyCoreBugsDto.WeekEndDate;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating weekly core bugs {WeeklyCoreBugsId}", id);
                
                if (!await WeeklyCoreBugsExists(id))
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }
                else
                {
                    return Conflict("The weekly core bugs was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while updating the weekly core bugs");
            }
        }

        // POST: api/WeeklyCoreBugs/{id}/add-bugs
        [HttpPost("{id}/add-bugs")]
        public async Task<ActionResult> AddBugsToWeekly(Guid id, AddBugsToWeeklyDto addBugsDto)
        {
            try
            {
                if (id != addBugsDto.WeeklyCoreBugsId)
                {
                    return BadRequest("Weekly core bugs ID mismatch");
                }

                var weeklyCoreBugs = await _context.WeeklyCoreBugs.FindAsync(id);
                if (weeklyCoreBugs == null)
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }

                // Get existing bug IDs to avoid duplicates
                var existingBugIds = await _context.WeeklyCoreBugEntries
                    .Where(wce => wce.WeeklyCoreBugsId == id)
                    .Select(wce => wce.BugId)
                    .ToListAsync();

                // Validate bugs exist and are not already added
                var validBugIds = await _context.CoreBugs
                    .Where(cb => addBugsDto.BugIds.Contains(cb.BugId) && !existingBugIds.Contains(cb.BugId))
                    .Select(cb => cb.BugId)
                    .ToListAsync();

                if (!validBugIds.Any())
                {
                    return BadRequest("No valid bugs to add (bugs may not exist or already be included)");
                }

                // Add new entries
                foreach (var bugId in validBugIds)
                {
                    var entry = new WeeklyCoreBugEntry
                    {
                        WeeklyCoreBugEntryId = Guid.NewGuid(),
                        WeeklyCoreBugsId = id,
                        BugId = bugId
                    };
                    _context.WeeklyCoreBugEntries.Add(entry);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Successfully added {validBugIds.Count} bugs to weekly core bugs", addedCount = validBugIds.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bugs to weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while adding bugs to weekly core bugs");
            }
        }

        // DELETE: api/WeeklyCoreBugs/{id}/remove-bugs
        [HttpDelete("{id}/remove-bugs")]
        public async Task<ActionResult> RemoveBugsFromWeekly(Guid id, RemoveBugsFromWeeklyDto removeBugsDto)
        {
            try
            {
                if (id != removeBugsDto.WeeklyCoreBugsId)
                {
                    return BadRequest("Weekly core bugs ID mismatch");
                }

                var weeklyCoreBugs = await _context.WeeklyCoreBugs.FindAsync(id);
                if (weeklyCoreBugs == null)
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }

                // Find entries to remove
                var entriesToRemove = await _context.WeeklyCoreBugEntries
                    .Where(wce => wce.WeeklyCoreBugsId == id && removeBugsDto.BugIds.Contains(wce.BugId))
                    .ToListAsync();

                if (!entriesToRemove.Any())
                {
                    return BadRequest("No matching bugs found to remove");
                }

                _context.WeeklyCoreBugEntries.RemoveRange(entriesToRemove);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Successfully removed {entriesToRemove.Count} bugs from weekly core bugs", removedCount = entriesToRemove.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing bugs from weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while removing bugs from weekly core bugs");
            }
        }

        // PUT: api/WeeklyCoreBugs/{id}/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateWeeklyCoreBugsStatus(Guid id, [FromBody] Status newStatus)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid weekly core bugs ID");
                }

                var weeklyCoreBugs = await _context.WeeklyCoreBugs.FindAsync(id);
                if (weeklyCoreBugs == null)
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }

                weeklyCoreBugs.Status = newStatus;
                if (newStatus == Status.Done)
                {
                    weeklyCoreBugs.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Weekly core bugs status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while updating the weekly core bugs status");
            }
        }

        // GET: api/WeeklyCoreBugs/{id}/excel-report
        [HttpGet("{id}/excel-report")]
        public async Task<ActionResult> GenerateExcelReport(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid weekly core bugs ID");
                }

                var weeklyCoreBugs = await _context.WeeklyCoreBugs
                    .Include(wcb => wcb.WeeklyCoreBugEntries)
                        .ThenInclude(wce => wce.CoreBug)
                            .ThenInclude(cb => cb.Tasks)
                                .ThenInclude(t => t.TrialManager)
                                    .ThenInclude(tm => tm.Client)
                    .Include(wcb => wcb.WeeklyCoreBugEntries)
                        .ThenInclude(wce => wce.CoreBug)
                            .ThenInclude(cb => cb.Tasks)
                                .ThenInclude(t => t.InteractiveResponseTechnology)
                                    .ThenInclude(irt => irt.Study)
                    .FirstOrDefaultAsync(wcb => wcb.WeeklyCoreBugsId == id);

                if (weeklyCoreBugs == null)
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }

                // Generate Excel report
                var excelBytes = await _excelReportService.GenerateWeeklyCoreBugsReport(weeklyCoreBugs);

                var fileName = $"WeeklyCoreBugs_{weeklyCoreBugs.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(excelBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel report for weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while generating the Excel report");
            }
        }

        // DELETE: api/WeeklyCoreBugs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWeeklyCoreBugs(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid weekly core bugs ID");
                }

                var weeklyCoreBugs = await _context.WeeklyCoreBugs
                    .Include(wcb => wcb.WeeklyCoreBugEntries)
                    .FirstOrDefaultAsync(wcb => wcb.WeeklyCoreBugsId == id);

                if (weeklyCoreBugs == null)
                {
                    return NotFound($"Weekly core bugs with ID {id} not found");
                }

                // Remove all entries first (cascade delete should handle this, but being explicit)
                _context.WeeklyCoreBugEntries.RemoveRange(weeklyCoreBugs.WeeklyCoreBugEntries);
                _context.WeeklyCoreBugs.Remove(weeklyCoreBugs);
                
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while deleting the weekly core bugs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting weekly core bugs {WeeklyCoreBugsId}", id);
                return StatusCode(500, "An error occurred while deleting the weekly core bugs");
            }
        }

        // GET: api/WeeklyCoreBugs/current-week
        [HttpGet("current-week")]
        public async Task<ActionResult<WeeklyCoreBugsResponseDto>> GetCurrentWeekCoreBugs()
        {
            try
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Start of current week (Sunday)
                var endOfWeek = startOfWeek.AddDays(6); // End of current week (Saturday)

                var currentWeek = await _context.WeeklyCoreBugs
                    .Include(wcb => wcb.WeeklyCoreBugEntries)
                        .ThenInclude(wce => wce.CoreBug)
                            .ThenInclude(cb => cb.Tasks)
                    .Where(wcb => wcb.WeekStartDate <= today && wcb.WeekEndDate >= today)
                    .FirstOrDefaultAsync();

                if (currentWeek == null)
                {
                    return NotFound("No weekly core bugs found for the current week");
                }

                // Use the same logic as GetWeeklyCoreBugs(id) to build response
                return await GetWeeklyCoreBugs(currentWeek.WeeklyCoreBugsId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current week core bugs");
                return StatusCode(500, "An error occurred while retrieving current week core bugs");
            }
        }

        // GET: api/WeeklyCoreBugs/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetWeeklyStatistics()
        {
            try
            {
                var stats = await _context.WeeklyCoreBugs
                    .Include(wcb => wcb.WeeklyCoreBugEntries)
                        .ThenInclude(wce => wce.CoreBug)
                            .ThenInclude(cb => cb.Tasks)
                    .Select(wcb => new
                    {
                        wcb.WeeklyCoreBugsId,
                        wcb.Name,
                        wcb.WeekStartDate,
                        wcb.Status,
                        BugsCount = wcb.WeeklyCoreBugEntries.Count,
                        TasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(),
                        CompletedTasksCount = wcb.WeeklyCoreBugEntries.SelectMany(wce => wce.CoreBug.Tasks).Count(t => t.Status == Status.Done)
                    })
                    .ToListAsync();

                var summary = new
                {
                    TotalWeeks = stats.Count,
                    CompletedWeeks = stats.Count(s => s.Status == Status.Done),
                    InProgressWeeks = stats.Count(s => s.Status == Status.InProgress),
                    TotalBugs = stats.Sum(s => s.BugsCount),
                    TotalTasks = stats.Sum(s => s.TasksCount),
                    CompletedTasks = stats.Sum(s => s.CompletedTasksCount),
                    OverallCompletionRate = stats.Sum(s => s.TasksCount) > 0 ? 
                        Math.Round((double)stats.Sum(s => s.CompletedTasksCount) / stats.Sum(s => s.TasksCount) * 100, 1) : 0,
                    WeeklyBreakdown = stats.OrderByDescending(s => s.WeekStartDate).Take(10).ToList()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weekly statistics");
                return StatusCode(500, "An error occurred while retrieving weekly statistics");
            }
        }

        private async Task<bool> WeeklyCoreBugsExists(Guid id)
        {
            return await _context.WeeklyCoreBugs.AnyAsync(e => e.WeeklyCoreBugsId == id);
        }
    }
}