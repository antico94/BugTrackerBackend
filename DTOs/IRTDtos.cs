// DTOs/IRTDtos.cs
using System.ComponentModel.DataAnnotations;

namespace BugTracker.DTOs;

public class CreateIRTDto
{
    [Required]
    public Guid StudyId { get; set; }
    
    [Required]
    public Guid TrialManagerId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Version { get; set; }
    
    [StringLength(50)]
    public string? JiraKey { get; set; }
    
    public string? JiraLink { get; set; }
    
    public string? WebLink { get; set; }
    
    public string? Protocol { get; set; }
}

public class UpdateIRTDto
{
    [Required]
    [StringLength(50)]
    public string Version { get; set; }
    
    [StringLength(50)]
    public string? JiraKey { get; set; }
    
    public string? JiraLink { get; set; }
    
    public string? WebLink { get; set; }
    
    public string? Protocol { get; set; }
}

public class IRTResponseDto
{
    public Guid InteractiveResponseTechnologyId { get; set; }
    public string Version { get; set; }
    public string? JiraKey { get; set; }
    public string? JiraLink { get; set; }
    public string? WebLink { get; set; }
    public string? Protocol { get; set; }
    public Guid StudyId { get; set; }
    public Guid TrialManagerId { get; set; }
    public StudyBasicDto? Study { get; set; }
    public TrialManagerSummaryDto? TrialManager { get; set; }
    public List<ExternalModuleSummaryDto>? ExternalModules { get; set; }
    public List<TaskSummaryDto>? Tasks { get; set; }
}

public class StudyBasicDto
{
    public Guid StudyId { get; set; }
    public string Name { get; set; }
    public string Protocol { get; set; }
    public string Description { get; set; }
    public ClientSummaryDto? Client { get; set; }
}

public class ExternalModuleSummaryDto
{
    public Guid ExternalModuleId { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string ExternalModuleType { get; set; }
}