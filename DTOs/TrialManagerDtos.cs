// DTOs/TrialManagerDtos.cs
using System.ComponentModel.DataAnnotations;

namespace BugTracker.DTOs;

public class CreateTrialManagerDto
{
    [Required]
    public Guid ClientId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Version { get; set; }
    
    [StringLength(50)]
    public string? JiraKey { get; set; }
    
    public string? JiraLink { get; set; }
    
    public string? WebLink { get; set; }
    
    public string? Protocol { get; set; }
}

public class UpdateTrialManagerDto
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

public class TrialManagerResponseDto
{
    public Guid TrialManagerId { get; set; }
    public string Version { get; set; }
    public string? JiraKey { get; set; }
    public string? JiraLink { get; set; }
    public string? WebLink { get; set; }
    public string? Protocol { get; set; }
    public Guid ClientId { get; set; }
    public ClientSummaryDto? Client { get; set; }
    public List<StudySummaryDto>? Studies { get; set; }
    public List<TaskSummaryDto>? Tasks { get; set; }
}

public class ClientSummaryDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public class StudySummaryDto
{
    public Guid StudyId { get; set; }
    public string Name { get; set; }
    public string Protocol { get; set; }
    public string Description { get; set; }
}

public class TaskSummaryDto
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}