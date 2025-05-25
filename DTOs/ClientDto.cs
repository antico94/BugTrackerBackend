// DTOs/ClientDto.cs
using System.ComponentModel.DataAnnotations;

namespace BugTracker.DTOs;

public class CreateClientDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    public string Description { get; set; }
}

public class UpdateClientDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    public string Description { get; set; }
}

public class ClientResponseDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TrialManagerDto? TrialManager { get; set; }
    public List<StudyDto>? Studies { get; set; }
}

public class TrialManagerDto
{
    public Guid TrialManagerId { get; set; }
    public string Version { get; set; }
    public string JiraKey { get; set; }
    public string JiraLink { get; set; }
    public string WebLink { get; set; }
    public string Protocol { get; set; }
}

public class StudyDto
{
    public Guid StudyId { get; set; }
    public string Name { get; set; }
    public string Protocol { get; set; }
    public string Description { get; set; }
}