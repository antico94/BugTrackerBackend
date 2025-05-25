// Models/Interfaces/IProduct.cs
using BugTracker.Models.Enums;

namespace BugTracker.Models.Interfaces;

public interface IProduct
{
    Guid ProductId { get; }
    ProductType Type { get; }
    string JiraLink { get; set; }
    string WebLink { get; set; }
    string Protocol { get; set; }
    string Version { get; set; }
    Study Study { get; set; }
    Guid StudyId { get; set; }
}