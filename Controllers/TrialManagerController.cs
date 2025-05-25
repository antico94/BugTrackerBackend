// Controllers/TrialManagerController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.DTOs;

namespace BugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrialManagerController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<TrialManagerController> _logger;

        public TrialManagerController(BugTrackerContext context, ILogger<TrialManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/TrialManager
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrialManagerResponseDto>>> GetTrialManagers()
        {
            try
            {
                var trialManagers = await _context.TrialManagers
                    .Include(tm => tm.Client)
                    .Include(tm => tm.Studies)
                    .Include(tm => tm.Tasks)
                    .Select(tm => new TrialManagerResponseDto
                    {
                        TrialManagerId = tm.TrialManagerId,
                        Version = tm.Version,
                        JiraKey = tm.JiraKey,
                        JiraLink = tm.JiraLink,
                        WebLink = tm.WebLink,
                        Protocol = tm.Protocol,
                        ClientId = tm.ClientId,
                        Client = tm.Client != null ? new ClientSummaryDto
                        {
                            ClientId = tm.Client.ClientId,
                            Name = tm.Client.Name,
                            Description = tm.Client.Description
                        } : null,
                        Studies = tm.Studies.Select(s => new StudySummaryDto
                        {
                            StudyId = s.StudyId,
                            Name = s.Name,
                            Protocol = s.Protocol,
                            Description = s.Description
                        }).ToList(),
                        Tasks = tm.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(trialManagers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trial managers");
                return StatusCode(500, "An error occurred while retrieving trial managers");
            }
        }

        // GET: api/TrialManager/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TrialManagerResponseDto>> GetTrialManager(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid trial manager ID");
                }

                var trialManager = await _context.TrialManagers
                    .Include(tm => tm.Client)
                    .Include(tm => tm.Studies)
                        .ThenInclude(s => s.InteractiveResponseTechnologies)
                    .Include(tm => tm.Tasks)
                    .Where(tm => tm.TrialManagerId == id)
                    .Select(tm => new TrialManagerResponseDto
                    {
                        TrialManagerId = tm.TrialManagerId,
                        Version = tm.Version,
                        JiraKey = tm.JiraKey,
                        JiraLink = tm.JiraLink,
                        WebLink = tm.WebLink,
                        Protocol = tm.Protocol,
                        ClientId = tm.ClientId,
                        Client = tm.Client != null ? new ClientSummaryDto
                        {
                            ClientId = tm.Client.ClientId,
                            Name = tm.Client.Name,
                            Description = tm.Client.Description
                        } : null,
                        Studies = tm.Studies.Select(s => new StudySummaryDto
                        {
                            StudyId = s.StudyId,
                            Name = s.Name,
                            Protocol = s.Protocol,
                            Description = s.Description
                        }).ToList(),
                        Tasks = tm.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (trialManager == null)
                {
                    return NotFound($"Trial manager with ID {id} not found");
                }

                return Ok(trialManager);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trial manager {TrialManagerId}", id);
                return StatusCode(500, "An error occurred while retrieving the trial manager");
            }
        }

        // POST: api/TrialManager
        [HttpPost]
        public async Task<ActionResult<TrialManagerResponseDto>> PostTrialManager(CreateTrialManagerDto createTrialManagerDto)
        {
            try
            {
                // Validate that the client exists
                var clientExists = await _context.Clients.AnyAsync(c => c.ClientId == createTrialManagerDto.ClientId);
                if (!clientExists)
                {
                    return BadRequest("The specified client does not exist");
                }

                // Check if client already has a trial manager (1:1 relationship)
                var existingTrialManager = await _context.TrialManagers
                    .AnyAsync(tm => tm.ClientId == createTrialManagerDto.ClientId);
                
                if (existingTrialManager)
                {
                    return Conflict("This client already has a trial manager");
                }

                var trialManager = new TrialManager
                {
                    TrialManagerId = Guid.NewGuid(),
                    ClientId = createTrialManagerDto.ClientId,
                    Version = createTrialManagerDto.Version,
                    JiraKey = createTrialManagerDto.JiraKey,
                    JiraLink = createTrialManagerDto.JiraLink,
                    WebLink = createTrialManagerDto.WebLink,
                    Protocol = createTrialManagerDto.Protocol
                };

                _context.TrialManagers.Add(trialManager);
                await _context.SaveChangesAsync();

                // Load the client for the response
                var client = await _context.Clients.FindAsync(createTrialManagerDto.ClientId);

                var responseDto = new TrialManagerResponseDto
                {
                    TrialManagerId = trialManager.TrialManagerId,
                    Version = trialManager.Version,
                    JiraKey = trialManager.JiraKey,
                    JiraLink = trialManager.JiraLink,
                    WebLink = trialManager.WebLink,
                    Protocol = trialManager.Protocol,
                    ClientId = trialManager.ClientId,
                    Client = client != null ? new ClientSummaryDto
                    {
                        ClientId = client.ClientId,
                        Name = client.Name,
                        Description = client.Description
                    } : null,
                    Studies = new List<StudySummaryDto>(),
                    Tasks = new List<TaskSummaryDto>()
                };

                return CreatedAtAction("GetTrialManager", new { id = trialManager.TrialManagerId }, responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating trial manager");
                return StatusCode(500, "An error occurred while creating the trial manager");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trial manager");
                return StatusCode(500, "An error occurred while creating the trial manager");
            }
        }

        // PUT: api/TrialManager/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTrialManager(Guid id, UpdateTrialManagerDto updateTrialManagerDto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid trial manager ID");
                }

                var trialManager = await _context.TrialManagers.FindAsync(id);
                if (trialManager == null)
                {
                    return NotFound($"Trial manager with ID {id} not found");
                }

                trialManager.Version = updateTrialManagerDto.Version;
                trialManager.JiraKey = updateTrialManagerDto.JiraKey;
                trialManager.JiraLink = updateTrialManagerDto.JiraLink;
                trialManager.WebLink = updateTrialManagerDto.WebLink;
                trialManager.Protocol = updateTrialManagerDto.Protocol;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating trial manager {TrialManagerId}", id);
                
                if (!await TrialManagerExists(id))
                {
                    return NotFound($"Trial manager with ID {id} not found");
                }
                else
                {
                    return Conflict("The trial manager was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trial manager {TrialManagerId}", id);
                return StatusCode(500, "An error occurred while updating the trial manager");
            }
        }

        // DELETE: api/TrialManager/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrialManager(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid trial manager ID");
                }

                var trialManager = await _context.TrialManagers
                    .Include(tm => tm.Studies)
                    .Include(tm => tm.Tasks)
                    .FirstOrDefaultAsync(tm => tm.TrialManagerId == id);

                if (trialManager == null)
                {
                    return NotFound($"Trial manager with ID {id} not found");
                }

                // Check if trial manager has dependent data
                if (trialManager.Studies?.Any() == true)
                {
                    return BadRequest("Cannot delete trial manager with existing studies. Delete studies first.");
                }

                if (trialManager.Tasks?.Any() == true)
                {
                    return BadRequest("Cannot delete trial manager with existing tasks. Complete or delete tasks first.");
                }

                _context.TrialManagers.Remove(trialManager);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting trial manager {TrialManagerId}", id);
                return StatusCode(500, "An error occurred while deleting the trial manager. The trial manager may have dependent records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting trial manager {TrialManagerId}", id);
                return StatusCode(500, "An error occurred while deleting the trial manager");
            }
        }

        // GET: api/TrialManager/by-client/{clientId}
        [HttpGet("by-client/{clientId}")]
        public async Task<ActionResult<TrialManagerResponseDto>> GetTrialManagerByClient(Guid clientId)
        {
            try
            {
                if (clientId == Guid.Empty)
                {
                    return BadRequest("Invalid client ID");
                }

                var trialManager = await _context.TrialManagers
                    .Include(tm => tm.Client)
                    .Include(tm => tm.Studies)
                    .Include(tm => tm.Tasks)
                    .Where(tm => tm.ClientId == clientId)
                    .Select(tm => new TrialManagerResponseDto
                    {
                        TrialManagerId = tm.TrialManagerId,
                        Version = tm.Version,
                        JiraKey = tm.JiraKey,
                        JiraLink = tm.JiraLink,
                        WebLink = tm.WebLink,
                        Protocol = tm.Protocol,
                        ClientId = tm.ClientId,
                        Client = tm.Client != null ? new ClientSummaryDto
                        {
                            ClientId = tm.Client.ClientId,
                            Name = tm.Client.Name,
                            Description = tm.Client.Description
                        } : null,
                        Studies = tm.Studies.Select(s => new StudySummaryDto
                        {
                            StudyId = s.StudyId,
                            Name = s.Name,
                            Protocol = s.Protocol,
                            Description = s.Description
                        }).ToList(),
                        Tasks = tm.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (trialManager == null)
                {
                    return NotFound($"No trial manager found for client {clientId}");
                }

                return Ok(trialManager);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trial manager for client {ClientId}", clientId);
                return StatusCode(500, "An error occurred while retrieving the trial manager");
            }
        }

        private async Task<bool> TrialManagerExists(Guid id)
        {
            return await _context.TrialManagers.AnyAsync(e => e.TrialManagerId == id);
        }
    }
}