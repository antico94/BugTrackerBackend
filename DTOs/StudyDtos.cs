// DTOs/StudyDtos.cs
using System.ComponentModel.DataAnnotations;

namespace BugTracker.DTOs;

public class CreateStudyDto
{
    [Required]
    public Guid ClientId { get; set; }
    
    [Required]
    public Guid TrialManagerId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Protocol { get; set; }
    
    [Required]
    public string Description { get; set; }
}

public class UpdateStudyDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Protocol { get; set; }
    
    [Required]
    public string Description { get; set; }
}

public class StudyResponseDto
{
    public Guid StudyId { get; set; }
    public string Name { get; set; }
    public string Protocol { get; set; }
    public string Description { get; set; }
    public Guid ClientId { get; set; }
    public Guid TrialManagerId { get; set; }
    public ClientSummaryDto? Client { get; set; }
    public TrialManagerSummaryDto? TrialManager { get; set; }
    public List<IRTSummaryDto>? InteractiveResponseTechnologies { get; set; }
    public List<TaskSummaryDto>? Tasks { get; set; }
}

public class TrialManagerSummaryDto
{
    public Guid TrialManagerId { get; set; }
    public string Version { get; set; }
    public string? JiraKey { get; set; }
}

public class IRTSummaryDto
{
    public Guid InteractiveResponseTechnologyId { get; set; }
    public string Version { get; set; }
    public string? JiraKey { get; set; }
    public string? WebLink { get; set; }
}