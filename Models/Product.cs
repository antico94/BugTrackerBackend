using BugTracker.Models.Enums;
using BugTracker.Models.Interfaces;

namespace BugTracker.Models;

public class Product : IProduct
{
    public Study Study { get; set; }
    public Guid StudyId { get; set; }
    public ProductType Type { get; set; }
    public Guid ProductId { get; set; }
    public string JiraLink { get; set; }
    public string WebLink { get; set; }
    public string Protocol { get; set; }
    public string Version { get; set; }
}