using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BugTracker.Models.Workflow;

/// <summary>
/// Represents a complete workflow definition with all steps, transitions, and rules
/// </summary>
public class WorkflowDefinition
{
    public Guid WorkflowDefinitionId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// JSON representation of the workflow definition
    /// </summary>
    [Required]
    public string DefinitionJson { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string CreatedBy { get; set; } = "System";
    
    /// <summary>
    /// Deserializes the workflow definition from JSON
    /// </summary>
    public WorkflowSchema GetWorkflowSchema()
    {
        return JsonSerializer.Deserialize<WorkflowSchema>(DefinitionJson) 
               ?? throw new InvalidOperationException("Invalid workflow definition JSON");
    }
    
    /// <summary>
    /// Sets the workflow definition from a schema object
    /// </summary>
    public void SetWorkflowSchema(WorkflowSchema schema)
    {
        DefinitionJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// The actual workflow schema that defines steps, transitions, and rules
/// </summary>
public class WorkflowSchema
{
    public string WorkflowId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InitialStepId { get; set; } = string.Empty;
    public List<WorkflowStepDefinition> Steps { get; set; } = new();
    public List<WorkflowTransition> Transitions { get; set; } = new();
    public WorkflowMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Defines a single step in the workflow
/// </summary>
public class WorkflowStepDefinition
{
    public string StepId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; }
    public bool IsTerminal { get; set; }
    public WorkflowStepConfig Config { get; set; } = new();
    public List<WorkflowAction> Actions { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Defines a transition between workflow steps
/// </summary>
public class WorkflowTransition
{
    public string TransitionId { get; set; } = string.Empty;
    public string FromStepId { get; set; } = string.Empty;
    public string ToStepId { get; set; } = string.Empty;
    public string TriggerAction { get; set; } = string.Empty;
    public List<WorkflowCondition> Conditions { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration for a workflow step
/// </summary>
public class WorkflowStepConfig
{
    public bool RequiresNote { get; set; }
    public bool AutoExecute { get; set; }
    public int? TimeoutMinutes { get; set; }
    public List<WorkflowValidationRule> ValidationRules { get; set; } = new();
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Defines an action that can be taken on a workflow step
/// </summary>
public class WorkflowAction
{
    public string ActionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public WorkflowActionType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Defines a condition that must be met for a transition
/// </summary>
public class WorkflowCondition
{
    public string ConditionId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public WorkflowConditionOperator Operator { get; set; }
    public object Value { get; set; } = new();
    public WorkflowConditionLogic Logic { get; set; } = WorkflowConditionLogic.And;
}

/// <summary>
/// Defines a validation rule for a workflow step
/// </summary>
public class WorkflowValidationRule
{
    public string RuleId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public WorkflowValidationType Type { get; set; }
    public object Value { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Metadata for the workflow
/// </summary>
public class WorkflowMetadata
{
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Types of workflow steps
/// </summary>
public enum WorkflowStepType
{
    Action,      // User performs an action
    Decision,    // User makes a decision (Yes/No)
    AutoCheck,   // System automatically evaluates condition
    Manual,      // Manual step requiring user input
    Terminal     // Final step that ends the workflow
}

/// <summary>
/// Types of workflow actions
/// </summary>
public enum WorkflowActionType
{
    Complete,    // Complete the current step
    Decide,      // Make a decision (Yes/No)
    Skip,        // Skip the current step
    Restart,     // Restart the workflow
    Custom       // Custom action
}

/// <summary>
/// Operators for workflow conditions
/// </summary>
public enum WorkflowConditionOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    In,
    NotIn,
    IsNull,
    IsNotNull
}

/// <summary>
/// Logic operators for combining conditions
/// </summary>
public enum WorkflowConditionLogic
{
    And,
    Or
}

/// <summary>
/// Types of validation rules
/// </summary>
public enum WorkflowValidationType
{
    Required,
    MinLength,
    MaxLength,
    Pattern,
    Range,
    Custom
}