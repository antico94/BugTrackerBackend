using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models.Workflow;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BugTracker.Services.Workflow;

/// <summary>
/// Service for seeding and managing workflow definitions
/// </summary>
public class WorkflowSeederService
{
    private readonly BugTrackerContext _context;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly ILogger<WorkflowSeederService> _logger;
    private readonly IWebHostEnvironment _environment;

    public WorkflowSeederService(
        BugTrackerContext context,
        IWorkflowDefinitionService workflowDefinitionService,
        ILogger<WorkflowSeederService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _workflowDefinitionService = workflowDefinitionService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Seeds all workflow definitions from the WorkflowDefinitions directory
    /// </summary>
    public async Task SeedWorkflowDefinitionsAsync()
    {
        try
        {
            _logger.LogInformation("Starting workflow definitions seeding process");

            var workflowDefinitionsPath = Path.Combine(_environment.ContentRootPath, "Data", "WorkflowDefinitions");
            
            if (!Directory.Exists(workflowDefinitionsPath))
            {
                _logger.LogWarning("Workflow definitions directory not found: {Path}", workflowDefinitionsPath);
                return;
            }

            var jsonFiles = Directory.GetFiles(workflowDefinitionsPath, "*.json");
            var seededCount = 0;
            var updatedCount = 0;
            var errorCount = 0;

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    await SeedWorkflowDefinitionFromFile(jsonFile);
                    
                    var fileName = Path.GetFileName(jsonFile);
                    var existingDefinition = await GetExistingDefinitionFromFile(jsonFile);
                    
                    if (existingDefinition != null)
                    {
                        updatedCount++;
                        _logger.LogInformation("Updated workflow definition from file: {FileName}", fileName);
                    }
                    else
                    {
                        seededCount++;
                        _logger.LogInformation("Seeded new workflow definition from file: {FileName}", fileName);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Error seeding workflow definition from file: {FilePath}", jsonFile);
                }
            }

            _logger.LogInformation("Workflow seeding completed. Seeded: {Seeded}, Updated: {Updated}, Errors: {Errors}", 
                seededCount, updatedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workflow definitions seeding");
            throw;
        }
    }

    /// <summary>
    /// Seeds a specific workflow definition from a JSON file
    /// </summary>
    public async Task SeedWorkflowDefinitionFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Workflow definition file not found: {filePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var workflowSchema = JsonSerializer.Deserialize<WorkflowSchema>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });

            if (workflowSchema == null)
            {
                throw new InvalidOperationException($"Failed to deserialize workflow schema from file: {filePath}");
            }

            // Create a temporary workflow definition for validation
            var tempDefinition = new WorkflowDefinition
            {
                Name = workflowSchema.Name,
                Description = workflowSchema.Description,
                Version = "1.0.0"
            };
            tempDefinition.SetWorkflowSchema(workflowSchema);
            
            // Validate the workflow schema
            var isValid = await _workflowDefinitionService.ValidateWorkflowDefinitionAsync(tempDefinition);
            if (!isValid)
            {
                throw new InvalidOperationException($"Invalid workflow definition in file {filePath}");
            }

            // Check if definition already exists
            var existingDefinition = await _workflowDefinitionService.LoadWorkflowDefinitionAsync(workflowSchema.Name);
            
            var definition = new WorkflowDefinition
            {
                Name = workflowSchema.Name,
                Description = workflowSchema.Description,
                Version = DetermineVersion(existingDefinition),
                IsActive = true,
                CreatedBy = "System"
            };

            definition.SetWorkflowSchema(workflowSchema);

            await _workflowDefinitionService.SaveWorkflowDefinitionAsync(definition);

            _logger.LogInformation("Successfully seeded workflow definition: {Name} v{Version}", 
                definition.Name, definition.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding workflow definition from file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Gets the bug assessment workflow definition
    /// </summary>
    public async Task<WorkflowDefinition> GetBugAssessmentWorkflowAsync()
    {
        var definition = await _workflowDefinitionService.LoadWorkflowDefinitionAsync("Bug Assessment Workflow");
        if (definition == null)
        {
            throw new InvalidOperationException("Bug Assessment Workflow not found. Please ensure workflow definitions have been seeded.");
        }
        return definition;
    }

    /// <summary>
    /// Checks if workflow definitions need to be seeded
    /// </summary>
    public async Task<bool> NeedsSeeding()
    {
        var existingDefinitions = await _workflowDefinitionService.GetActiveWorkflowDefinitionsAsync();
        return !existingDefinitions.Any();
    }

    /// <summary>
    /// Forces re-seeding of all workflow definitions (useful for updates)
    /// </summary>
    public async Task ForceReseedAllDefinitionsAsync()
    {
        try
        {
            _logger.LogInformation("Force re-seeding all workflow definitions");

            // Deactivate existing definitions
            var existingDefinitions = await _context.WorkflowDefinitions
                .Where(wd => wd.IsActive)
                .ToListAsync();

            foreach (var definition in existingDefinitions)
            {
                definition.IsActive = false;
                definition.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Re-seed all definitions
            await SeedWorkflowDefinitionsAsync();

            _logger.LogInformation("Force re-seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during force re-seeding");
            throw;
        }
    }

    /// <summary>
    /// Validates all seeded workflow definitions
    /// </summary>
    public async Task<Dictionary<string, WorkflowValidationResult>> ValidateAllDefinitionsAsync()
    {
        var results = new Dictionary<string, WorkflowValidationResult>();

        try
        {
            var definitions = await _workflowDefinitionService.GetActiveWorkflowDefinitionsAsync();

            foreach (var definition in definitions)
            {
                var schema = definition.GetWorkflowSchema();
                var isValid = await _workflowDefinitionService.ValidateWorkflowDefinitionAsync(definition);
                results[definition.Name] = new WorkflowValidationResult { IsValid = isValid };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow definitions");
            throw;
        }

        return results;
    }

    private async Task<WorkflowDefinition?> GetExistingDefinitionFromFile(string filePath)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath);
            var workflowSchema = JsonSerializer.Deserialize<WorkflowSchema>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });

            if (workflowSchema?.Name != null)
            {
                return await _workflowDefinitionService.LoadWorkflowDefinitionAsync(workflowSchema.Name);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string DetermineVersion(WorkflowDefinition? existingDefinition)
    {
        if (existingDefinition == null)
        {
            return "1.0.0";
        }

        // Parse existing version and increment patch version
        if (Version.TryParse(existingDefinition.Version, out var currentVersion))
        {
            var newVersion = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build + 1);
            return newVersion.ToString();
        }

        // Fallback to timestamp-based version
        return $"1.0.{DateTime.UtcNow:yyyyMMddHHmmss}";
    }
}