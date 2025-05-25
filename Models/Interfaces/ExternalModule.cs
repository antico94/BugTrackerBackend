using BugTracker.Models.Enums;

namespace BugTracker.Models.Interfaces;

public class ExternalModule
{
    public ExternalModuleType ExternalModuleType { get; set; }
    public Guid ExternalModuleId { get; set; }  
    public string Name { get; set; }
    public string Version { get; set; }
    public InteractiveResponseTechnology InteractiveResponseTechnology { get; set; }
    public Guid InteractiveResponseTechnologyId { get; set; }
}