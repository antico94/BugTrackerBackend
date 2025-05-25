// Models/ExternalModule.cs
using BugTracker.Models.Enums;

namespace BugTracker.Models;

public class ExternalModule
{
    public Guid ExternalModuleId { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public ExternalModuleType ExternalModuleType { get; set; }
    
    // Foreign Key
    public Guid InteractiveResponseTechnologyId { get; set; }
    
    // Navigation Properties
    public InteractiveResponseTechnology InteractiveResponseTechnology { get; set; }
}