// Controllers/StudyController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.DTOs;

namespace BugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudyController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<StudyController> _logger;

        public StudyController(BugTrackerContext context, ILogger<StudyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Study
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudyResponseDto>>> GetStudies()
        {
            try
            {
                var studies = await _context.Studies
                    .Include(s => s.Client)
                    .Include(s => s.TrialManager)
                    .Include(s => s.InteractiveResponseTechnologies)
                    .Include(s => s.Tasks)
                    .Select(s => new StudyResponseDto
                    {
                        StudyId = s.StudyId,
                        Name = s.Name,
                        Protocol = s.Protocol,
                        Description = s.Description,
                        ClientId = s.ClientId,
                        TrialManagerId = s.TrialManagerId,
                        Client = s.Client != null ? new ClientSummaryDto
                        {
                            ClientId = s.Client.ClientId,
                            Name = s.Client.Name,
                            Description = s.Client.Description
                        } : null,
                        TrialManager = s.TrialManager != null ? new TrialManagerSummaryDto
                        {
                            TrialManagerId = s.TrialManager.TrialManagerId,
                            Version = s.TrialManager.Version,
                            JiraKey = s.TrialManager.JiraKey
                        } : null,
                        InteractiveResponseTechnologies = s.InteractiveResponseTechnologies.Select(irt => new IRTSummaryDto
                        {
                            InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                            Version = irt.Version,
                            JiraKey = irt.JiraKey,
                            WebLink = irt.WebLink
                        }).ToList(),
                        Tasks = s.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(studies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving studies");
                return StatusCode(500, "An error occurred while retrieving studies");
            }
        }

        // GET: api/Study/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StudyResponseDto>> GetStudy(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid study ID");
                }

                var study = await _context.Studies
                    .Include(s => s.Client)
                    .Include(s => s.TrialManager)
                    .Include(s => s.InteractiveResponseTechnologies)
                        .ThenInclude(irt => irt.ExternalModules)
                    .Include(s => s.Tasks)
                    .Where(s => s.StudyId == id)
                    .Select(s => new StudyResponseDto
                    {
                        StudyId = s.StudyId,
                        Name = s.Name,
                        Protocol = s.Protocol,
                        Description = s.Description,
                        ClientId = s.ClientId,
                        TrialManagerId = s.TrialManagerId,
                        Client = s.Client != null ? new ClientSummaryDto
                        {
                            ClientId = s.Client.ClientId,
                            Name = s.Client.Name,
                            Description = s.Client.Description
                        } : null,
                        TrialManager = s.TrialManager != null ? new TrialManagerSummaryDto
                        {
                            TrialManagerId = s.TrialManager.TrialManagerId,
                            Version = s.TrialManager.Version,
                            JiraKey = s.TrialManager.JiraKey
                        } : null,
                        InteractiveResponseTechnologies = s.InteractiveResponseTechnologies.Select(irt => new IRTSummaryDto
                        {
                            InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                            Version = irt.Version,
                            JiraKey = irt.JiraKey,
                            WebLink = irt.WebLink
                        }).ToList(),
                        Tasks = s.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (study == null)
                {
                    return NotFound($"Study with ID {id} not found");
                }

                return Ok(study);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving study {StudyId}", id);
                return StatusCode(500, "An error occurred while retrieving the study");
            }
        }

        // POST: api/Study
        [HttpPost]
        public async Task<ActionResult<StudyResponseDto>> PostStudy(CreateStudyDto createStudyDto)
        {
            try
            {
                // Validate that the client exists
                var clientExists = await _context.Clients.AnyAsync(c => c.ClientId == createStudyDto.ClientId);
                if (!clientExists)
                {
                    return BadRequest("The specified client does not exist");
                }

                // Validate that the trial manager exists and belongs to the client
                var trialManager = await _context.TrialManagers
                    .FirstOrDefaultAsync(tm => tm.TrialManagerId == createStudyDto.TrialManagerId && 
                                              tm.ClientId == createStudyDto.ClientId);
                if (trialManager == null)
                {
                    return BadRequest("The specified trial manager does not exist or does not belong to the specified client");
                }

                // Check for duplicate protocol within the same trial manager
                if (await _context.Studies.AnyAsync(s => s.Protocol == createStudyDto.Protocol && 
                                                        s.TrialManagerId == createStudyDto.TrialManagerId))
                {
                    return Conflict("A study with this protocol already exists for this trial manager");
                }

                var study = new Study
                {
                    StudyId = Guid.NewGuid(),
                    ClientId = createStudyDto.ClientId,
                    TrialManagerId = createStudyDto.TrialManagerId,
                    Name = createStudyDto.Name,
                    Protocol = createStudyDto.Protocol,
                    Description = createStudyDto.Description
                };

                _context.Studies.Add(study);
                await _context.SaveChangesAsync();

                // Load related data for response
                var client = await _context.Clients.FindAsync(createStudyDto.ClientId);

                var responseDto = new StudyResponseDto
                {
                    StudyId = study.StudyId,
                    Name = study.Name,
                    Protocol = study.Protocol,
                    Description = study.Description,
                    ClientId = study.ClientId,
                    TrialManagerId = study.TrialManagerId,
                    Client = client != null ? new ClientSummaryDto
                    {
                        ClientId = client.ClientId,
                        Name = client.Name,
                        Description = client.Description
                    } : null,
                    TrialManager = new TrialManagerSummaryDto
                    {
                        TrialManagerId = trialManager.TrialManagerId,
                        Version = trialManager.Version,
                        JiraKey = trialManager.JiraKey
                    },
                    InteractiveResponseTechnologies = new List<IRTSummaryDto>(),
                    Tasks = new List<TaskSummaryDto>()
                };

                return CreatedAtAction("GetStudy", new { id = study.StudyId }, responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating study");
                return StatusCode(500, "An error occurred while creating the study");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating study");
                return StatusCode(500, "An error occurred while creating the study");
            }
        }

        // PUT: api/Study/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudy(Guid id, UpdateStudyDto updateStudyDto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid study ID");
                }

                var study = await _context.Studies.FindAsync(id);
                if (study == null)
                {
                    return NotFound($"Study with ID {id} not found");
                }

                // Check for duplicate protocol within the same trial manager (excluding current study)
                if (await _context.Studies.AnyAsync(s => s.Protocol == updateStudyDto.Protocol && 
                                                        s.TrialManagerId == study.TrialManagerId &&
                                                        s.StudyId != id))
                {
                    return Conflict("A study with this protocol already exists for this trial manager");
                }

                study.Name = updateStudyDto.Name;
                study.Protocol = updateStudyDto.Protocol;
                study.Description = updateStudyDto.Description;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating study {StudyId}", id);
                
                if (!await StudyExists(id))
                {
                    return NotFound($"Study with ID {id} not found");
                }
                else
                {
                    return Conflict("The study was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating study {StudyId}", id);
                return StatusCode(500, "An error occurred while updating the study");
            }
        }

        // DELETE: api/Study/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudy(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid study ID");
                }

                var study = await _context.Studies
                    .Include(s => s.InteractiveResponseTechnologies)
                    .Include(s => s.Tasks)
                    .FirstOrDefaultAsync(s => s.StudyId == id);

                if (study == null)
                {
                    return NotFound($"Study with ID {id} not found");
                }

                // Check if study has dependent data
                if (study.InteractiveResponseTechnologies?.Any() == true)
                {
                    return BadRequest("Cannot delete study with existing IRTs. Delete IRTs first.");
                }

                if (study.Tasks?.Any() == true)
                {
                    return BadRequest("Cannot delete study with existing tasks. Complete or delete tasks first.");
                }

                _context.Studies.Remove(study);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting study {StudyId}", id);
                return StatusCode(500, "An error occurred while deleting the study. The study may have dependent records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting study {StudyId}", id);
                return StatusCode(500, "An error occurred while deleting the study");
            }
        }

        // GET: api/Study/by-client/{clientId}
        [HttpGet("by-client/{clientId}")]
        public async Task<ActionResult<IEnumerable<StudyResponseDto>>> GetStudiesByClient(Guid clientId)
        {
            try
            {
                if (clientId == Guid.Empty)
                {
                    return BadRequest("Invalid client ID");
                }

                var studies = await _context.Studies
                    .Include(s => s.Client)
                    .Include(s => s.TrialManager)
                    .Include(s => s.InteractiveResponseTechnologies)
                    .Where(s => s.ClientId == clientId)
                    .Select(s => new StudyResponseDto
                    {
                        StudyId = s.StudyId,
                        Name = s.Name,
                        Protocol = s.Protocol,
                        Description = s.Description,
                        ClientId = s.ClientId,
                        TrialManagerId = s.TrialManagerId,
                        Client = s.Client != null ? new ClientSummaryDto
                        {
                            ClientId = s.Client.ClientId,
                            Name = s.Client.Name,
                            Description = s.Client.Description
                        } : null,
                        TrialManager = s.TrialManager != null ? new TrialManagerSummaryDto
                        {
                            TrialManagerId = s.TrialManager.TrialManagerId,
                            Version = s.TrialManager.Version,
                            JiraKey = s.TrialManager.JiraKey
                        } : null,
                        InteractiveResponseTechnologies = s.InteractiveResponseTechnologies.Select(irt => new IRTSummaryDto
                        {
                            InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                            Version = irt.Version,
                            JiraKey = irt.JiraKey,
                            WebLink = irt.WebLink
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(studies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving studies for client {ClientId}", clientId);
                return StatusCode(500, "An error occurred while retrieving studies");
            }
        }

        private async Task<bool> StudyExists(Guid id)
        {
            return await _context.Studies.AnyAsync(e => e.StudyId == id);
        }
    }
}