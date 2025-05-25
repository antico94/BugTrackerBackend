using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.DTOs;

namespace BugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly BugTrackerContext _context;
        private readonly ILogger<ClientController> _logger;

        public ClientController(BugTrackerContext context, ILogger<ClientController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Client
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClients()
        {
            try
            {
                var clients = await _context.Clients
                    .Include(c => c.TrialManager)
                    .Include(c => c.Studies)
                    .Select(c => new ClientResponseDto
                    {
                        ClientId = c.ClientId,
                        Name = c.Name,
                        Description = c.Description,
                        TrialManager = c.TrialManager != null ? new TrialManagerDto
                        {
                            TrialManagerId = c.TrialManager.TrialManagerId,
                            Version = c.TrialManager.Version,
                            JiraKey = c.TrialManager.JiraKey,
                            JiraLink = c.TrialManager.JiraLink,
                            WebLink = c.TrialManager.WebLink,
                            Protocol = c.TrialManager.Protocol
                        } : null,
                        Studies = c.Studies.Select(s => new StudyDto
                        {
                            StudyId = s.StudyId,
                            Name = s.Name,
                            Protocol = s.Protocol,
                            Description = s.Description
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clients");
                return StatusCode(500, "An error occurred while retrieving clients");
            }
        }

        // GET: api/Client/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientResponseDto>> GetClient(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid client ID");
                }

                var client = await _context.Clients
                    .Include(c => c.TrialManager)
                    .Include(c => c.Studies)
                        .ThenInclude(s => s.InteractiveResponseTechnologies)
                    .Where(c => c.ClientId == id)
                    .Select(c => new ClientResponseDto
                    {
                        ClientId = c.ClientId,
                        Name = c.Name,
                        Description = c.Description,
                        TrialManager = c.TrialManager != null ? new TrialManagerDto
                        {
                            TrialManagerId = c.TrialManager.TrialManagerId,
                            Version = c.TrialManager.Version,
                            JiraKey = c.TrialManager.JiraKey,
                            JiraLink = c.TrialManager.JiraLink,
                            WebLink = c.TrialManager.WebLink,
                            Protocol = c.TrialManager.Protocol
                        } : null,
                        Studies = c.Studies.Select(s => new StudyDto
                        {
                            StudyId = s.StudyId,
                            Name = s.Name,
                            Protocol = s.Protocol,
                            Description = s.Description
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (client == null)
                {
                    return NotFound($"Client with ID {id} not found");
                }

                return Ok(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client {ClientId}", id);
                return StatusCode(500, "An error occurred while retrieving the client");
            }
        }

        // POST: api/Client
        [HttpPost]
        public async Task<ActionResult<ClientResponseDto>> PostClient(CreateClientDto createClientDto)
        {
            try
            {
                // Check for duplicate names
                if (await _context.Clients.AnyAsync(c => c.Name == createClientDto.Name))
                {
                    return Conflict("A client with this name already exists");
                }

                var client = new Client
                {
                    ClientId = Guid.NewGuid(),
                    Name = createClientDto.Name,
                    Description = createClientDto.Description
                };

                _context.Clients.Add(client);
                await _context.SaveChangesAsync();

                var responseDto = new ClientResponseDto
                {
                    ClientId = client.ClientId,
                    Name = client.Name,
                    Description = client.Description,
                    TrialManager = null,
                    Studies = new List<StudyDto>()
                };

                return CreatedAtAction("GetClient", new { id = client.ClientId }, responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating client");
                return StatusCode(500, "An error occurred while creating the client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client");
                return StatusCode(500, "An error occurred while creating the client");
            }
        }

        // PUT: api/Client/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClient(Guid id, UpdateClientDto updateClientDto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid client ID");
                }

                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                {
                    return NotFound($"Client with ID {id} not found");
                }

                // Check for duplicate names (excluding current client)
                if (await _context.Clients.AnyAsync(c => c.Name == updateClientDto.Name && c.ClientId != id))
                {
                    return Conflict("A client with this name already exists");
                }

                client.Name = updateClientDto.Name;
                client.Description = updateClientDto.Description;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating client {ClientId}", id);
                
                if (!await ClientExists(id))
                {
                    return NotFound($"Client with ID {id} not found");
                }
                else
                {
                    return Conflict("The client was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client {ClientId}", id);
                return StatusCode(500, "An error occurred while updating the client");
            }
        }

        // DELETE: api/Client/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Invalid client ID");
                }

                var client = await _context.Clients
                    .Include(c => c.TrialManager)
                    .Include(c => c.Studies)
                    .FirstOrDefaultAsync(c => c.ClientId == id);

                if (client == null)
                {
                    return NotFound($"Client with ID {id} not found");
                }

                // Check if client has dependent data
                if (client.Studies?.Any() == true)
                {
                    return BadRequest("Cannot delete client with existing studies. Delete studies first.");
                }

                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting client {ClientId}", id);
                return StatusCode(500, "An error occurred while deleting the client. The client may have dependent records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client {ClientId}", id);
                return StatusCode(500, "An error occurred while deleting the client");
            }
        }

        private async Task<bool> ClientExists(Guid id)
        {
            return await _context.Clients.AnyAsync(e => e.ClientId == id);
        }
    }
}