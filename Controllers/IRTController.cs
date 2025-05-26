// Controllers/IRTController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.DTOs;

namespace BugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IRTController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<IRTController> _logger;

        public IRTController(BugTrackerContext context, ILogger<IRTController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/IRT
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IRTResponseDto>>> GetIRTs()
        {
            try
            {
                var irts = await _context.InteractiveResponseTechnologies
                    .Include(irt => irt.Study)
                        .ThenInclude(s => s.Client)
                    .Include(irt => irt.TrialManager)
                    .Include(irt => irt.ExternalModules)
                    .Include(irt => irt.Tasks)
                    .Select(irt => new IRTResponseDto
                    {
                        InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                        Version = irt.Version,
                        JiraKey = irt.JiraKey,
                        JiraLink = irt.JiraLink,
                        WebLink = irt.WebLink,
                        Protocol = irt.Protocol,
                        StudyId = irt.StudyId,
                        TrialManagerId = irt.TrialManagerId,
                        Study = irt.Study != null ? new StudyBasicDto
                        {
                            StudyId = irt.Study.StudyId,
                            Name = irt.Study.Name,
                            Protocol = irt.Study.Protocol,
                            Description = irt.Study.Description,
                            Client = irt.Study.Client != null ? new ClientSummaryDto
                            {
                                ClientId = irt.Study.Client.ClientId,
                                Name = irt.Study.Client.Name,
                                Description = irt.Study.Client.Description
                            } : null
                        } : null,
                        TrialManager = irt.TrialManager != null ? new TrialManagerSummaryDto
                        {
                            TrialManagerId = irt.TrialManager.TrialManagerId,
                            Version = irt.TrialManager.Version,
                            JiraKey = irt.TrialManager.JiraKey
                        } : null,
                        ExternalModules = irt.ExternalModules.Select(em => new ExternalModuleSummaryDto
                        {
                            ExternalModuleId = em.ExternalModuleId,
                            Name = em.Name,
                            Version = em.Version,
                            ExternalModuleType = em.ExternalModuleType.ToString()
                        }).ToList(),
                        Tasks = irt.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(irts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IRTs");
                return StatusCode(500, "An error occurred while retrieving IRTs");
            }
        }

        // GET: api/IRT/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IRTResponseDto>> GetIRT(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid IRT ID");
                }

                var irt = await _context.InteractiveResponseTechnologies
                    .Include(irt => irt.Study)
                        .ThenInclude(s => s.Client)
                    .Include(irt => irt.TrialManager)
                    .Include(irt => irt.ExternalModules)
                    .Include(irt => irt.Tasks)
                    .Where(irt => irt.InteractiveResponseTechnologyId == id)
                    .Select(irt => new IRTResponseDto
                    {
                        InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                        Version = irt.Version,
                        JiraKey = irt.JiraKey,
                        JiraLink = irt.JiraLink,
                        WebLink = irt.WebLink,
                        Protocol = irt.Protocol,
                        StudyId = irt.StudyId,
                        TrialManagerId = irt.TrialManagerId,
                        Study = irt.Study != null ? new StudyBasicDto
                        {
                            StudyId = irt.Study.StudyId,
                            Name = irt.Study.Name,
                            Protocol = irt.Study.Protocol,
                            Description = irt.Study.Description,
                            Client = irt.Study.Client != null ? new ClientSummaryDto
                            {
                                ClientId = irt.Study.Client.ClientId,
                                Name = irt.Study.Client.Name,
                                Description = irt.Study.Client.Description
                            } : null
                        } : null,
                        TrialManager = irt.TrialManager != null ? new TrialManagerSummaryDto
                        {
                            TrialManagerId = irt.TrialManager.TrialManagerId,
                            Version = irt.TrialManager.Version,
                            JiraKey = irt.TrialManager.JiraKey
                        } : null,
                        ExternalModules = irt.ExternalModules.Select(em => new ExternalModuleSummaryDto
                        {
                            ExternalModuleId = em.ExternalModuleId,
                            Name = em.Name,
                            Version = em.Version,
                            ExternalModuleType = em.ExternalModuleType.ToString()
                        }).ToList(),
                        Tasks = irt.Tasks.Select(t => new TaskSummaryDto
                        {
                            TaskId = t.TaskId,
                            TaskTitle = t.TaskTitle,
                            Status = t.Status.ToString(),
                            CreatedAt = t.CreatedAt,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (irt == null)
                {
                    return NotFound($"IRT with ID {id} not found");
                }

                return Ok(irt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IRT {IRTId}", id);
                return StatusCode(500, "An error occurred while retrieving the IRT");
            }
        }

        // POST: api/IRT
        [HttpPost]
        public async Task<ActionResult<IRTResponseDto>> PostIRT(CreateIRTDto createIRTDto)
        {
            try
            {
                // Validate that the study exists
                var study = await _context.Studies
                    .Include(s => s.Client)
                    .FirstOrDefaultAsync(s => s.StudyId == createIRTDto.StudyId);
                if (study == null)
                {
                    return BadRequest("The specified study does not exist");
                }

                // Validate that the trial manager exists and belongs to the same client as the study
                var trialManager = await _context.TrialManagers
                    .FirstOrDefaultAsync(tm => tm.TrialManagerId == createIRTDto.TrialManagerId && 
                                              tm.ClientId == study.ClientId);
                if (trialManager == null)
                {
                    return BadRequest("The specified trial manager does not exist or does not belong to the same client as the study");
                }

                // Check for duplicate version within the same study
                if (await _context.InteractiveResponseTechnologies.AnyAsync(irt => irt.Version == createIRTDto.Version && 
                                                                                   irt.StudyId == createIRTDto.StudyId))
                {
                    return Conflict("An IRT with this version already exists for this study");
                }

                var irt = new InteractiveResponseTechnology
                {
                    InteractiveResponseTechnologyId = Guid.NewGuid(),
                    StudyId = createIRTDto.StudyId,
                    TrialManagerId = createIRTDto.TrialManagerId,
                    Version = createIRTDto.Version,
                    JiraKey = createIRTDto.JiraKey,
                    JiraLink = createIRTDto.JiraLink,
                    WebLink = createIRTDto.WebLink,
                    Protocol = createIRTDto.Protocol
                };

                _context.InteractiveResponseTechnologies.Add(irt);
                await _context.SaveChangesAsync();

                var responseDto = new IRTResponseDto
                {
                    InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                    Version = irt.Version,
                    JiraKey = irt.JiraKey,
                    JiraLink = irt.JiraLink,
                    WebLink = irt.WebLink,
                    Protocol = irt.Protocol,
                    StudyId = irt.StudyId,
                    TrialManagerId = irt.TrialManagerId,
                    Study = new StudyBasicDto
                    {
                        StudyId = study.StudyId,
                        Name = study.Name,
                        Protocol = study.Protocol,
                        Description = study.Description,
                        Client = study.Client != null ? new ClientSummaryDto
                        {
                            ClientId = study.Client.ClientId,
                            Name = study.Client.Name,
                            Description = study.Client.Description
                        } : null
                    },
                    TrialManager = new TrialManagerSummaryDto
                    {
                        TrialManagerId = trialManager.TrialManagerId,
                        Version = trialManager.Version,
                        JiraKey = trialManager.JiraKey
                    },
                    ExternalModules = new List<ExternalModuleSummaryDto>(),
                    Tasks = new List<TaskSummaryDto>()
                };

                return CreatedAtAction("GetIRT", new { id = irt.InteractiveResponseTechnologyId }, responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating IRT");
                return StatusCode(500, "An error occurred while creating the IRT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating IRT");
                return StatusCode(500, "An error occurred while creating the IRT");
            }
        }

        // PUT: api/IRT/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutIRT(Guid id, UpdateIRTDto updateIRTDto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid IRT ID");
                }

                var irt = await _context.InteractiveResponseTechnologies.FindAsync(id);
                if (irt == null)
                {
                    return NotFound($"IRT with ID {id} not found");
                }

                // Check for duplicate version within the same study (excluding current IRT)
                if (await _context.InteractiveResponseTechnologies.AnyAsync(i => i.Version == updateIRTDto.Version && 
                                                                                i.StudyId == irt.StudyId &&
                                                                                i.InteractiveResponseTechnologyId != id))
                {
                    return Conflict("An IRT with this version already exists for this study");
                }

                irt.Version = updateIRTDto.Version;
                irt.JiraKey = updateIRTDto.JiraKey;
                irt.JiraLink = updateIRTDto.JiraLink;
                irt.WebLink = updateIRTDto.WebLink;
                irt.Protocol = updateIRTDto.Protocol;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating IRT {IRTId}", id);
                
                if (!await IRTExists(id))
                {
                    return NotFound($"IRT with ID {id} not found");
                }
                else
                {
                    return Conflict("The IRT was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating IRT {IRTId}", id);
                return StatusCode(500, "An error occurred while updating the IRT");
            }
        }

        // DELETE: api/IRT/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIRT(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid IRT ID");
                }

                var irt = await _context.InteractiveResponseTechnologies
                    .Include(irt => irt.ExternalModules)
                    .Include(irt => irt.Tasks)
                    .FirstOrDefaultAsync(irt => irt.InteractiveResponseTechnologyId == id);

                if (irt == null)
                {
                    return NotFound($"IRT with ID {id} not found");
                }

                // Check if IRT has dependent data
                if (irt.ExternalModules?.Any() == true)
                {
                    return BadRequest("Cannot delete IRT with existing external modules. Delete external modules first.");
                }

                if (irt.Tasks?.Any() == true)
                {
                    return BadRequest("Cannot delete IRT with existing tasks. Complete or delete tasks first.");
                }

                _context.InteractiveResponseTechnologies.Remove(irt);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting IRT {IRTId}", id);
                return StatusCode(500, "An error occurred while deleting the IRT. The IRT may have dependent records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting IRT {IRTId}", id);
                return StatusCode(500, "An error occurred while deleting the IRT");
            }
        }

        // GET: api/IRT/by-study/{studyId}
        [HttpGet("by-study/{studyId}")]
        public async Task<ActionResult<IEnumerable<IRTResponseDto>>> GetIRTsByStudy(Guid studyId)
        {
            try
            {
                if (studyId == Guid.Empty)
                {
                    return BadRequest("Invalid study ID");
                }

                var irts = await _context.InteractiveResponseTechnologies
                    .Include(irt => irt.Study)
                        .ThenInclude(s => s.Client)
                    .Include(irt => irt.TrialManager)
                    .Include(irt => irt.ExternalModules)
                    .Where(irt => irt.StudyId == studyId)
                    .Select(irt => new IRTResponseDto
                    {
                        InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                        Version = irt.Version,
                        JiraKey = irt.JiraKey,
                        JiraLink = irt.JiraLink,
                        WebLink = irt.WebLink,
                        Protocol = irt.Protocol,
                        StudyId = irt.StudyId,
                        TrialManagerId = irt.TrialManagerId,
                        Study = irt.Study != null ? new StudyBasicDto
                        {
                            StudyId = irt.Study.StudyId,
                            Name = irt.Study.Name,
                            Protocol = irt.Study.Protocol,
                            Description = irt.Study.Description,
                            Client = irt.Study.Client != null ? new ClientSummaryDto
                            {
                                ClientId = irt.Study.Client.ClientId,
                                Name = irt.Study.Client.Name,
                                Description = irt.Study.Client.Description
                            } : null
                        } : null,
                        ExternalModules = irt.ExternalModules.Select(em => new ExternalModuleSummaryDto
                        {
                            ExternalModuleId = em.ExternalModuleId,
                            Name = em.Name,
                            Version = em.Version,
                            ExternalModuleType = em.ExternalModuleType.ToString()
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(irts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IRTs for study {StudyId}", studyId);
                return StatusCode(500, "An error occurred while retrieving IRTs");
            }
        }

        // GET: api/IRT/by-client/{clientId}
        [HttpGet("by-client/{clientId}")]
        public async Task<ActionResult<IEnumerable<IRTResponseDto>>> GetIRTsByClient(Guid clientId)
        {
            try
            {
                if (clientId == Guid.Empty)
                {
                    return BadRequest("Invalid client ID");
                }

                var irts = await _context.InteractiveResponseTechnologies
                    .Include(irt => irt.Study)
                        .ThenInclude(s => s.Client)
                    .Include(irt => irt.TrialManager)
                    .Include(irt => irt.ExternalModules)
                    .Where(irt => irt.Study.ClientId == clientId)
                    .Select(irt => new IRTResponseDto
                    {
                        InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                        Version = irt.Version,
                        JiraKey = irt.JiraKey,
                        JiraLink = irt.JiraLink,
                        WebLink = irt.WebLink,
                        Protocol = irt.Protocol,
                        StudyId = irt.StudyId,
                        TrialManagerId = irt.TrialManagerId,
                        Study = irt.Study != null ? new StudyBasicDto
                        {
                            StudyId = irt.Study.StudyId,
                            Name = irt.Study.Name,
                            Protocol = irt.Study.Protocol,
                            Description = irt.Study.Description,
                            Client = irt.Study.Client != null ? new ClientSummaryDto
                            {
                                ClientId = irt.Study.Client.ClientId,
                                Name = irt.Study.Client.Name,
                                Description = irt.Study.Client.Description
                            } : null
                        } : null,
                        ExternalModules = irt.ExternalModules.Select(em => new ExternalModuleSummaryDto
                        {
                            ExternalModuleId = em.ExternalModuleId,
                            Name = em.Name,
                            Version = em.Version,
                            ExternalModuleType = em.ExternalModuleType.ToString()
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(irts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IRTs for client {ClientId}", clientId);
                return StatusCode(500, "An error occurred while retrieving IRTs");
            }
        }

        private async Task<bool> IRTExists(Guid id)
        {
            return await _context.InteractiveResponseTechnologies.AnyAsync(e => e.InteractiveResponseTechnologyId == id);
        }
    }
}