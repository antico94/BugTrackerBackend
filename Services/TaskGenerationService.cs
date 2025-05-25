// Services/TaskGenerationService.cs
using BugTracker.Models;
using BugTracker.Models.Enums;
using BugTracker.DTOs;
using System.Text.Json;

namespace BugTracker.Services;

public class TaskGenerationService
{
    private readonly IServiceProvider _serviceProvider;
    
    public TaskGenerationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task<List<CustomTask>> GenerateTasksForAssessedBug(CoreBug assessedBug)
    {
        var tasks = new List<CustomTask>();
        
        if (!assessedBug.IsAssessed || assessedBug.AssessedProductType == null)
            return tasks;
        
        var impactedVersions = JsonSerializer.Deserialize<List<string>>(
            assessedBug.AssessedImpactedVersions ?? "[]");
        
        if (assessedBug.AssessedProductType == ProductType.TM)
        {
            tasks.AddRange(await GenerateTrialManagerTasks(assessedBug, impactedVersions));
        }
        else if (assessedBug.AssessedProductType == ProductType.InteractiveResponseTechnology)
        {
            tasks.AddRange(await GenerateIRTTasks(assessedBug, impactedVersions));
        }
        
        return tasks;
    }
    
    private async Task<List<CustomTask>> GenerateTrialManagerTasks(CoreBug bug, List<string> impactedVersions)
    {
        var tasks = new List<CustomTask>();
        
        // Get unique TrialManagers with impacted versions
        // Implementation would query your database for TMs matching the versions
        
        return tasks;
    }
    
    private async Task<List<CustomTask>> GenerateIRTTasks(CoreBug bug, List<string> impactedVersions)
    {
        var tasks = new List<CustomTask>();
        
        // Get unique IRTs with impacted versions
        // Implementation would query your database for IRTs matching the versions
        
        return tasks;
    }
}