// Models/Enums/BugSeverity.cs

using System.Text.Json.Serialization;

namespace BugTracker.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BugSeverity
{
    None = 4,
    Minor = 3,
    Moderate = 2,
    Major = 1,
    Critical = 0,
}