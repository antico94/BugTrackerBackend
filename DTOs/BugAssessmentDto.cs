// DTOs/BugAssessmentDto.cs

using BugTracker.Models.Enums;

namespace BugTracker.DTOs;

public class BugAssessmentDto
{
    public Guid BugId { get; set; }
    public ProductType AssessedProductType { get; set; }
    public List<string> AssessedImpactedVersions { get; set; } = new List<string>();
}