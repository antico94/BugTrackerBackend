namespace BugTracker.DTOs;

public class BugImportDto
{
    public string Key { get; set; } // JIRA key like "SVS-98114"
    public string Title { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public string FoundInBuild { get; set; }
    public List<string> AffectedVersions { get; set; } = new List<string>();
}