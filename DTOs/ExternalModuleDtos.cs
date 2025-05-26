// DTOs/ExternalModuleDtos.cs
using System.ComponentModel.DataAnnotations;
using BugTracker.Models.Enums;

namespace BugTracker.DTOs;

public class CreateExternalModuleDto
{
    [Required]
    public Guid InteractiveResponseTechnologyId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Version { get; set; }
    
    [Required]
    public ExternalModuleType ExternalModuleType { get; set; }
}

public class UpdateExternalModuleDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Version { get; set; }
    
    [Required]
    public ExternalModuleType ExternalModuleType { get; set; }
}

public class ExternalModuleResponseDto
{
    public Guid ExternalModuleId { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public ExternalModuleType ExternalModuleType { get; set; }
    public Guid InteractiveResponseTechnologyId { get; set; }
    public IRTBasicDto? InteractiveResponseTechnology { get; set; }
}

public class IRTBasicDto
{
    public Guid InteractiveResponseTechnologyId { get; set; }
    public string Version { get; set; }
    public string? JiraKey { get; set; }
    public string? WebLink { get; set; }
    public StudyBasicDto? Study { get; set; }
}