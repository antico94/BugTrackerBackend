// Models/Enums/ProductType.cs

using System.Text.Json.Serialization;

namespace BugTracker.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductType
{
    InteractiveResponseTechnology,
    TM, // TrialManager
    ExternalModule
}