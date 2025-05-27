// DTOs/WeeklyCoreBugsDtos.cs
using System.ComponentModel.DataAnnotations;
using BugTracker.Models.Enums;

namespace BugTracker.DTOs;

public class CreateWeeklyCoreBugsDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    public DateTime WeekStartDate { get; set; }
    
    [Required]
    public DateTime WeekEndDate { get; set; }
    
    public List<Guid>? BugIds { get; set; } = new List<Guid>();
}

public class UpdateWeeklyCoreBugsDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    public DateTime WeekStartDate { get; set; }
    
    [Required]
    public DateTime WeekEndDate { get; set; }
}

public class WeeklyCoreBugsResponseDto
{
    public Guid WeeklyCoreBugsId { get; set; }
    public string Name { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Related Data
    public List<WeeklyCoreBugEntryDto> WeeklyCoreBugEntries { get; set; } = new List<WeeklyCoreBugEntryDto>();
    
    // Statistics
    public int TotalBugsCount { get; set; }
    public int AssessedBugsCount { get; set; }
    public int UnassessedBugsCount { get; set; }
    public int TotalTasksCount { get; set; }
    public int CompletedTasksCount { get; set; }
    public int InProgressTasksCount { get; set; }
    public double CompletionPercentage { get; set; }
}

public class WeeklyCoreBugEntryDto
{
    public Guid WeeklyCoreBugEntryId { get; set; }
    public Guid WeeklyCoreBugsId { get; set; }
    public Guid BugId { get; set; }
    public CoreBugSummaryDto? CoreBug { get; set; }
}

public class CoreBugSummaryDto
{
    public Guid BugId { get; set; }
    public string BugTitle { get; set; }
    public string JiraKey { get; set; }
    public string JiraLink { get; set; }
    public Status Status { get; set; }
    public BugSeverity Severity { get; set; }
    public bool IsAssessed { get; set; }
    public ProductType? AssessedProductType { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Task Statistics
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public List<TaskSummaryDto> Tasks { get; set; } = new List<TaskSummaryDto>();
}

public class AddBugsToWeeklyDto
{
    [Required]
    public Guid WeeklyCoreBugsId { get; set; }
    
    [Required]
    public List<Guid> BugIds { get; set; } = new List<Guid>();
}

public class RemoveBugsFromWeeklyDto
{
    [Required]
    public Guid WeeklyCoreBugsId { get; set; }
    
    [Required]
    public List<Guid> BugIds { get; set; } = new List<Guid>();
}