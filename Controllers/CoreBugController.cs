// Controllers/CoreBugController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.DTOs;
using BugTracker.Models.Enums;
using BugTracker.Services;
using System.Text.Json;

namespace BugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoreBugController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<CoreBugController> _logger;
        private readonly TaskGenerationService _taskGenerationService;

        public CoreBugController(BugTrackerContext context, ILogger<CoreBugController> logger, TaskGenerationService taskGenerationService)
        {
            _context = context;
            _logger = logger;
            _taskGenerationService = taskGenerationService;
        }

        // GET: api/CoreBug
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CoreBugResponseDto>>> GetCoreBugs(
            [FromQuery] Status? status = null,
            [FromQuery] bool? isAssessed = null,
            [FromQuery] BugSeverity? severity = null,
            [FromQuery] ProductType? assessedProductType = null)
        {
            try
            {
                var query = _context.CoreBugs
                    .Include(cb => cb.Tasks)
                    .AsQueryable();

                // Apply filters
                if (status.HasValue)
                    query = query.Where(cb => cb.Status == status.Value);

                if (isAssessed.HasValue)
                    query = query.Where(cb => cb.IsAssessed == isAssessed.Value);

                if (severity.HasValue)
                    query = query.Where(cb => cb.Severity == severity.Value);

                if (assessedProductType.HasValue)
                    query = query.Where(cb => cb.AssessedProductType == assessedProductType.Value);

                var coreBugsData = await query
                    .Select(cb => new 
                    {
                        cb.BugId,
                        cb.BugTitle,
                        cb.JiraKey,
                        cb.JiraLink,
                        cb.BugDescription,
                        cb.Status,
                        cb.FoundInBuild,
                        cb.AffectedVersions,
                        cb.Severity,
                        cb.AssessedProductType,
                        cb.AssessedImpactedVersions,
                        cb.IsAssessed,
                        cb.AssessedAt,
                        cb.AssessedBy,
                        cb.CreatedAt,
                        cb.ResolvedAt,
                        Tasks = cb.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList(),
                        TaskCount = cb.Tasks.Count,
                        CompletedTaskCount = cb.Tasks.Count(t => t.Status == Status.Done)
                    })
                    .OrderByDescending(cb => cb.CreatedAt)
                    .ToListAsync();

                // Convert to response DTOs after database query
                var coreBugs = coreBugsData.Select(cb => new CoreBugResponseDto
                {
                    BugId = cb.BugId,
                    BugTitle = cb.BugTitle,
                    JiraKey = cb.JiraKey,
                    JiraLink = cb.JiraLink,
                    BugDescription = cb.BugDescription,
                    Status = cb.Status,
                    FoundInBuild = cb.FoundInBuild,
                    AffectedVersions = string.IsNullOrEmpty(cb.AffectedVersions) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(cb.AffectedVersions),
                    Severity = cb.Severity,
                    AssessedProductType = cb.AssessedProductType,
                    AssessedImpactedVersions = string.IsNullOrEmpty(cb.AssessedImpactedVersions) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(cb.AssessedImpactedVersions),
                    IsAssessed = cb.IsAssessed,
                    AssessedAt = cb.AssessedAt,
                    AssessedBy = cb.AssessedBy,
                    CreatedAt = cb.CreatedAt,
                    ResolvedAt = cb.ResolvedAt,
                    Tasks = cb.Tasks,
                    TaskCount = cb.TaskCount,
                    CompletedTaskCount = cb.CompletedTaskCount
                }).ToList();

                return Ok(coreBugs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving core bugs");
                return StatusCode(500, "An error occurred while retrieving core bugs");
            }
        }

        // GET: api/CoreBug/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CoreBugResponseDto>> GetCoreBug(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid bug ID");
                }

                var coreBugData = await _context.CoreBugs
                    .Include(cb => cb.Tasks)
                        .ThenInclude(t => t.Study)
                    .Include(cb => cb.Tasks)
                        .ThenInclude(t => t.TrialManager)
                            .ThenInclude(tm => tm.Client)
                    .Include(cb => cb.Tasks)
                        .ThenInclude(t => t.InteractiveResponseTechnology)
                    .Where(cb => cb.BugId == id)
                    .Select(cb => new 
                    {
                        cb.BugId,
                        cb.BugTitle,
                        cb.JiraKey,
                        cb.JiraLink,
                        cb.BugDescription,
                        cb.Status,
                        cb.FoundInBuild,
                        cb.AffectedVersions,
                        cb.Severity,
                        cb.AssessedProductType,
                        cb.AssessedImpactedVersions,
                        cb.IsAssessed,
                        cb.AssessedAt,
                        cb.AssessedBy,
                        cb.CreatedAt,
                        cb.ResolvedAt,
                        Tasks = cb.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList(),
                        TaskCount = cb.Tasks.Count,
                        CompletedTaskCount = cb.Tasks.Count(t => t.Status == Status.Done)
                    })
                    .FirstOrDefaultAsync();

                if (coreBugData == null)
                {
                    return NotFound($"Core bug with ID {id} not found");
                }

                // Convert to response DTO after database query
                var coreBug = new CoreBugResponseDto
                {
                    BugId = coreBugData.BugId,
                    BugTitle = coreBugData.BugTitle,
                    JiraKey = coreBugData.JiraKey,
                    JiraLink = coreBugData.JiraLink,
                    BugDescription = coreBugData.BugDescription,
                    Status = coreBugData.Status,
                    FoundInBuild = coreBugData.FoundInBuild,
                    AffectedVersions = string.IsNullOrEmpty(coreBugData.AffectedVersions) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(coreBugData.AffectedVersions),
                    Severity = coreBugData.Severity,
                    AssessedProductType = coreBugData.AssessedProductType,
                    AssessedImpactedVersions = string.IsNullOrEmpty(coreBugData.AssessedImpactedVersions) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(coreBugData.AssessedImpactedVersions),
                    IsAssessed = coreBugData.IsAssessed,
                    AssessedAt = coreBugData.AssessedAt,
                    AssessedBy = coreBugData.AssessedBy,
                    CreatedAt = coreBugData.CreatedAt,
                    ResolvedAt = coreBugData.ResolvedAt,
                    Tasks = coreBugData.Tasks,
                    TaskCount = coreBugData.TaskCount,
                    CompletedTaskCount = coreBugData.CompletedTaskCount
                };

                return Ok(coreBug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving core bug {BugId}", id);
                return StatusCode(500, "An error occurred while retrieving the core bug");
            }
        }

        // POST: api/CoreBug
        [HttpPost]
        public async Task<ActionResult<CoreBugResponseDto>> PostCoreBug(CreateCoreBugDto createCoreBugDto)
        {
            try
            {
                // Check for duplicate JIRA key
                if (await _context.CoreBugs.AnyAsync(cb => cb.JiraKey == createCoreBugDto.JiraKey))
                {
                    return Conflict($"A bug with JIRA key '{createCoreBugDto.JiraKey}' already exists");
                }

                var coreBug = new CoreBug
                {
                    BugId = Guid.NewGuid(),
                    BugTitle = createCoreBugDto.BugTitle,
                    JiraKey = createCoreBugDto.JiraKey,
                    JiraLink = createCoreBugDto.JiraLink,
                    BugDescription = createCoreBugDto.BugDescription,
                    Status = Status.New,
                    FoundInBuild = createCoreBugDto.FoundInBuild,
                    AffectedVersions = JsonSerializer.Serialize(createCoreBugDto.AffectedVersions ?? new List<string>()),
                    Severity = createCoreBugDto.Severity,
                    CreatedAt = DateTime.UtcNow,
                    IsAssessed = false
                };

                _context.CoreBugs.Add(coreBug);
                await _context.SaveChangesAsync();

                var responseDto = new CoreBugResponseDto
                {
                    BugId = coreBug.BugId,
                    BugTitle = coreBug.BugTitle,
                    JiraKey = coreBug.JiraKey,
                    JiraLink = coreBug.JiraLink,
                    BugDescription = coreBug.BugDescription,
                    Status = coreBug.Status,
                    FoundInBuild = coreBug.FoundInBuild,
                    AffectedVersions = createCoreBugDto.AffectedVersions ?? new List<string>(),
                    Severity = coreBug.Severity,
                    CreatedAt = coreBug.CreatedAt,
                    IsAssessed = false,
                    Tasks = new List<TaskSummaryDto>(),
                    TaskCount = 0,
                    CompletedTaskCount = 0
                };

                return CreatedAtAction("GetCoreBug", new { id = coreBug.BugId }, responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating core bug");
                return StatusCode(500, "An error occurred while creating the core bug");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating core bug");
                return StatusCode(500, "An error occurred while creating the core bug");
            }
        }

        // PUT: api/CoreBug/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCoreBug(Guid id, UpdateCoreBugDto updateCoreBugDto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid bug ID");
                }

                var coreBug = await _context.CoreBugs.FindAsync(id);
                if (coreBug == null)
                {
                    return NotFound($"Core bug with ID {id} not found");
                }

                // Don't allow updates if bug is assessed (would break task relationships)
                if (coreBug.IsAssessed)
                {
                    return BadRequest("Cannot update an assessed bug. This would affect generated tasks.");
                }

                coreBug.BugTitle = updateCoreBugDto.BugTitle;
                coreBug.BugDescription = updateCoreBugDto.BugDescription;
                coreBug.Severity = updateCoreBugDto.Severity;
                coreBug.FoundInBuild = updateCoreBugDto.FoundInBuild;
                coreBug.AffectedVersions = JsonSerializer.Serialize(updateCoreBugDto.AffectedVersions ?? new List<string>());

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating core bug {BugId}", id);
                
                if (!await CoreBugExists(id))
                {
                    return NotFound($"Core bug with ID {id} not found");
                }
                else
                {
                    return Conflict("The bug was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating core bug {BugId}", id);
                return StatusCode(500, "An error occurred while updating the core bug");
            }
        }

        // POST: api/CoreBug/{id}/assess
        [HttpPost("{id}/assess")]
        public async Task<ActionResult<CoreBugResponseDto>> AssessCoreBug(Guid id, BugAssessmentDto assessmentDto)
        {
            try
            {
                if (id != assessmentDto.BugId)
                {
                    return BadRequest("Bug ID mismatch");
                }

                var coreBug = await _context.CoreBugs
                    .Include(cb => cb.Tasks)
                    .FirstOrDefaultAsync(cb => cb.BugId == id);

                if (coreBug == null)
                {
                    return NotFound($"Core bug with ID {id} not found");
                }

                if (coreBug.IsAssessed)
                {
                    return BadRequest("This bug has already been assessed");
                }

                // Update assessment
                coreBug.AssessedProductType = assessmentDto.AssessedProductType;
                coreBug.AssessedImpactedVersions = JsonSerializer.Serialize(assessmentDto.AssessedImpactedVersions);
                coreBug.IsAssessed = true;
                coreBug.AssessedAt = DateTime.UtcNow;
                coreBug.AssessedBy = assessmentDto.AssessedBy ?? "System";

                // Generate tasks for impacted products
                var generatedTasks = await _taskGenerationService.GenerateTasksForAssessedBug(coreBug);

                // Add generated tasks to context
                _context.CustomTasks.AddRange(generatedTasks);

                await _context.SaveChangesAsync();

                // Return updated bug with new tasks
                var responseDto = new CoreBugResponseDto
                {
                    BugId = coreBug.BugId,
                    BugTitle = coreBug.BugTitle,
                    JiraKey = coreBug.JiraKey,
                    JiraLink = coreBug.JiraLink,
                    BugDescription = coreBug.BugDescription,
                    Status = coreBug.Status,
                    FoundInBuild = coreBug.FoundInBuild,
                    AffectedVersions = string.IsNullOrEmpty(coreBug.AffectedVersions) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(coreBug.AffectedVersions),
                    Severity = coreBug.Severity,
                    AssessedProductType = coreBug.AssessedProductType,
                    AssessedImpactedVersions = string.IsNullOrEmpty(coreBug.AssessedImpactedVersions) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(coreBug.AssessedImpactedVersions),
                    IsAssessed = coreBug.IsAssessed,
                    AssessedAt = coreBug.AssessedAt,
                    AssessedBy = coreBug.AssessedBy,
                    CreatedAt = coreBug.CreatedAt,
                    ResolvedAt = coreBug.ResolvedAt,
                    Tasks = generatedTasks.Select(t => new TaskSummaryDto
                    {
                        TaskId = t.TaskId,
                        TaskTitle = t.TaskTitle,
                        Status = t.Status.ToString(),
                        CreatedAt = t.CreatedAt,
                        CompletedAt = t.CompletedAt
                    }).ToList(),
                    TaskCount = generatedTasks.Count,
                    CompletedTaskCount = 0
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assessing core bug {BugId}", id);
                return StatusCode(500, "An error occurred while assessing the core bug");
            }
        }

        // GET: api/CoreBug/product-versions/{productType}
        [HttpGet("product-versions/{productType}")]
        public async Task<ActionResult<List<string>>> GetProductVersions(ProductType productType)
        {
            try
            {
                List<string> versions;

                if (productType == ProductType.TM)
                {
                    versions = await _context.TrialManagers
                        .Select(tm => tm.Version)
                        .Distinct()
                        .OrderBy(v => v)
                        .ToListAsync();
                }
                else if (productType == ProductType.InteractiveResponseTechnology)
                {
                    versions = await _context.InteractiveResponseTechnologies
                        .Select(irt => irt.Version)
                        .Distinct()
                        .OrderBy(v => v)
                        .ToListAsync();
                }
                else
                {
                    return BadRequest("Invalid product type for version lookup");
                }

                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product versions for {ProductType}", productType);
                return StatusCode(500, "An error occurred while retrieving product versions");
            }
        }

        // POST: api/CoreBug/import-xml
        [HttpPost("import-xml")]
        public async Task<ActionResult<BulkImportResultDto>> ImportBugsFromXml(IFormFile file)
        {
            try
            {
                if (file == null || !file.FileName.EndsWith(".xml"))
                {
                    return BadRequest("Invalid file format. Please upload an XML file.");
                }

                using var reader = new StreamReader(file.OpenReadStream());
                var xmlContent = await reader.ReadToEndAsync();

                // Parse XML and extract bugs (you'll need to implement this)
                var bugs = ParseJiraXml(xmlContent);

                var importedCount = 0;
                var skippedCount = 0;
                var errors = new List<string>();

                foreach (var bugDto in bugs)
                {
                    try
                    {
                        // Check if bug already exists
                        if (await _context.CoreBugs.AnyAsync(cb => cb.JiraKey == bugDto.Key))
                        {
                            skippedCount++;
                            continue;
                        }

                        var coreBug = new CoreBug
                        {
                            BugId = Guid.NewGuid(),
                            JiraKey = bugDto.Key,
                            BugTitle = bugDto.Title,
                            BugDescription = bugDto.Description,
                            Severity = Enum.Parse<BugSeverity>(bugDto.Severity, true),
                            FoundInBuild = bugDto.FoundInBuild,
                            AffectedVersions = JsonSerializer.Serialize(bugDto.AffectedVersions),
                            JiraLink = $"https://jira.company.com/browse/{bugDto.Key}",
                            Status = Status.New,
                            CreatedAt = DateTime.UtcNow,
                            IsAssessed = false
                        };

                        _context.CoreBugs.Add(coreBug);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to import bug {bugDto.Key}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new BulkImportResultDto
                {
                    Success = importedCount > 0,
                    Message = $"Successfully imported {importedCount} bugs, skipped {skippedCount} duplicates",
                    ImportedCount = importedCount,
                    SkippedCount = skippedCount,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing bugs from XML");
                return StatusCode(500, "An error occurred while importing bugs");
            }
        }

        // DELETE: api/CoreBug/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoreBug(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid bug ID");
                }

                var coreBug = await _context.CoreBugs
                    .Include(cb => cb.Tasks)
                    .FirstOrDefaultAsync(cb => cb.BugId == id);

                if (coreBug == null)
                {
                    return NotFound($"Core bug with ID {id} not found");
                }

                // Check if bug has tasks
                if (coreBug.Tasks?.Any() == true)
                {
                    return BadRequest("Cannot delete bug with existing tasks. Complete or delete tasks first.");
                }

                _context.CoreBugs.Remove(coreBug);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting core bug {BugId}", id);
                return StatusCode(500, "An error occurred while deleting the core bug");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting core bug {BugId}", id);
                return StatusCode(500, "An error occurred while deleting the core bug");
            }
        }

        private List<BugImportDto> ParseJiraXml(string xmlContent)
        {
            // TODO: Implement XML parsing logic
            // This should parse your JIRA XML export and extract bug information
            throw new NotImplementedException("XML parsing logic needed");
        }

        private async Task<bool> CoreBugExists(Guid id)
        {
            return await _context.CoreBugs.AnyAsync(e => e.BugId == id);
        }
    }
}