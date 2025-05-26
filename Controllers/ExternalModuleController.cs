// Controllers/ExternalModuleController.cs
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
    public class ExternalModuleController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<ExternalModuleController> _logger;

        public ExternalModuleController(BugTrackerContext context, ILogger<ExternalModuleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ExternalModule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExternalModuleResponseDto>>> GetExternalModules()
        {
            try
            {
                var externalModules = await _context.ExternalModules
                    .Include(em => em.InteractiveResponseTechnology)
                        .ThenInclude(irt => irt.Study)
                            .ThenInclude(s => s.Client)
                    .Select(em => new ExternalModuleResponseDto
                    {
                        ExternalModuleId = em.ExternalModuleId,
                        Name = em.Name,
                        Version = em.Version,
                        ExternalModuleType = em.ExternalModuleType,
                        InteractiveResponseTechnologyId = em.InteractiveResponseTechnologyId,
                        InteractiveResponseTechnology = em.InteractiveResponseTechnology != null ? new IRTBasicDto
                        {
                            InteractiveResponseTechnologyId = em.InteractiveResponseTechnology.InteractiveResponseTechnologyId,
                            Version = em.InteractiveResponseTechnology.Version,
                            JiraKey = em.InteractiveResponseTechnology.JiraKey,
                            WebLink = em.InteractiveResponseTechnology.WebLink,
                            Study = em.InteractiveResponseTechnology.Study != null ? new StudyBasicDto
                            {
                                StudyId = em.InteractiveResponseTechnology.Study.StudyId,
                                Name = em.InteractiveResponseTechnology.Study.Name,
                                Protocol = em.InteractiveResponseTechnology.Study.Protocol,
                                Description = em.InteractiveResponseTechnology.Study.Description,
                                Client = em.InteractiveResponseTechnology.Study.Client != null ? new ClientSummaryDto
                                {
                                    ClientId = em.InteractiveResponseTechnology.Study.Client.ClientId,
                                    Name = em.InteractiveResponseTechnology.Study.Client.Name,
                                    Description = em.InteractiveResponseTechnology.Study.Client.Description
                                } : null
                            } : null
                        } : null
                    })
                    .ToListAsync();

                return Ok(externalModules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving external modules");
                return StatusCode(500, "An error occurred while retrieving external modules");
            }
        }

        // GET: api/ExternalModule/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExternalModuleResponseDto>> GetExternalModule(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid external module ID");
                }

                var externalModule = await _context.ExternalModules
                    .Include(em => em.InteractiveResponseTechnology)
                        .ThenInclude(irt => irt.Study)
                            .ThenInclude(s => s.Client)
                    .Where(em => em.ExternalModuleId == id)
                    .Select(em => new ExternalModuleResponseDto
                    {
                        ExternalModuleId = em.ExternalModuleId,
                        Name = em.Name,
                        Version = em.Version,
                        ExternalModuleType = em.ExternalModuleType,
                        InteractiveResponseTechnologyId = em.InteractiveResponseTechnologyId,
                        InteractiveResponseTechnology = em.InteractiveResponseTechnology != null ? new IRTBasicDto
                        {
                            InteractiveResponseTechnologyId = em.InteractiveResponseTechnology.InteractiveResponseTechnologyId,
                            Version = em.InteractiveResponseTechnology.Version,
                            JiraKey = em.InteractiveResponseTechnology.JiraKey,
                            WebLink = em.InteractiveResponseTechnology.WebLink,
                            Study = em.InteractiveResponseTechnology.Study != null ? new StudyBasicDto
                            {
                                StudyId = em.InteractiveResponseTechnology.Study.StudyId,
                                Name = em.InteractiveResponseTechnology.Study.Name,
                                Protocol = em.InteractiveResponseTechnology.Study.Protocol,
                                Description = em.InteractiveResponseTechnology.Study.Description,
                                Client = em.InteractiveResponseTechnology.Study.Client != null ? new ClientSummaryDto
                                {
                                    ClientId = em.InteractiveResponseTechnology.Study.Client.ClientId,
                                    Name = em.InteractiveResponseTechnology.Study.Client.Name,
                                    Description = em.InteractiveResponseTechnology.Study.Client.Description
                                } : null
                            } : null
                        } : null
                    })
                    .FirstOrDefaultAsync();

                if (externalModule == null)
                {
                    return NotFound($"External module with ID {id} not found");
                }

                return Ok(externalModule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving external module {ExternalModuleId}", id);
                return StatusCode(500, "An error occurred while retrieving the external module");
            }
        }

        // POST: api/ExternalModule
        [HttpPost]
        public async Task<ActionResult<ExternalModuleResponseDto>> PostExternalModule(CreateExternalModuleDto createExternalModuleDto)
        {
            try
            {
                // Validate that the IRT exists
                var irt = await _context.InteractiveResponseTechnologies
                    .Include(irt => irt.Study)
                        .ThenInclude(s => s.Client)
                    .FirstOrDefaultAsync(irt => irt.InteractiveResponseTechnologyId == createExternalModuleDto.InteractiveResponseTechnologyId);
                if (irt == null)
                {
                    return BadRequest("The specified IRT does not exist");
                }

                // Check for duplicate name and type within the same IRT
                if (await _context.ExternalModules.AnyAsync(em => em.Name == createExternalModuleDto.Name && 
                                                                 em.ExternalModuleType == createExternalModuleDto.ExternalModuleType &&
                                                                 em.InteractiveResponseTechnologyId == createExternalModuleDto.InteractiveResponseTechnologyId))
                {
                    return Conflict("An external module with this name and type already exists for this IRT");
                }

                var externalModule = new ExternalModule
                {
                    ExternalModuleId = Guid.NewGuid(),
                    InteractiveResponseTechnologyId = createExternalModuleDto.InteractiveResponseTechnologyId,
                    Name = createExternalModuleDto.Name,
                    Version = createExternalModuleDto.Version,
                    ExternalModuleType = createExternalModuleDto.ExternalModuleType
                };

                _context.ExternalModules.Add(externalModule);
                await _context.SaveChangesAsync();

                var responseDto = new ExternalModuleResponseDto
                {
                    ExternalModuleId = externalModule.ExternalModuleId,
                    Name = externalModule.Name,
                    Version = externalModule.Version,
                    ExternalModuleType = externalModule.ExternalModuleType,
                    InteractiveResponseTechnologyId = externalModule.InteractiveResponseTechnologyId,
                    InteractiveResponseTechnology = new IRTBasicDto
                    {
                        InteractiveResponseTechnologyId = irt.InteractiveResponseTechnologyId,
                        Version = irt.Version,
                        JiraKey = irt.JiraKey,
                        WebLink = irt.WebLink,
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
                        } : null
                    }
                };

                return CreatedAtAction("GetExternalModule", new { id = externalModule.ExternalModuleId }, responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating external module");
                return StatusCode(500, "An error occurred while creating the external module");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating external module");
                return StatusCode(500, "An error occurred while creating the external module");
            }
        }

        // PUT: api/ExternalModule/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutExternalModule(Guid id, UpdateExternalModuleDto updateExternalModuleDto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid external module ID");
                }

                var externalModule = await _context.ExternalModules.FindAsync(id);
                if (externalModule == null)
                {
                    return NotFound($"External module with ID {id} not found");
                }

                // Check for duplicate name and type within the same IRT (excluding current module)
                if (await _context.ExternalModules.AnyAsync(em => em.Name == updateExternalModuleDto.Name && 
                                                                 em.ExternalModuleType == updateExternalModuleDto.ExternalModuleType &&
                                                                 em.InteractiveResponseTechnologyId == externalModule.InteractiveResponseTechnologyId &&
                                                                 em.ExternalModuleId != id))
                {
                    return Conflict("An external module with this name and type already exists for this IRT");
                }

                externalModule.Name = updateExternalModuleDto.Name;
                externalModule.Version = updateExternalModuleDto.Version;
                externalModule.ExternalModuleType = updateExternalModuleDto.ExternalModuleType;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating external module {ExternalModuleId}", id);
                
                if (!await ExternalModuleExists(id))
                {
                    return NotFound($"External module with ID {id} not found");
                }
                else
                {
                    return Conflict("The external module was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating external module {ExternalModuleId}", id);
                return StatusCode(500, "An error occurred while updating the external module");
            }
        }

        // DELETE: api/ExternalModule/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExternalModule(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid external module ID");
                }

                var externalModule = await _context.ExternalModules.FindAsync(id);
                if (externalModule == null)
                {
                    return NotFound($"External module with ID {id} not found");
                }

                _context.ExternalModules.Remove(externalModule);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting external module {ExternalModuleId}", id);
                return StatusCode(500, "An error occurred while deleting the external module");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting external module {ExternalModuleId}", id);
                return StatusCode(500, "An error occurred while deleting the external module");
            }
        }

        // GET: api/ExternalModule/by-irt/{irtId}
        [HttpGet("by-irt/{irtId}")]
        public async Task<ActionResult<IEnumerable<ExternalModuleResponseDto>>> GetExternalModulesByIRT(Guid irtId)
        {
            try
            {
                if (irtId == Guid.Empty)
                {
                    return BadRequest("Invalid IRT ID");
                }

                var externalModules = await _context.ExternalModules
                    .Include(em => em.InteractiveResponseTechnology)
                        .ThenInclude(irt => irt.Study)
                            .ThenInclude(s => s.Client)
                    .Where(em => em.InteractiveResponseTechnologyId == irtId)
                    .Select(em => new ExternalModuleResponseDto
                    {
                        ExternalModuleId = em.ExternalModuleId,
                        Name = em.Name,
                        Version = em.Version,
                        ExternalModuleType = em.ExternalModuleType,
                        InteractiveResponseTechnologyId = em.InteractiveResponseTechnologyId,
                        InteractiveResponseTechnology = em.InteractiveResponseTechnology != null ? new IRTBasicDto
                        {
                            InteractiveResponseTechnologyId = em.InteractiveResponseTechnology.InteractiveResponseTechnologyId,
                            Version = em.InteractiveResponseTechnology.Version,
                            JiraKey = em.InteractiveResponseTechnology.JiraKey,
                            WebLink = em.InteractiveResponseTechnology.WebLink,
                            Study = em.InteractiveResponseTechnology.Study != null ? new StudyBasicDto
                            {
                                StudyId = em.InteractiveResponseTechnology.Study.StudyId,
                                Name = em.InteractiveResponseTechnology.Study.Name,
                                Protocol = em.InteractiveResponseTechnology.Study.Protocol,
                                Description = em.InteractiveResponseTechnology.Study.Description,
                                Client = em.InteractiveResponseTechnology.Study.Client != null ? new ClientSummaryDto
                                {
                                    ClientId = em.InteractiveResponseTechnology.Study.Client.ClientId,
                                    Name = em.InteractiveResponseTechnology.Study.Client.Name,
                                    Description = em.InteractiveResponseTechnology.Study.Client.Description
                                } : null
                            } : null
                        } : null
                    })
                    .ToListAsync();

                return Ok(externalModules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving external modules for IRT {IRTId}", irtId);
                return StatusCode(500, "An error occurred while retrieving external modules");
            }
        }

        // GET: api/ExternalModule/by-type/{moduleType}
        [HttpGet("by-type/{moduleType}")]
        public async Task<ActionResult<IEnumerable<ExternalModuleResponseDto>>> GetExternalModulesByType(ExternalModuleType moduleType)
        {
            try
            {
                var externalModules = await _context.ExternalModules
                    .Include(em => em.InteractiveResponseTechnology)
                        .ThenInclude(irt => irt.Study)
                            .ThenInclude(s => s.Client)
                    .Where(em => em.ExternalModuleType == moduleType)
                    .Select(em => new ExternalModuleResponseDto
                    {
                        ExternalModuleId = em.ExternalModuleId,
                        Name = em.Name,
                        Version = em.Version,
                        ExternalModuleType = em.ExternalModuleType,
                        InteractiveResponseTechnologyId = em.InteractiveResponseTechnologyId,
                        InteractiveResponseTechnology = em.InteractiveResponseTechnology != null ? new IRTBasicDto
                        {
                            InteractiveResponseTechnologyId = em.InteractiveResponseTechnology.InteractiveResponseTechnologyId,
                            Version = em.InteractiveResponseTechnology.Version,
                            JiraKey = em.InteractiveResponseTechnology.JiraKey,
                            WebLink = em.InteractiveResponseTechnology.WebLink,
                            Study = em.InteractiveResponseTechnology.Study != null ? new StudyBasicDto
                            {
                                StudyId = em.InteractiveResponseTechnology.Study.StudyId,
                                Name = em.InteractiveResponseTechnology.Study.Name,
                                Protocol = em.InteractiveResponseTechnology.Study.Protocol,
                                Description = em.InteractiveResponseTechnology.Study.Description
                            } : null
                        } : null
                    })
                    .ToListAsync();

                return Ok(externalModules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving external modules by type {ModuleType}", moduleType);
                return StatusCode(500, "An error occurred while retrieving external modules");
            }
        }

        private async Task<bool> ExternalModuleExists(Guid id)
        {
            return await _context.ExternalModules.AnyAsync(e => e.ExternalModuleId == id);
        }
    }
}