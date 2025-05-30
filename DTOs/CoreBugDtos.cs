// DTOs/CoreBugDtos.cs
using System.ComponentModel.DataAnnotations;
using BugTracker.Models.Enums;

namespace BugTracker.DTOs;

public class CreateCoreBugDto
{
    [Required]
    [StringLength(500)]
    public string BugTitle { get; set; }
    
    [Required]
    [StringLength(50)]
    public string JiraKey { get; set; }
    
    [Required]
    public string JiraLink { get; set; }
    
    [Required]
    public string BugDescription { get; set; }
    
    [Required]
    public BugSeverity Severity { get; set; }
    
    public string? FoundInBuild { get; set; }
    
    public List<string>? AffectedVersions { get; set; } = new List<string>();
}

public class UpdateCoreBugDto
{
    [Required]
    [StringLength(500)]
    public string BugTitle { get; set; }
    
    [Required]
    public string BugDescription { get; set; }
    
    [Required]
    public BugSeverity Severity { get; set; }
    
    public string? FoundInBuild { get; set; }
    
    public List<string>? AffectedVersions { get; set; } = new List<string>();
}

public class BugAssessmentDto
{
    [Required]
    public Guid BugId { get; set; }
    
    [Required]
    public ProductType AssessedProductType { get; set; }
    
    [Required]
    public List<string> AssessedImpactedVersions { get; set; } = new List<string>();
    
    public string? AssessedBy { get; set; }
}

public class CoreBugResponseDto
{
    public Guid BugId { get; set; }
    public string BugTitle { get; set; }
    public string JiraKey { get; set; }
    public string JiraLink { get; set; }
    public string BugDescription { get; set; }
    public Status Status { get; set; }
    public string? FoundInBuild { get; set; }
    public List<string> AffectedVersions { get; set; } = new List<string>();
    public BugSeverity Severity { get; set; }
    
    // Assessment Fields
    public ProductType? AssessedProductType { get; set; }
    public List<string>? AssessedImpactedVersions { get; set; } = new List<string>();
    public bool IsAssessed { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string? AssessedBy { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    // Related Data
    public List<TaskSummaryDto> Tasks { get; set; } = new List<TaskSummaryDto>();
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
}

public class BugImportDto
{
    public string Key { get; set; } // JIRA key like "CBS-29804"
    public string Title { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public string FoundInBuild { get; set; }

    public string JiraLink { get; set; }
    public List<string> AffectedVersions { get; set; } = new List<string>();
}

public class BulkImportResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}