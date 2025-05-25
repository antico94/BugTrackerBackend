// Models/TaskNote.cs
namespace BugTracker.Models;

public class TaskNote
{
    public Guid TaskNoteId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } // User who created the note
    
    // Foreign Key
    public Guid TaskId { get; set; }
    
    // Navigation Properties
    public CustomTask Task { get; set; }
}